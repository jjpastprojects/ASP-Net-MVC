using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;
using WebScannerAnalyzer.Entities;

namespace WebScannerAnalyzer.Controllers
{
    public class ScanItemsController : BaseController
    {
        // GET: ScanItems
        public ActionResult Index()
        {
            ViewBag.MarketPlaces = PricingBusiness.GetAllMarketPlaces();
            return View();
        }

        public ActionResult Scan(string marketPlace, HttpPostedFileBase inputFile)
        {
            List<ProductData> products = PricingBusiness.ScanProduct(marketPlace, inputFile.InputStream);
            return Json(products, JsonRequestBehavior.AllowGet);
        }

        public FileResult DownloadTemplate()
        {
            string fileName = "ASA_Template.xlsx";
            byte[] content = System.IO.File.ReadAllBytes(Server.MapPath("~/App_Data/ASA_Template.xlsx"));
            return File(content, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
        }
    }
}