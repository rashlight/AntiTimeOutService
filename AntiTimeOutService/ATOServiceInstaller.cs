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
            // The services run under the system account.
            processInstaller.Account = ServiceAccount.LocalSystem;

            // The services are started manually.
            serviceInstaller.StartType = ServiceStartMode.Automatic;

            // ServiceName must equal those on ServiceBase derived classes.
            serviceInstaller.ServiceName = "AntiTimeOutService";

            // Add installers to collection. Order is not important.
            Installers.Add(serviceInstaller);
            Installers.Add(processInstaller);
        }
    }
}
