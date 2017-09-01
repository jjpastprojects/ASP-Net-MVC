
namespace WebScannerAnalyzer.Entities
{
    /// <summary>
    /// Class prepresent the result when scanning items
    /// </summary>
    public class ProductData
    {

        public ProductData()
        {
            InputManufactureName = "";
            InputManufacturePart = "";
            InputBarCode = "";
            InputProductName = "";
            OutputManufactureName = "";
            OutputManufacturePart = "";
            OutputProductName = "";
            OutputBarCode = "";
            Asin = "";
            FirstCategory = "";
            FirstCategoryRank = "";
            Likes = "";
            Cost = "";
            ShippingWeight = "";
            AmazoneShippingWeight = "";
            BBPrice = "";
            BBShipping = "";
            BBDelivered = "";
            Seller = "";
            SalesValue = "";
            Profit = "";
            AmazonLink = "";
        }

        public string InputManufactureName { get; set; }

        public string InputManufacturePart { get; set; }

        public string InputBarCode { get; set; }

        public string InputProductName { get; set; }

        public string OutputManufactureName { get; set; }

        public string OutputManufacturePart { get; set; }

        public string OutputProductName { get; set; }

        public string OutputBarCode { get; set; }

        public string Asin { get; set; }

        public string FirstCategory { get; set; }

        public string FirstCategoryRank { get; set; }

        public string Likes { get; set; }

        public string Cost { get; set; }

        public string ShippingWeight { get; set; }

        public string AmazoneShippingWeight { get; set; }

        public string BBPrice { get; set; }

        public string BBShipping { get; set; }

        public string BBDelivered { get; set; }

        public string Seller { get; set; }

        public string SalesValue { get; set; }

        public string Profit { get; set; }
        public string AmazonLink { get; set; }

    }
    

}
