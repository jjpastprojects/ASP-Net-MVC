
namespace WebScannerAnalyzer.Entities
{
   
    public class FBAStockData
    {
        public string realItemCode { get; set; }
        public string SKU { get; set; }
        public decimal Cost { get; set; }
        public decimal SalesValue { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal AmazonWeight { get; set; }
        public decimal Profit { get; set; }
        public decimal AmazonWeightBasedFee { get; set; }
        public string Deduct { get; set; }
        public int Approved { get; set; }
        public int isDirty { get; set; }
    }
}
