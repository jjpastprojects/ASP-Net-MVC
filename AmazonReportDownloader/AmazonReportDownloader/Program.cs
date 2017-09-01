using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;

namespace AmazonReportDownloader
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            log4net.Config.XmlConfigurator.Configure();

#if DEBUG
            Service service = new Service();
            service.Debug();

#else
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[] { new Service() };
            ServiceBase.Run(ServicesToRun);
#endif
        }
    }
}
