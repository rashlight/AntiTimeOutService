﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.ServiceModel;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Xml;

namespace AntiTimeOutService
{
    /// <summary>
    /// The services states that will be reported
    /// </summary>
    public enum ServiceState
    {
        STOPPED = 0x00000001,
        START_PENDING = 0x00000002,
        STOP_PENDING = 0x00000003,
        RUNNING = 0x00000004,
        CONTINUE_PENDING = 0x00000005,
        PAUSE_PENDING = 0x00000006,
        PAUSED = 0x00000007,
    }

    /// <summary>
    /// Actions provided by the service
    /// </summary>
    public enum LimitAction
    {
        THROW_EXCEPTION = -1,
        DO_NOTHING = 0,
        WRITE_WARNING_LOG = 1,
    }

    /// <summary>
    /// The command ID received by custom clients (must start from 128)
    /// </summary>
    public enum CommandAction
    {
        CONNECTION_TEST = 128,       
        ERASE_MEM_EVENTLOGS = 129,
        CHANGE_LOGLVL_NONE = 130,
        CHANGE_LOGLVL_CONSERVATIVE = 131,
        CHANGE_LOGLVL_NORMAL = 132,
        CHANGE_LOGLVL_VERBOSE = 133,
    }

    /// <summary>
    /// Conditional conventions in deciding to write an EventLog or not.
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// No logs will be used (parameter override only)
        /// </summary>
        None = 0,
        /// <summary>
        /// SuccessAudit, Information and Warning level will not be logged
        /// </summary>
        Conservative = 1,
        /// <summary>
        /// All events will be logged
        /// </summary>
        Normal = 2,
        /// <summary>
        /// Like Normal, also includes debug texts
        /// </summary>
        Verbose = 3,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ServiceStatus
    {
        public int dwServiceType;
        public ServiceState dwCurrentState;
        public int dwControlsAccepted;
        public int dwWin32ExitCode;
        public int dwServiceSpecificExitCode;
        public int dwCheckPoint;
        public int dwWaitHint;
    };

    public partial class ATOSvc : ServiceBase
    {
        int interval = 1000;
        int timeOut = 1000;
        string link = "1.1.1.1";
        ulong failLimit = 3;
        int failMode = 1; // LimitAction.WRITE_WARNING_LOG
        ulong failedTime = 0;
        LogLevel level = LogLevel.Normal;
        Timer timer = new Timer();

