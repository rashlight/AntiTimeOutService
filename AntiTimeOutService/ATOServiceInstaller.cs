using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace AntiTimeOutService
{
    [RunInstaller(true)]
    public class ATOServiceInstaller : Installer
    {
        private ServiceInstaller serviceInstaller = new ServiceInstaller();
        private ServiceProcessInstaller processInstaller = new ServiceProcessInstaller();

        public ATOServiceInstaller()
        {
            processInstaller.Account = ServiceAccount.LocalSystem;
            serviceInstaller.StartType = ServiceStartMode.Automatic;

            // Note: Changing these two will requires all ServiceController actions to be changed!
            serviceInstaller.ServiceName = "Anti Time-Out Network Service";
            serviceInstaller.DisplayName = "Anti Time-Out Network Service";

            serviceInstaller.Description = "Monitoring network availibility on custom intervals. " +
                "If this service is stopped, any applications depends on it might not work properly.";

            // Add installers to collection. Order is not important.
            Installers.Add(serviceInstaller);
            Installers.Add(processInstaller);
        }
    }
}
