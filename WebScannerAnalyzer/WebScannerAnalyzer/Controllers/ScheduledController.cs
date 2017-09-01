using System.Web.Mvc;
using WebScannerAnalyzer.Entities;

namespace WebScannerAnalyzer.Controllers
{
    public class ScheduledController : BaseController
    {
        // GET: Analysis
        public ActionResult Index()
        {
            ViewBag.VendorNameList = OrderBusiness.GetVendorNameList();
            ViewBag.MarketPlaces = PricingBusiness.GetAllMarketPlaces();

            return View();
        }
        //*
        public JsonResult DownloadOrders(BRApplyParamemters param)
        {
            var result = PricingBusiness.DownloadOrders(param);

            return Json(result, JsonRequestBehavior.AllowGet);
        }
        //*/
        [HttpPost]
        public JsonResult BRApply(BRApplyParamemters param)
        {
            var result_data = PricingBusiness.BRApply(param);

            return Json(result_data, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public JsonResult ASAApply(BRApplyParamemters param)
        {
            var result_data = PricingBusiness.ASAApply(param);

            return Json(result_data, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult DeleteInventory(DeleteInventoryParams param)
        {
            var delete_res = PricingBusiness.DeleteInventory(param);

            BRApplyParamemters brParam = new BRApplyParamemters();
            brParam.MarketPlace = param.MarketPlace;
            brParam.VendorName = param.VendorName;

            var result_data = PricingBusiness.BRApply(brParam);
            return Json(result_data, JsonRequestBehavior.AllowGet);
        }
    }
}