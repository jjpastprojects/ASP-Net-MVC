using System;
using System.Security.Policy;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using WebAnalyzerScanner.Common;

namespace WebScannerAnalyzer
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            Exception exception = Server.GetLastError();
            Logger.Error(exception);

            Response.Redirect("~/Error");
        }
    }
}
