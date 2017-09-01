using System.ComponentModel.DataAnnotations;

namespace WebScannerAnalyzer.Entities
{
    public class AmazonFulfilmentOrderItem
    {
        public string AmazonOrderId { get; set; }
        public string Asin { get; set; }
        public string Sku { get; set; }
        public int Quantity { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
    }

}
