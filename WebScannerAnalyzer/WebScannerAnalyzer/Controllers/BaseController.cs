using System.Web.Mvc;
using WebAnalyzerScanner.Common;
using WebScannerAnalyzer.BusinessLayer;
using WebScannerAnalyzer.Interfaces;

namespace WebScannerAnalyzer.Controllers
{
    public class BaseController: Controller
    {
        public IScheduledBusinessLayer ScheduledBusiness
        {
            get
            {
                if (_scheduledBusinessLayer == null)
                    _scheduledBusinessLayer = new ScheduledBusinessLayer();

                return _scheduledBusinessLayer;
            }
        }
        private IScheduledBusinessLayer _scheduledBusinessLayer;

        public IOrderBusinessLayer OrderBusiness
        {
            get
            {
                if (_orderBusinessLayer == null)
                    _orderBusinessLayer = new OrderBusinessLayer();

                return _orderBusinessLayer;
            }
        }
        private IOrderBusinessLayer _orderBusinessLayer;

        public IPricingBusinessLayer PricingBusiness
        {
            get
            {
                if (_pricingBusinessLayer == null)
                    _pricingBusinessLayer = new PricingBusinessLayer();

                return _pricingBusinessLayer;
            }
        }
        private IPricingBusinessLayer _pricingBusinessLayer;

        protected override void OnException(ExceptionContext filterContext)
        {
            if (filterContext.ExceptionHandled)
                return;

            Logger.Error(filterContext.Exception);
            
            if (filterContext.HttpContext.Request.IsAjaxRequest())
            {
                filterContext.HttpContext.Response.StatusCode = 500;
                filterContext.Result = new JsonResult()
                {
                    Data = new {error = filterContext.Exception.ToString()},
                    JsonRequestBehavior = JsonRequestBehavior.AllowGet
                };
            }
            else
            {
                filterContext.Result = RedirectToAction("Index", "Error");
            }

            filterContext.ExceptionHandled = true;
        }
    }
}