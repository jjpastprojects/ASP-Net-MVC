using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using WebScannerAnalyzer.Entities;
using WebScannerAnalyzer.Interfaces;

namespace WebScannerAnalyzer.DataAccessLayer
{
    public class PricingDataAccessLayer: IPricingDataAccessLayer
    {
        public string ConnectionString
        {
            get { return ConfigurationManager.ConnectionStrings["Repricing"].ConnectionString; }
        }

        public List<Product> SearchProduct(ProductSearchParamemters param)
        {
            List<Product> result = new List<Product>();

            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                using (var command = new SqlCommand())
                {
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "usp_search_product";

                    if (!string.IsNullOrEmpty(param.VendorName))
                        command.Parameters.AddWithValue("@VendorName", param.VendorName);
                    if (param.LimitRecords.HasValue)
                        command.Parameters.AddWithValue("@LimitRecords", param.LimitRecords.Value);
                    if (!string.IsNullOrEmpty(param.Description))
                        command.Parameters.AddWithValue("@Description", param.Description);
                    if (param.MinProfit.HasValue)
                        command.Parameters.AddWithValue("@MinProfit", param.MinProfit.Value);
                    if (param.MinProfitPecent.HasValue)
                        command.Parameters.AddWithValue("@MinProfitPercent", param.MinProfitPecent.Value);
                    if (param.Discount.HasValue)
                        command.Parameters.AddWithValue("@Discount", param.Discount.Value);
                    if (param.QuantityInStock.HasValue)
                        command.Parameters.AddWithValue("@QuanityInStock", param.QuantityInStock.Value);
                    if (param.CostToBringOver.HasValue)
                        command.Parameters.AddWithValue("@CostToBring", param.CostToBringOver.Value);
                    if (!string.IsNullOrEmpty(param.Sku))
                        command.Parameters.AddWithValue("@Sku", param.Sku);
                    command.Parameters.AddWithValue("@IsWatchList", param.IsWatchList);

                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        var product = new Product();

                        product.Asin = (string) reader["Asin"];
                        product.Sku = (string) reader["SKU"];
                        product.SalesValue = Convert.ToDecimal(reader["SalesValue"]);
                        product.Profit = Convert.ToDecimal(reader["Profit"]);
                        product.Seller = (string)reader["Seller"];
                        product.Manufacturer = (string)reader["Manufacturer"];

                        if (!reader.IsDBNull(reader.GetOrdinal("QuantityInStock")))
                            product.QuantityInStock = Convert.ToInt32(reader["QuantityInStock"]);

                        if (!reader.IsDBNull(reader.GetOrdinal("CasePack")))
                            product.CasePack = Convert.ToInt32(reader["CasePack"]);

                        if (!reader.IsDBNull(reader.GetOrdinal("FirstCategory")))
                            product.FirstCategory = (string) reader["FirstCategory"];

                        if (!reader.IsDBNull(reader.GetOrdinal("FirstCategoryRank")))
                            product.FirstCategoryRank = Convert.ToInt32(reader["FirstCategoryRank"]);

                        if (!reader.IsDBNull(reader.GetOrdinal("Cost")))
                            product.Cost = Convert.ToDecimal(reader["Cost"]);

                        if (!reader.IsDBNull(reader.GetOrdinal("BB-Price")))
                            product.BBPrice = Convert.ToDecimal(reader["BB-Price"]);

                        if (!reader.IsDBNull(reader.GetOrdinal("BB-Shipping")))
                            product.BBShipping = Convert.ToDecimal(reader["BB-Shipping"]);

                        if (!reader.IsDBNull(reader.GetOrdinal("BB-Delivered")))
                            product.BBDelivered = Convert.ToDecimal(reader["BB-Delivered"]);

                        if (!reader.IsDBNull(reader.GetOrdinal("Name")))
                            product.Name = (string) reader["Name"];

                        if (!reader.IsDBNull(reader.GetOrdinal("AmazonShippingWeight")))
                            product.AmazonShippingWeight = (string) reader["AmazonShippingWeight"];

                        if (!reader.IsDBNull(reader.GetOrdinal("ShippingWeight")))
                            product.ShippingWeight = (string) reader["ShippingWeight"];

                        if (!reader.IsDBNull(reader.GetOrdinal("AVGRanking")))
                            product.AverageRanking = Convert.ToInt32(reader["AVGRanking"]);

                        if (!reader.IsDBNull(reader.GetOrdinal("Likes")))
                            product.Likes = Convert.ToInt32(reader["Likes"]);
                       
                        result.Add(product);
                    }
                }