        public ATOSvc()
        {
            // Create - Reuse service
            eventLogger = new System.Diagnostics.EventLog();
            try
            {
                if (!System.Diagnostics.EventLog.SourceExists("AntiTimeOut"))
                {
                    System.Diagnostics.EventLog.CreateEventSource(
                        "AntiTimeOut", "Anti Time-Out Service");
                }
                eventLogger.Source = "AntiTimeOut";
                eventLogger.Log = EventLog.LogNameFromSourceName("AntiTimeOut", ".");
            }
            catch
            {
                Console.WriteLine("Installation of this service requires administrative privileges. Please try again.");
                return;
            }
                              
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(System.IntPtr handle, ref ServiceStatus status);

        /// <summary>
        /// Write something to the EventLog, depends on log level.
        /// Basically a wrapper for eventLogger.WriteEntry().
        /// Usually ID = 1 for error, 2 for warning, 3 for informational stuff
        /// </summary>
        private void AddLogEntry(string message, EventLogEntryType type = EventLogEntryType.Information, int eventID = 3, bool debug = false)
        {
            switch (level)
            {
                case LogLevel.None:
                    return;
                case LogLevel.Normal:
                    if (!debug)
                    {
                        eventLogger.WriteEntry(message, type, eventID);
                    }
                    break;
                case LogLevel.Conservative:
                    if ((type != EventLogEntryType.SuccessAudit || type != EventLogEntryType.Information || type != EventLogEntryType.Warning) && !debug)
                    {
                        eventLogger.WriteEntry(message, type, eventID);
                    }
                    break;
                default:
                    eventLogger.WriteEntry(message, type, eventID);
                    break;
            }
        }
        private string WriteDateTimeLog(string message)
        {
            return "[" + DateTime.UtcNow + "] " + message;
        }

        private void OnReachingFailLimit()
        {           
            switch (failMode)
            {
                case (int)LimitAction.THROW_EXCEPTION:
                    {
                        AddLogEntry(WriteDateTimeLog("An user-handled exception has occurred in AntiTimeOut Service. " +
                            "This service will now trigger a crash."), EventLogEntryType.Error, 1);
                        throw new PingException("Connection attempts exceeded (" + failLimit + ")");
                    }
                case (int)LimitAction.WRITE_WARNING_LOG:
                    {
                        AddLogEntry(WriteDateTimeLog("Connection attempts exceeded (" + failedTime + " out of " + failLimit + " attempt(s)"), EventLogEntryType.Warning, 2);
                        break;
                    }               
                default: break;
            }
        }
        protected override void OnCustomCommand(int command)
        {
            switch (command)
            {
                case (int)CommandAction.CONNECTION_TEST:
                    AddLogEntry(WriteDateTimeLog("A connection test to service has completed."));
                    break;
                case (int)CommandAction.ERASE_MEM_EVENTLOGS:
                    eventLogger.Clear();
                    break;
                case (int)CommandAction.CHANGE_LOGLVL_NONE:
                    level = LogLevel.None;
                    AddLogEntry(WriteDateTimeLog("LogLevel changed to None"));
                    break;
                case (int)CommandAction.CHANGE_LOGLVL_CONSERVATIVE:
                    level = LogLevel.Conservative;
                    AddLogEntry(WriteDateTimeLog("LogLevel changed to Conservative mode"));
                    break;
                case (int)CommandAction.CHANGE_LOGLVL_NORMAL:
                    level = LogLevel.Normal;
                    AddLogEntry(WriteDateTimeLog("LogLevel changed to Normal mode"));
                    break;
                case (int)CommandAction.CHANGE_LOGLVL_VERBOSE:
                    level = LogLevel.Verbose;
                    AddLogEntry(WriteDateTimeLog("LogLevel changed to Verbose mode"));
                    break;
                default: break;
            }

            AddLogEntry(WriteDateTimeLog("Command " + Enum.GetName(typeof(CommandAction), command) + " executed sucessfully"));
        }

        private async void OnPolling(object sender, ElapsedEventArgs e)
        {
            string errString = string.Empty;
            
            await Task.Run(() =>
            {
                Ping p = new Ping();
                PingReply result = null;

                try
                {
                    string source = link;
                    result = p.Send(source, timeOut);

                    if (result.Status == IPStatus.Success)
                    {
                        AddLogEntry(WriteDateTimeLog("Ping to " + source.ToString() + " at [" + result.Address.ToString() + "]" + " completed,"
                           + " roundtrip time = " + result.RoundtripTime.ToString() + "ms"), EventLogEntryType.Information, 3);
                        failedTime = 0;
                        errString = "OK";
                    }
                    else
                    {
                        AddLogEntry(WriteDateTimeLog("Ping to " + source.ToString() + " at [" + result.Address.ToString() + "]" + " failed - " + ((IPStatus)result.Status).ToString()), EventLogEntryType.Warning, 2);
                        failedTime++;
                        if (failedTime >= failLimit)
                        {
                            OnReachingFailLimit();
                        }
                        errString = "PING_STATUS_FAILED";
                    }
                }
                catch (Exception exp)
                {
                    AddLogEntry(WriteDateTimeLog("Ping exception raised:\n" + exp.Source + ": " + exp.Message), EventLogEntryType.Error, 1);
                    failedTime++;
                    if (failedTime >= failLimit)
                    {
                        OnReachingFailLimit();      
                    }
                    errString = "PING_EXCEPTION_FAILED";
                }         
                finally
                {
                    p.Dispose();
                    result = null;
                }
            }).ConfigureAwait(false);

            bool isOutOfTries = (failedTime > failLimit) ? true : false;
            var configFolderName = AppDomain.CurrentDomain.BaseDirectory;
            try
            {
                File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "\\ServiceStatus.log", errString + " " + isOutOfTries.ToString() + " " + DateTime.Now);
            }
            catch (Exception exp)
            {
                AddLogEntry(WriteDateTimeLog("Writing to status log failed:\n" + exp.ToString()), EventLogEntryType.FailureAudit, 1);
            }        
        }
        protected override void OnStart(string[] args)
        {
            // Update the service state to Start Pending.
            ServiceStatus status = new ServiceStatus();
            status.dwCurrentState = ServiceState.START_PENDING;
            #if DEBUG
                status.dwWaitHint = 120000;
            #else
                status.dwWaitHint = 15000;
            #endif
            SetServiceStatus(this.ServiceHandle, ref status);

            base.OnStart(args);

            var configFolderName = AppDomain.CurrentDomain.BaseDirectory;

            try
            {
                string[] parameters;
                if (args.Length == 5)
                {
                    parameters = new string[5] { args[0], args[1], args[2], args[3], args[4] };
                    AddLogEntry(WriteDateTimeLog("Special start parameters detected, overriding..." + configFolderName + "\\ServiceConfig.cfg"));
                }
                else 
                {
                    parameters = File.ReadAllText(configFolderName + "\\ServiceConfig.cfg").Split(' ');
                    AddLogEntry(WriteDateTimeLog("Reading from " + configFolderName + "\\ServiceConfig.cfg"));
                }                              

                // Load parameters
                interval = Convert.ToInt32(parameters[0]);
                timeOut = Convert.ToInt32(parameters[1]);
                link = parameters[2];
                failLimit = Convert.ToUInt64(parameters[3]);
                failMode = Convert.ToInt32(parameters[4]);
            }
            catch
            {
                AddLogEntry(WriteDateTimeLog("Ignoring parameters assignment..."), EventLogEntryType.Information, 2);

                if (args.Length == 0) // Does start parameter is applied? Else it's a file problem
                {
                    Directory.CreateDirectory(configFolderName);
                    File.WriteAllText(configFolderName + "\\ServiceConfig.cfg", interval + " " + timeOut + " " + link + " " + failLimit + " " + failMode);
                    AddLogEntry(WriteDateTimeLog("Created configuration file at " + configFolderName + "\\ServiceConfig.cfg"));
                }
            }

            AddLogEntry(WriteDateTimeLog("Service started with:\n INTERVAL = " + interval + ",\n TIMEOUT = " + timeOut + ",\n LINK = " + link + ",\n LIMIT = " + failLimit + ",\n FAILMODE = " + ((LimitAction)failMode).ToString()));

            // Update the service state to Running.
            status.dwCurrentState = ServiceState.RUNNING;
            SetServiceStatus(this.ServiceHandle, ref status);

            timer.Interval = interval;
            timer.Elapsed += new ElapsedEventHandler(OnPolling);
            timer.Start();
        }
        protected override void OnPause()
        {
            // Update the service state to Pause Pending.
            ServiceStatus status = new ServiceStatus();
            status.dwCurrentState = ServiceState.PAUSE_PENDING;
            #if DEBUG
                status.dwWaitHint = 120000;
            #else
                status.dwWaitHint = 15000;
            #endif
            SetServiceStatus(this.ServiceHandle, ref status);

            timer.Stop();

            base.OnPause();

            AddLogEntry(WriteDateTimeLog("Service paused"));

            // Update the service state to Paused.
            status.dwCurrentState = ServiceState.PAUSED;
            SetServiceStatus(this.ServiceHandle, ref status);
        }
        protected override void OnContinue()
        {
            // Update the service state to Continue Pending.
            ServiceStatus status = new ServiceStatus();
            status.dwCurrentState = ServiceState.CONTINUE_PENDING;
            #if DEBUG
                status.dwWaitHint = 120000;
            #else
                status.dwWaitHint = 15000;
            #endif

            SetServiceStatus(this.ServiceHandle, ref status);

            base.OnContinue();

            timer.Start();

            AddLogEntry(WriteDateTimeLog("Service continued"));

            // Update the service state to Running.
            status.dwCurrentState = ServiceState.RUNNING;
            SetServiceStatus(this.ServiceHandle, ref status);

        }
        protected override void OnShutdown()
        {
            timer.Stop();

            base.OnShutdown();

            AddLogEntry(WriteDateTimeLog("System shutdown, stopping..."));
        }
        protected override void OnStop()
        {
            // Update the service state to Stop Pending.
            ServiceStatus status = new ServiceStatus();
            status.dwCurrentState = ServiceState.STOP_PENDING;
            #if DEBUG
                status.dwWaitHint = 120000;
            #else
                status.dwWaitHint = 15000;
            #endif
            SetServiceStatus(this.ServiceHandle, ref status);

            timer.Stop();

            base.OnStop();

            AddLogEntry(WriteDateTimeLog("Service stopped"));

            // Update the service state to Stopped.
            status.dwCurrentState = ServiceState.STOPPED;
            SetServiceStatus(this.ServiceHandle, ref status);
        }
    }
}
