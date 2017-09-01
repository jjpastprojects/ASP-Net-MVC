
namespace WebScannerAnalyzer.Entities
{
    public class Product
    {
        public string Asin { get; set; }

        public int QuantityInStock { get; set; }

        public string Sku { get; set; }

        public int CasePack { get; set; }

        public string FirstCategory { get; set; }

        public int FirstCategoryRank { get; set; }

        public int Likes { get; set; }

        public string Manufacturer { get; set; }

        public decimal Cost { get; set; }

        public decimal SalesValue { get; set; }

        public decimal Profit { get; set; }

        public decimal BBPrice { get; set; }

        public decimal BBShipping { get; set; }

        public decimal BBDelivered { get; set; }

        public int AverageRanking { get; set; }

        public string Seller { get; set; }

        public string Name { get; set; }

        public string AmazonShippingWeight { get; set; }

        public string ShippingWeight { get; set; }


        //statistics for sold units

        /// <summary>
        /// Show number of units sold in one week
        /// </summary>
        public string OrdersSoldUnitsW { get; set; }

        /// <summary>
        /// Show number of units sold in a month
        /// </summary>
        public string OrdersSoldUnitsM { get; set; }

        /// <summary>
        /// Show number of units sold in a quarter
        /// </summary>
        public string OrdersSoldUnitsQ { get; set; }
        public int QuantityRecommend { get; set; }
    }

   
}
