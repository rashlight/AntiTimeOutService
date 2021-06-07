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
                switch (args[0])
                {
                    case "-i":
                        {
                            ManagedInstallerClass.InstallHelper(new string[] { System.Reflection.Assembly.GetExecutingAssembly().Location });
                            break;
                        }
                    case "-u":
                        {
                            ManagedInstallerClass.InstallHelper(new string[] { "/u", System.Reflection.Assembly.GetExecutingAssembly().Location });
                            break;
                        }
                } 
            }
            else
            {
                ServiceBase[] ServicesToRun;

                ServicesToRun = new ServiceBase[]
                {
                    new ATOSvc()
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
