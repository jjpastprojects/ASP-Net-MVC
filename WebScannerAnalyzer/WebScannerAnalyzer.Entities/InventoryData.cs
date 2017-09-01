
namespace WebScannerAnalyzer.Entities
{
    public class InventoryData
    {
        public string ItemID { get; set; }
        public string ItemCode { get; set; }
        public string itemDescription { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public int SQuantity { get; set; }
        public string Manufacturer { get; set; }
        public string AlternateItemCode { get; set; }
        public string UPC { get; set; }
        public string ShippingWeight { get; set; }
        public string ItemLocation { get; set; }
        public string BoxSize { get; set; }
        public int OpenOrders { get; set; }
        public string DontFollow { get; set; }
        public string FollowAsIs { get; set; }
        public decimal DSFee { get; set; }
        public string CasePack { get; set; }
        public string ActualWeight { get; set; }
        public string Map { get; set; }
        public string brand { get; set; }

        // Stock Data
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

        // Asin Data
        public string asin { get; set; }
        public string price { get; set; }
        public int quantity { get; set; }
        public string Business_Price { get; set; }
        public string Quantity_Price_Type { get; set; }
        public string Quantity_Lower_Bound_1 { get; set; }
        public string Quantity_Price_1 { get; set; }
        public string Quantity_Lower_Bound_2 { get; set; }
        public string Quantity_Price_2 { get; set; }
        public string Quantity_Lower_Bound_3 { get; set; }
        public string Quantity_Price_3 { get; set; }
        public string Quantity_Lower_Bound_4 { get; set; }
        public string Quantity_Price_4 { get; set; }
        public string Quantity_Lower_Bound_5 { get; set; }
        public string Quantity_Price_5 { get; set; }

        // Recommended
        public int recommendedQuantity { get; set; }
        public int D7SQuantity { get; set; }
        public int D30SQuantity { get; set; }
        public string Category { get; set; }
        public string CategoryRanking { get; set; }

        public InventoryData()
        {
            ItemID = "";
            ItemCode = "";
            itemDescription = "";
            Price = 0;
            Quantity = 0;
            SQuantity = 0;
            Manufacturer = "";
            AlternateItemCode = "";
            UPC = "";
            ShippingWeight = "";
            ItemLocation = "";
            BoxSize = "";
            OpenOrders = 0;
            DontFollow = "";
            FollowAsIs = "";
            DSFee = 0;
            CasePack = "";
            ActualWeight = "";
            Map = "";
            brand = "";
         
            // Stock Data
            SKU = "";
            Cost = 0;
            SalesValue = 0;
            SellingPrice = 0;
            AmazonWeight = 0;
            Profit = 0;
            AmazonWeightBasedFee = 0;
            Deduct = "";
            Approved = 0;
            isDirty = 0;

            // Asin Data
            asin = "";
            price = "";
            quantity = 0;
            /*Business_Price = "";
            Quantity_Price_Type = "";
            Quantity_Lower_Bound_1 = "";
            Quantity_Price_1 = "";
            Quantity_Lower_Bound_2 = "";
            Quantity_Price_2 = "";
            Quantity_Lower_Bound_3 = "";
            Quantity_Price_3 = "";
            Quantity_Lower_Bound_4 = "";
            Quantity_Price_4 = "";
            Quantity_Lower_Bound_5 = "";
            Quantity_Price_5 = "";
            */
            // Recommended
            recommendedQuantity = 0;
            D7SQuantity = 0;
            D30SQuantity = 0;

            Category = "";
            CategoryRanking = "";
        }
    }
    
}
