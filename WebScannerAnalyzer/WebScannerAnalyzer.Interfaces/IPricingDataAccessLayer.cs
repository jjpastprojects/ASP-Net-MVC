
using System.Collections.Generic;
using WebScannerAnalyzer.Entities;

namespace WebScannerAnalyzer.Interfaces
{
    public interface IPricingDataAccessLayer
    {
        List<Product> SearchProduct(ProductSearchParamemters param);
        Vendor GetVendorByName(string vendorName);
        List<InventoryData> GetProductsByInventoryForm(ProductSearchParamemters param);
        List<MarketPlace> GetAllMarketPlaces();
        double GetSalesValue(string marketPlace, double shippingWeight, double price, string category, string sku);
    }
}