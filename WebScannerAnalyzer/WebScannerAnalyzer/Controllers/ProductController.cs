using System.Web.Mvc;
using WebScannerAnalyzer.Entities;

namespace WebScannerAnalyzer.Controllers
{
    public class ProductController : BaseController
    {
        // GET: Analysis
        public ActionResult Index()
        {
            ViewBag.VendorNameList = OrderBusiness.GetVendorNameList();
            return View();
        }

        [HttpPost]
        public JsonResult Search(ProductSearchParamemters param)
        {
            var products = PricingBusiness.SearchProduct(param);
            return Json(products, JsonRequestBehavior.AllowGet);
        }
    }
}