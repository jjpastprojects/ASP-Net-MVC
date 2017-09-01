
using System.Collections.Generic;
using System.IO;
using WebScannerAnalyzer.Entities;

namespace WebScannerAnalyzer.Interfaces
{
    public interface IPricingBusinessLayer
    {
        List<Product> SearchProduct(ProductSearchParamemters param);
        object[] BRApply(BRApplyParamemters param);
        object[] ASAApply(BRApplyParamemters param);
        List<MarketPlace> GetAllMarketPlaces();
        List<ProductData> ScanProduct(string marketPlace, Stream fileStream);
        bool DownloadOrders(BRApplyParamemters param);
        bool DeleteInventory(DeleteInventoryParams param);
    }
}
