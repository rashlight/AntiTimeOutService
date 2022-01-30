using System;
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
                    "\n================ Service install at " + DateTime.Now + " ================\n");
                
                // Available arguments: logtoconsole, assemblypath, logfile
                string[] installerArgs = new string[1] { "/LogFile=install.log" };
                AssemblyInstaller installer = new AssemblyInstaller(System.Reflection.Assembly.GetExecutingAssembly().Location, installerArgs);
                
                // Allow the installer to use text logs
                installer.UseNewContext = true;

                switch (args[0])
                {
                    case "-i":
                    case "--install":
                    case "/i":
                        try
                        {
                            // A service installation must be combined with a commit
                            installer.Install(null);
                            installer.Commit(null);
                            System.IO.File.AppendAllText("install.log", "[OK] Installation success\n");
                        }
                        catch (Exception exp)
                        {
                            System.IO.File.AppendAllText("install.log",
                               "[!!] An error has occurred from " + exp.Source + ":\n" + exp.Message + "\n" + exp.StackTrace + "\n" +
                               "[!!] Error installing service: Operation cannot continue due to an exception\n");
                            throw exp;
                        }
                        break;
                    case "-u":
                    case "--uninstall":
                    case "/u":
                        try
                        {
                            installer.Uninstall(null);
                            System.IO.File.AppendAllText("install.log", "[OK] Uninstallation success\n");
                        }
                        catch (Exception exp)
                        {
                            System.IO.File.AppendAllText("install.log",
                                "[!!] An error has occurred from " + exp.Source + ":\n" + exp.Message + "\n" + exp.StackTrace + "\n" +
                                "[!!] Error uninstalling service: Operation cannot continue due to an exception\n");
                            throw exp;
                        }
                        break;
                    default:
                        System.IO.File.AppendAllText("install.log", "[!!] Error installing service: Invalid arguments\n");
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
