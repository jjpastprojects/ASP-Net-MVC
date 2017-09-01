using System.ComponentModel.DataAnnotations;

namespace WebScannerAnalyzer.Entities
{
    public class AmazonFulfilmentOrder
    {
        public string AmazonOrderId { get; set; }
        public string PurchaseDate { get; set; }
        public string LastUpdateDate { get; set; }
        public string MerchantOrderId { get; set; }
        public string OrderStatus { get; set; }
        public string SalesChannel { get; set; }
        public string Url { get; set; }
        public string FulfillmentChannel { get; set; }
        public string ShipServiceLevel { get; set; }
        public string Ship_City { get; set; }
        public string Ship_State { get; set; }
        public string Ship_PostalCode { get; set; }
        public string Ship_Country { get; set; }
    }

}
