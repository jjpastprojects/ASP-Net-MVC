using System.ComponentModel.DataAnnotations;

namespace WebScannerAnalyzer.Entities
{
    public class OrderQuantity
    {
        public string ASIN { get; set; }
        public int QuantityOrdered { get; set; }
    }

}