                connection.Close();
            }

            return result;
        }
        public List<InventoryData> GetProductsByInventoryForm(ProductSearchParamemters param)
        {
            List<InventoryData> result = new List<InventoryData>();

            Vendor vendor = GetVendorByName(param.VendorName);

            string cmdText = "SELECT * FROM [OrderManager].[dbo].[InventoryMatrix] where Manufacturer = '" + vendor.InventoryFrom + "'";

            string _connectionString = ConfigurationManager.ConnectionStrings["OrderManager"].ConnectionString;
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var command = new SqlCommand(cmdText, connection))
                {
                    command.CommandType = CommandType.Text;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        { // Vendor exists in Vendor table
                            var inventoryData = new InventoryData();

                            inventoryData.ItemID = reader["ItemID"].ToString();
                            inventoryData.ItemCode = reader["ItemCode"].ToString().Replace("\"", "");
                            inventoryData.itemDescription = reader["itemDescription"].ToString();
                            try { inventoryData.Quantity = int.Parse(reader["Quantity"].ToString()); } catch (Exception e) { }
                            inventoryData.Manufacturer = reader["Manufacturer"].ToString();
                            inventoryData.AlternateItemCode = reader["AlternateItemCode"].ToString();
                            inventoryData.UPC = reader["UPC"].ToString();
                            inventoryData.ShippingWeight = reader["ShippingWeight"].ToString();
                            inventoryData.ItemLocation = reader["ItemLocation"].ToString();
                            inventoryData.BoxSize = reader["BoxSize"].ToString();
                            inventoryData.OpenOrders = int.Parse(reader["OpenOrders"].ToString());
                            inventoryData.DontFollow = reader["DontFollow"].ToString();
                            inventoryData.FollowAsIs = reader["FollowAsIs"].ToString();
                            inventoryData.DSFee = decimal.Parse((string)reader["DSFee"].ToString());
                            inventoryData.CasePack = reader["CasePack"].ToString();
                            inventoryData.ActualWeight = reader["ActualWeight"].ToString();
                            inventoryData.Map = reader["Map"].ToString();
                            inventoryData.brand = reader["brand"].ToString();

                            result.Add(inventoryData);
                        }
                    }
                }

                connection.Close();
            }

            return result;
        }//*/

        /*
        public List<ProductData2> GetProductsByInventoryForm(ProductSearchParamemters param)
        {
            List<ProductData2> result = new List<ProductData2>();

            //string cmdText = "SELECT a.* FROM[OrderManager].[dbo].[InventoryMatrix] a inner join[OrderManager].[dbo].[VENDORS] b on a.Manufacturer = b.inventoryFrom where b.VendorName = '" + param.VendorName + "'";
            string cmdText = "SELECT c.ShippingWeight AS _ShippingWeight, c.*, a.* FROM [OrderManager].[dbo].[InventoryMatrix] a inner join[OrderManager].[dbo].[VENDORS] b on a.Manufacturer = b.inventoryFrom LEFT JOIN[REPRICING].[dbo].[AsinsInfo] c ON a.ItemCode=c.SKU OR a.ItemCode=c.ItemModelNumber where b.VendorName = '" + param.VendorName + "'";

            string _connectionString = ConfigurationManager.ConnectionStrings["OrderManager"].ConnectionString;
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var command = new SqlCommand(cmdText, connection))
                {
                    command.CommandType = CommandType.Text;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        { // Vendor exists in Vendor table
                            var data = new ProductData2();

                            data.Asin = reader["Asin"].ToString();
                            data.AmazoneShippingWeight = reader["_ShippingWeight"].ToString();
                            data.FirstCategory = reader["FirstCategory"].ToString();
                            data.FirstCategoryRank = reader["FirstCategoryRank"].ToString();
                            data.ShippingWeight = reader["ShippingWeight"].ToString();
                            data.InputManufactureName = reader["Manufacturer"].ToString();
                            data.OutputBarCode = "";
                            data.OutputManufactureName = "";
                            data.OutputManufacturePart = "";
                            data.OutputProductName = reader["Name"].ToString();
                            data.InputProductName = reader["itemDescription"].ToString();
                            data.AmazonLink = "";
                            data.BBDelivered = "";
                            data.BBPrice = "";
                            data.BBShipping = "";
                            data.Cost = reader["Price"].ToString();
                            data.InputBarCode = "";
                            data.InputManufacturePart = reader["Manufacturer"].ToString();
                            data.Likes = "";
                            data.Profit = "";
                            data.SalesValue = "";
                            data.Seller = "";

                            try { data.Quantity = int.Parse(reader["Quantity"].ToString()); }catch(Exception e) { }

                            result.Add(data);
                        }
                    }
                }

                connection.Close();
            }

            return result;
        }
        //*/
        public Vendor GetVendorByName(string vendorName)
        {
            string cmdText = "SELECT * FROM Vendors WHERE VendorName = '" + vendorName + "'";

            string _connectionString = ConfigurationManager.ConnectionStrings["OrderManager"].ConnectionString;

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var command = new SqlCommand(cmdText, connection))
                {
                    command.CommandType = CommandType.Text;
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        { // Vendor exists in Vendor table
                            var vendor = new Vendor();

                            try
                            {
                                vendor.VendorId = int.Parse(reader["VendorID"].ToString());
                            }
                            catch (Exception e) { }

                            try
                            {
                                vendor.VendorName = reader["VendorName"].ToString();
                            }
                            catch (Exception e) { }

                            try
                            {
                                vendor.InventoryFrom = reader["InventoryFrom"].ToString();
                            }
                            catch (Exception e) { }

                            try
                            {
                                vendor.Prefix = reader["Prefix"].ToString();
                            }
                            catch (Exception e) { }

                            try
                            {
                                vendor.AvgDaysInTransit = int.Parse(reader["AvgDaysInTransit"].ToString());
                            }
                            catch (Exception e) { }
                            try
                            {
                                vendor.AvgLeadTimeToShip = int.Parse(reader["AvgLeadTimeToShip"].ToString());
                            }
                            catch (Exception e) { }
                            try
                            {
                                vendor.AvgDaysToAcceptGoods = int.Parse(reader["AvgDaysToAcceptGoods"].ToString());
                            }
                            catch (Exception e) { }
                            try
                            {
                                vendor.DaysToFollow = int.Parse(reader["DaysToFollow"].ToString());
                            }
                            catch (Exception e) { }
                            try
                            {
                                vendor.PrepFee = Decimal.Parse(reader["PrepFree"].ToString());
                            }
                            catch (Exception e) { vendor.PrepFee = 0; }

                            return vendor;
                        }
                        else
                        { // No vendor in Vendors
                            return null;
                        }
                    }
                }

                connection.Close();
            }

            return null;
        }
        public List<MarketPlace> GetAllMarketPlaces()
        {
            return new List<MarketPlace>()
            {
                new MarketPlace()
                {
                    Id = "US",
                    DisplayName = "USA Market Places (Amazon.com)"
                },
                new MarketPlace()
                {
                    Id = "EU",
                    DisplayName = "EU Market Places (Amazon.EU)",
                },
                new MarketPlace()
                {
                    Id = "CA",
                    DisplayName = "CA Market Places (Amazon.CA)"
                },
                new MarketPlace()
                {
                    Id = "MX",
                    DisplayName = "MX Market Places (Amazon.MX)"
                }
            };
        }

        public double GetSalesValue(string marketPlace, double shippingWeight, double price, string category, string sku)
        {
            string storedProcSuffix = "";
            if (marketPlace.ToUpper() == "EU")
                storedProcSuffix = "_uk";
            else if (marketPlace.ToUpper() == "CA")
                storedProcSuffix = "_ca";
            else if (marketPlace.ToUpper() == "MX")
                storedProcSuffix = "_mx";

            using (var connection = new SqlConnection(ConnectionString))
            {
                using (var command = new SqlCommand())
                {
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = $"dbo.get_sale_value{storedProcSuffix}";

                    command.Parameters.AddWithValue("@shipping_weight", shippingWeight);
                    command.Parameters.AddWithValue("@price", price);
                    command.Parameters.AddWithValue("@category", category);
                    command.Parameters.AddWithValue("@SKU", sku);

                    var returnValue = command.Parameters.Add("@RETURN_VALUE", SqlDbType.Float);
                    returnValue.Direction = ParameterDirection.ReturnValue;
                    
                    connection.Open();
                    command.ExecuteNonQuery();
                    connection.Close();

                    if (returnValue.Value == null)
                        return 0;

                    double salesValue;
                    double.TryParse(returnValue.Value.ToString(), out salesValue);

                    return salesValue;
                }
            }
        }
    }
}
