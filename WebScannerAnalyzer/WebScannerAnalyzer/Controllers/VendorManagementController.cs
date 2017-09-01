using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using WebScannerAnalyzer.Entities;
using WebScannerAnalyzer.Models;

namespace WebScannerAnalyzer.Controllers
{
    public class VendorManagementController : BaseController
    {
        [HttpGet]
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public JsonResult Search(DataTableParameters param, string vendorName)
        {
            string sortBy = "VendorName";
            string sortDirection = "ASC";

            if (param.Order.Length > 0)
            {
                sortBy = param.Columns[param.Order[0].Column].Name;
                sortDirection = param.Order[0].Dir.ToString();
            }

            int pageSize = param.Length;
            int pageNumber = param.Start/pageSize + 1;

            var results = OrderBusiness.SearchVendor(vendorName, pageNumber, pageSize, sortBy, sortDirection);
            var data = from vendor in results.Data
                select new
                {
                    vendor.VendorId,
                    vendor.VendorName,
                    vendor.ContactPerson,
                    vendor.Email,
                    vendor.DaysToFollow,
                    vendor.IsDropShipper,
                    vendor.Zip,
                    vendor.StreetAddress,
                    vendor.OptionalAddress,
                    vendor.City,
                    vendor.State,
                    vendor.Country,
                    vendor.CanBarcode,
                    vendor.FreeFreightProgram,
                    vendor.DiscountProgram,
                    vendor.WillUseAmazonUPSLabel,
                    vendor.AvgLeadTimeToShip,
                    vendor.AvgDaysInTransit,
                    vendor.AvgDaysToAcceptGoods,
                    vendor.TotalDaysFromOrderToSell,
                    vendor.InventoryFrom
                };

            return Json(new DataTableJsonResult()
            {
                draw = param.Draw,
                recordsTotal = results.TotalRecords,
                recordsFiltered = results.TotalRecords,
                data = data
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var vendor = OrderBusiness.GetVendorById(id.Value);
            if (vendor == null)
                throw new InvalidDataException("Vendor does not exist.");

            return View(vendor);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Vendor vendor)
        {
            if (ModelState.IsValid)
            {
                OrderBusiness.UpdateVendor(vendor);
                return RedirectToAction("Index");
            }

            return View(vendor);
        }
    }
}