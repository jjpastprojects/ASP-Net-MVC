
namespace WebScannerAnalyzer.Entities
{
    public class ProductSearchParamemters
    {
        public string VendorName { get; set; }
        public string Description { get; set; }
        public int? QuantityInStock { get; set; }
        public string Sku { get; set; }
        public int? CostToBringOver { get; set; }
        public bool IsWatchList { get; set; }
        public int? LimitRecords { get; set; }
        public decimal? MinProfit { get; set; }
        public decimal? MinProfitPecent { get; set; }
        public decimal? Discount { get; set; }
    }
}