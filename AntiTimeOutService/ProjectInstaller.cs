using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.Threading.Tasks;

namespace AntiTimeOutService
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
        }

        protected override void OnBeforeInstall(IDictionary savedState)
        {
            Console.WriteLine("Installing...");
            string parameter = "CustomSourceName\" \"CustomLogName";
            Context.Parameters["assemblypath"] = "\"" + Context.Parameters["assemblypath"] + "\" \"" + parameter + "\"";
            base.OnBeforeInstall(savedState);
        }

        protected override void OnAfterInstall(IDictionary savedState)
        {
            foreach (var param in Context.Parameters)
            {
                Console.WriteLine("Parameter loaded: " + param);
            }
            Console.WriteLine("Finished, cleaning up...");
            base.OnAfterInstall(savedState);
        }

        protected override void OnAfterUninstall(IDictionary savedState)
        {
            Console.WriteLine("Uninstalling, see you next time...");
            base.OnAfterUninstall(savedState);
        }
    }
}
