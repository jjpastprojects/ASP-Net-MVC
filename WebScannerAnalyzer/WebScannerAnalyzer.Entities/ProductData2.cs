
namespace WebScannerAnalyzer.Entities
{
    /// <summary>
    /// Class prepresent the result when scanning items
    /// </summary>
    public class ProductData2 : ProductData
    {
        public int Quantity { get; set; }

        public void SetData(ProductData data2)
        {
            this.InputManufactureName = data2.InputManufactureName;
            this.InputManufacturePart = data2.InputManufacturePart;
            this.InputBarCode = data2.InputBarCode;
            this.InputProductName = data2.InputProductName;
            this.OutputManufactureName = data2.OutputManufactureName;
            this.OutputManufacturePart = data2.OutputManufacturePart;
            this.OutputProductName = data2.OutputProductName;
            this.OutputBarCode = data2.OutputBarCode;
            this.Asin = data2.Asin;
            this.FirstCategory = data2.FirstCategory;
            this.FirstCategoryRank = data2.FirstCategoryRank;
            this.Likes = data2.Likes;
            this.Cost = data2.Cost;
            this.ShippingWeight = data2.ShippingWeight;
            this.AmazoneShippingWeight = data2.AmazoneShippingWeight;
            this.BBPrice = data2.BBPrice;
            this.BBShipping = data2.BBShipping;
            this.BBDelivered = data2.BBDelivered;
            this.Seller = data2.Seller;
            this.SalesValue = data2.SalesValue;
            this.Profit = data2.Profit;
            this.AmazonLink = data2.AmazonLink;
        }
    }

}
