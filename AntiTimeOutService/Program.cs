﻿using System;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace AntiTimeOutService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            if (System.Environment.UserInteractive && args.Length > 0) // aka is running as console mode
            {
                System.IO.File.AppendAllText(
                    "install.log",
                    "\n" +
                    "+=============================================\n" +
                    "| Service install at " + DateTime.Now + "\n" +
                    "+=============================================" +
                    "\n");
                // Available arguments: logtoconsole, assemblypath, logfile
                string[] installerArgs = new string[1] { "/LogFile=install.log" };
                AssemblyInstaller installer = new AssemblyInstaller(System.AppDomain.CurrentDomain.FriendlyName, installerArgs);
                // Allow the installer to use text logs
                installer.UseNewContext = true;

                switch (args[0])
                {
                    case "-i":
                        // A service installation must be combined with a commit
                        installer.Install(null);
                        installer.Commit(null); 
                        break;
                    case "-u":
                        installer.Uninstall(null);
                        break;
                    default:
                        System.IO.File.AppendAllText("install.log", "Error installing service: invalid arguments\n");
                        break;
                } 
            }
            else
            {
                ServiceBase[] ServicesToRun;

                ServicesToRun = new ServiceBase[]
                {
                    new AntiTimeOutService()
                };

                foreach (ServiceBase sb in ServicesToRun)
                {
                    sb.CanShutdown = true;
                    sb.CanPauseAndContinue = true;
                }

                ServiceBase.Run(ServicesToRun);
            }           
        }
    }
}
