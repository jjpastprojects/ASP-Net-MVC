using System.ComponentModel.DataAnnotations;

namespace WebScannerAnalyzer.Entities
{
    public class OrderItem
    {
        public string ID { get; set; }
        public string ASIN { get; set; }
        public string SellerSKU { get; set; }
        public int QuantityOrdered { get; set; }
        public int QuantityShipped { get; set; }
        
        public int TotalQuantity
        {
            get
            {
                int result = 0;
                result += QuantityOrdered;
                result += QuantityShipped;

                return result;
            }
        }
    }

}
