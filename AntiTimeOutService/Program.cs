using System;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace AntiTimeOutService
{
    static class Program
    {
        const string license = @"
Copyright 2020-2022 rashlight
Licensed under the Apache License, Version 2.0 (the ""License"");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an ""AS IS"" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.";

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
                    case "-l":
                    case "--license":
                    case "/l":
                        AllocConsole();
                        Console.WriteLine(license);
                        Console.Write("\nPress Enter to exit...");
                        Console.ReadLine();
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

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();
    }
}
