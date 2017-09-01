using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;
using WebAnalyzerScanner.Common;
using WebScannerAnalyzer.BusinessLayer.AWS;
using WebScannerAnalyzer.DataAccessLayer;
using WebScannerAnalyzer.Entities;
using WebScannerAnalyzer.Interfaces;

namespace WebScannerAnalyzer.BusinessLayer
{
    public class PricingBusinessLayer: IPricingBusinessLayer
    {
        private const string NotFound = "Not Found";

        private IPricingDataAccessLayer _pricingDataAccessLayer;
        private IPricingDataAccessLayer DataAccessLayer
        {
            get
            {
                if (_pricingDataAccessLayer == null)
                    _pricingDataAccessLayer = new PricingDataAccessLayer();

                return _pricingDataAccessLayer;
            }
        }

        public List<Product> SearchProduct(ProductSearchParamemters param)
        {
            return DataAccessLayer.SearchProduct(param);
        }

        private string SearchOrders(string additionalParam, string action,
            AwsSettings awsSettings)
        {
            var serviceName = $"/Orders/";
            string apiVersion = $"2013-09-01";

            var param = $"AWSAccessKeyId=" + awsSettings.AccessKey;
            param += $"&Action={action}";
            param += $"&{additionalParam}";
            param += $"&MarketplaceId.Id.1=" + awsSettings.MarketPlaceId;
            param += $"&SellerId=" + awsSettings.MerchantId;
            param += $"&SignatureMethod=HmacSHA256";
            param += $"&SignatureVersion=2";
            param += $"&Timestamp=" + AwsSettings.TimeStamp;
            param += $"&Version=" + apiVersion;
 
            string signature = AmazonHelper.SignRequest(param, awsSettings.SecretKey, awsSettings.Url,
               serviceName, apiVersion)
               .Replace("=", "%3D").Replace("+", "%2B").Replace("/", "%2F");

            param += "&Signature=" + signature;
            string request = $"https://{awsSettings.Url}{serviceName}{apiVersion}?{param}";
            return AmazonHelper.SendRequest(request);
        }

        public bool DownloadOrders(BRApplyParamemters param)
        {
            Vendor vendor = DataAccessLayer.GetVendorByName(param.VendorName);

            QuantityRecommedFactor factor = GetQuantityRecommendFactor(vendor);

            var awsSettings = AwsSettings.GetSettings(param.MarketPlace);

            var dtToday = DateTime.Now;

            var date0 = dtToday.ToString("dd/MM/yyyy");
            DateTime date = DateTime.ParseExact(date0, "dd/MM/yyyy", null);
            
            var dtFrom = date.AddDays(-1 * factor.HowFarBack);

            // Get Orders Quantity
            List<AmazonFulfilmentOrder> orderList = new List<AmazonFulfilmentOrder>();

            string strDtAfter = StringHelper.EscapeString(dtFrom.ToString("yyyy-MM-ddTHH:mm:ss.sss±hhmm"));
            string strDtBefore = StringHelper.EscapeString(dtToday.ToString("yyyy-MM-ddTHH:mm:ss.sss±hhmm"));

            string serviceName = $"/Orders/";
            
            string param0 = "CreatedAfter=" + StringHelper.EscapeString(dtFrom.ToString("yyyy-MM-dd"));
            param0 += "&CreatedBefore="+ StringHelper.EscapeString(dtToday.ToString("yyyy-MM-dd"));

            string response = SearchOrders(param0, $"ListOrders", awsSettings);

            if (string.IsNullOrEmpty(response))
                return false;
            else
                orderList = ParseListOrdersData(response, $"ListOrdersResult", awsSettings, param.MarketPlace);

            int delayTime = int.Parse(ConfigurationManager.AppSettings["DelayTime"]);
            Thread.Sleep(delayTime);
            // Save AmazonOrderId to AmazonOrderList
            foreach (AmazonFulfilmentOrder order in orderList)
            {
                //string orderDate = dtAfter.ToString("yyyy-MM-dd");

                SaveAmazonOrder(order);

                List<AmazonFulfilmentOrderItem> orderItemList = new List<AmazonFulfilmentOrderItem>();

                orderItemList = GetOrderItemScan(order, awsSettings);

                SaveOrderItem(orderItemList);
            }
           //*/
            return true;
        }
        private void SaveOrderItem(List<AmazonFulfilmentOrderItem> orderItemList)
        {
            foreach (AmazonFulfilmentOrderItem orderItem in orderItemList) {
                string cmdText = "SELECT * FROM [OrderManager].[dbo].[AmazonFulfillmentOrderItem] ";
                cmdText += " WHERE AmazonOrderId='" + orderItem.AmazonOrderId + "'";
                cmdText += " AND Sku='" + orderItem.Sku + "'";

                string connectionString = ConfigurationManager.ConnectionStrings["OrderManager"].ConnectionString;

                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    using (var command = new SqlCommand(cmdText, connection))
                    {
                        command.CommandType = CommandType.Text;
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                using (var conn2 = new SqlConnection(connectionString))
                                {
                                    conn2.Open();

                                    string cmdUpdate = "UPDATE [OrderManager].[dbo].[AmazonFulfillmentOrderItem] SET ";
                                    cmdUpdate += " Asin = '" + orderItem.Asin + "'";
                                    cmdUpdate += ", Sku = '" + orderItem.Sku + "'";
                                    cmdUpdate += ", Quantity =" + orderItem.Quantity;
                                    cmdUpdate += ", ProductName = '" + orderItem.ProductName + "'";
                                    cmdUpdate += ", Price = " + orderItem.Price;
                                    cmdUpdate += " WHERE AmazonOrderId='" + orderItem.AmazonOrderId + "'";

                                    using (var command2 = new SqlCommand(cmdUpdate, conn2))
                                    {
                                        try
                                        {
                                            command2.CommandType = CommandType.Text;
                                            command2.ExecuteNonQuery();
                                        } catch(Exception e) { }
                                    }
                                    conn2.Close();
                                }
                            }
                            else
                            {
                                using (var conn2 = new SqlConnection(connectionString))
                                {
                                    conn2.Open();

                                    string cmdInsert = "INSERT INTO [OrderManager].[dbo].[AmazonFulfillmentOrderItem] (";
                                    cmdInsert += "[AmazonOrderId],[Asin],[Sku],[Quantity],[ProductName],[Price]";
                                    cmdInsert += ") VALUES (";
                                    cmdInsert += "'" + orderItem.AmazonOrderId + "'";
                                    cmdInsert += ", '" + orderItem.Asin + "'";
                                    cmdInsert += ", '" + orderItem.Sku + "'";
                                    cmdInsert += ", " + orderItem.Quantity;
                                    cmdInsert += ", '" + orderItem.ProductName + "'";
                                    cmdInsert += ", " + orderItem.Price;
                                    cmdInsert += ")";

                                    using (var command2 = new SqlCommand(cmdInsert, conn2))
                                    {
                                        try
                                        {
                                            command2.CommandType = CommandType.Text;
                                            command2.ExecuteNonQuery();
                                        }catch(Exception e) { }
                                    }

                                    conn2.Close();
                                }
                            }
                        }
                    }

                    connection.Close();
                }
            }
        }
        private List<AmazonFulfilmentOrderItem> GetOrderItemScan(AmazonFulfilmentOrder order, AwsSettings awsSettings)
        {
            List<AmazonFulfilmentOrderItem> orderItemList = new List<AmazonFulfilmentOrderItem>();

            if (string.IsNullOrEmpty(order.AmazonOrderId))
                return orderItemList;

            var serviceName = $"/Orders/";
            string apiVersion = $"2013-09-01";

            var param = "AWSAccessKeyId=" + awsSettings.AccessKey;
            param += $"&Action=ListOrderItems";
            param += "&AmazonOrderId=" + order.AmazonOrderId;
            param += "&SellerId=" + awsSettings.MerchantId;
            param += "&SignatureMethod=HmacSHA256";
            param += "&SignatureVersion=2";
            param += "&Timestamp=" + AwsSettings.TimeStamp;
            param += "&Version=" + apiVersion;

            var signature = AmazonHelper.SignRequest(param, awsSettings.SecretKey, awsSettings.Url,
                serviceName, apiVersion)
                .Replace("=", "%3D").Replace("+", "%2B").Replace("/", "%2F");

            param += "&Signature=" + signature;
            var request = $"https://{awsSettings.Url}{serviceName}{apiVersion}?{param}";
            var response = AmazonHelper.SendRequest(request);

            if (!string.IsNullOrEmpty(response))
            {
                orderItemList = ParseOrderItemsData(response, $"ListOrderItemsResult");
            }

            var delayTime = int.Parse(ConfigurationManager.AppSettings["DelayTime"]);
            Thread.Sleep(delayTime);

            return orderItemList;
        }
        private List<AmazonFulfilmentOrderItem> ParseOrderItemsData(string responseData, string resultElementName)
        {
            List<AmazonFulfilmentOrderItem> orderItemList = new List<AmazonFulfilmentOrderItem>();

            XDocument xmlDoc = XDocument.Parse(responseData);
            
            if (xmlDoc.Root == null)
                return orderItemList;

            string AmazonOrderId = xmlDoc.Root
            .Elements().Where(x => x.Name.LocalName == resultElementName)
            .Elements().Where(x => x.Name.LocalName == "AmazonOrderId").First().Value;


            IEnumerable<XElement> orderItems = xmlDoc.Root
                .Elements().Where(x => x.Name.LocalName == resultElementName)
                .Elements().Where(x => x.Name.LocalName == "OrderItems")
                .Elements().Where(x => x.Name.LocalName == "OrderItem");

            int itemsCount = orderItems.Count();
            if (itemsCount > 0)
            {
                foreach (XElement item in orderItems)
                {
                    AmazonFulfilmentOrderItem orderItem = new AmazonFulfilmentOrderItem();

                    orderItem.AmazonOrderId = AmazonOrderId;
                    
                    try
                    {
                        XElement nodeASIN = item
                        .Elements().Where(x => x.Name.LocalName == "ASIN")
                        .First();
                        orderItem.Asin = nodeASIN.Value;
                    }
                    catch (Exception e)
                    {
                        orderItem.Asin = "";
                    }
                    try
                    {
                        XElement nodeASIN = item
                        .Elements().Where(x => x.Name.LocalName == "SellerSKU")
                        .First();
                        orderItem.Sku = nodeASIN.Value;
                    }
                    catch (Exception e)
                    {
                        orderItem.Sku = "";
                    }
                    try
                    {
                        XElement nodeASIN = item
                        .Elements().Where(x => x.Name.LocalName == "QuantityOrdered")
                        .First();
                        orderItem.Quantity = int.Parse(nodeASIN.Value.ToString());
                    }
                    catch (Exception e)
                    {
                        orderItem.Quantity = 0;
                    }
                    try
                    {
                        XElement nodeASIN = item
                        .Elements().Where(x => x.Name.LocalName == "Title")
                        .First();
                        orderItem.ProductName = nodeASIN.Value;
                    }
                    catch (Exception e)
                    {
                        orderItem.ProductName = "";
                    }
                    try
                    {
                        XElement nodeASIN = item
                        .Elements().Where(x => x.Name.LocalName == "ItemPrice")
                        .Elements().Where(x => x.Name.LocalName == "Amount")
                        .First();
                        orderItem.Price = decimal.Parse(nodeASIN.Value.ToString());
                    }
                    catch (Exception e)
                    {
                        orderItem.Price = 0;
                    }

                    orderItemList.Add(orderItem);
                }
            }

            return orderItemList;
        }
        private void SaveAmazonOrder(AmazonFulfilmentOrder order)
        {
            string cmdText = "SELECT [AmazonOrderId] FROM [OrderManager].[dbo].[AmazonFulfillmentOrders] ";
            cmdText += " WHERE AmazonOrderId='" + order.AmazonOrderId + "'";

            string connectionString = ConfigurationManager.ConnectionStrings["OrderManager"].ConnectionString;

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (var command = new SqlCommand(cmdText, connection))
                {
                    command.CommandType = CommandType.Text;
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            using (var conn2 = new SqlConnection(connectionString))
                            {
                                conn2.Open();

                                string cmdUpdate = "UPDATE [OrderManager].[dbo].[AmazonFulfillmentOrders] SET ";
                                cmdUpdate += "MerchantOrderId = '" + order.MerchantOrderId + "'";
                                cmdUpdate += ", PurchaseDate = convert(datetime, '" + order.PurchaseDate + "')";
                                cmdUpdate += ", LastUpdateDate = convert(datetime, '" + order.LastUpdateDate + "')";
                                cmdUpdate += ", OrderStatus = '" + order.OrderStatus + "'";
                                cmdUpdate += ", SalesChannel = '" + order.SalesChannel + "'";
                                cmdUpdate += ", Url = '" + order.Url + "'";
                                cmdUpdate += ", FulfillmentChannel = '" + order.FulfillmentChannel + "'";
                                cmdUpdate += ", ShipServiceLevel = '" + order.ShipServiceLevel + "'";
                                cmdUpdate += ", [Ship-City] = '" + order.Ship_City + "'";
                                cmdUpdate += ", [Ship-State] = '" + order.Ship_State + "'";
                                cmdUpdate += ", [Ship-PostalCode] = '" + order.Ship_PostalCode + "'";
                                cmdUpdate += ", [Ship-Country] = '" + order.Ship_Country + "'";
                                cmdUpdate += " WHERE AmazonOrderId = '" + order.AmazonOrderId + "'";

                                
                                using (var command2 = new SqlCommand(cmdUpdate, conn2))
                                {
                                    command2.CommandType = CommandType.Text;
                                    command2.ExecuteNonQuery();
                                }
                                conn2.Close();
                            }
                        }
                        else
                        {
                            using (var conn2 = new SqlConnection(connectionString))
                            {
                                conn2.Open();

                                string cmdInsert = "INSERT INTO [OrderManager].[dbo].[AmazonFulfillmentOrders] (";
                                cmdInsert += "[AmazonOrderId],[MerchantOrderId],[PurchaseDate],[LastUpdateDate],[OrderStatus]";
                                cmdInsert += ",[SalesChannel],[Url],[FulfillmentChannel],[ShipServiceLevel],[Ship-City]";
                                cmdInsert += ",[Ship-State],[Ship-PostalCode],[Ship-Country]";
                                cmdInsert += ") VALUES (";
                                cmdInsert += "'" + order.AmazonOrderId + "'";
                                cmdInsert += ", '" + order.MerchantOrderId + "'";
                                cmdInsert += ", convert(datetime, '" + order.PurchaseDate + "')";
                                cmdInsert += ", convert(datetime, '" + order.LastUpdateDate + "')";
                                cmdInsert += ", '" + order.OrderStatus + "'";
                                cmdInsert += ", '" + order.SalesChannel + "'";
                                cmdInsert += ", '" + order.Url + "'";
                                cmdInsert += ", '" + order.FulfillmentChannel + "'";
                                cmdInsert += ", '" + order.ShipServiceLevel + "'";
                                cmdInsert += ", '" + order.Ship_City + "'";
                                cmdInsert += ", '" + order.Ship_State + "'";
                                cmdInsert += ", '" + order.Ship_PostalCode + "'";
                                cmdInsert += ", '" + order.Ship_Country + "'";
                                cmdInsert += ")";


                                using (var command2 = new SqlCommand(cmdInsert, conn2))
                                {
                                    command2.CommandType = CommandType.Text;
                                    command2.ExecuteNonQuery();
                                }

                                conn2.Close();
                            }
                        }
                    }
                }

                connection.Close();
            }
        }
        private OrderQuantity FindOrderQuantityByAsin(string asin, List<OrderQuantity> orderQuantityList)
        {
            foreach (OrderQuantity orderQuantity in orderQuantityList)
            {
                if (orderQuantity.ASIN.Equals(asin))
                    return orderQuantity;
            }

            return null;
        }
        public bool DeleteInventory(DeleteInventoryParams param)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["AMAZON"].ConnectionString;
            string del_command = "DELETE FROM [OrderManager].[dbo].[InventoryMatrix] WHERE ItemCode = '";
            del_command += param.ItemCode + "'";
            del_command += " AND Manufacturer = '" + param.Manufacturer + "'";

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    using (SqlCommand command = new SqlCommand(del_command, con))
                    {
                        command.ExecuteNonQuery();
                    }
                    con.Close();
                }
            }
            catch (SystemException ex)
            {
                return false;
            }
            return true;
        }
        public object[] BRApply(BRApplyParamemters param)
        {
            Vendor vendor = DataAccessLayer.GetVendorByName(param.VendorName);

            //List<FBAStockData> fbaStockDataList = GetFBAStockList(vendor.Prefix);

            var inventoryProducts = GetInventoryProduct(vendor.InventoryFrom);

            object[] resOrderQuantity;
            resOrderQuantity = GetOrdersQuantity(param.MarketPlace, vendor);
            List<OrderQuantity> pastOrderQuantityList = (List<OrderQuantity>)resOrderQuantity[0];
            QuantityRecommedFactor factor = (QuantityRecommedFactor)resOrderQuantity[1];

            resOrderQuantity = GetDOrdersQuantity(param.MarketPlace, vendor);
            List<OrderQuantity> D7OrderQuantityList = (List<OrderQuantity>)resOrderQuantity[0];
            List<OrderQuantity> D30OrderQuantityList = (List<OrderQuantity>)resOrderQuantity[1];

            decimal freight_cost = 0;
            decimal profit_cost = 0;
            foreach (InventoryData inventory in inventoryProducts)
            {
                try
                {
                    SearchData searchData = new SearchData();
                    searchData.ASIN = inventory.asin;
                    AwsSettings awsSettings = AwsSettings.GetSettings(param.MarketPlace);
                    List<ProductData> products = SearchProductsByAsin(searchData, awsSettings, param.MarketPlace);

                    int delayTime = int.Parse(ConfigurationManager.AppSettings["DelayTime"]);
                    Thread.Sleep(delayTime);

                    if (products.Count > 0)
                    {
                        decimal bbPrice = 0, bbShipping = 0;
                        try
                        {
                            bbPrice = decimal.Parse(products[0].BBPrice);
                        } catch (Exception e) { }
                        try
                        {
                            bbShipping = decimal.Parse(products[0].BBShipping);
                        }
                        catch (Exception e) { }

                        inventory.SellingPrice = bbPrice + bbShipping;
                    }
                } catch (Exception e) { }

                inventory.SQuantity = GetSQuantityFromDB(inventory);

                inventory.Profit = inventory.SalesValue - inventory.Price;

                inventory.recommendedQuantity = 0;

                string asin = inventory.asin;
                if (string.IsNullOrEmpty(asin))
                {
                    inventoryProducts.Remove(inventory);
                    continue;
                }

                OrderQuantity pastOrderQuantity = FindOrderQuantityByAsin(asin, pastOrderQuantityList);

                OrderQuantity d7OrderQuantity = FindOrderQuantityByAsin(asin, D7OrderQuantityList);
                OrderQuantity d30OrderQuantity = FindOrderQuantityByAsin(asin, D30OrderQuantityList);
                
                if (pastOrderQuantity != null && factor.HowFarBack > 0)
                {
                    int pastAmount = pastOrderQuantity.QuantityOrdered;

                    int recommend =  (pastAmount * (factor.HowlongToStockFor + (int)vendor.AvgLeadTimeToShip + (int)vendor.AvgDaysInTransit + (int)vendor.AvgDaysToAcceptGoods) / factor.HowFarBack) - inventory.SQuantity;

                    if (recommend > 0)
                    {
                        inventory.recommendedQuantity = recommend;
                    }
                    try
                    {
                        if ((inventory.Profit / inventory.Cost * 100 ) < factor.MinProfit)
                            inventory.recommendedQuantity = 0;
                    } catch(DivideByZeroException ex) {}
                }

                inventory.D7SQuantity = d7OrderQuantity == null ? 0 : d7OrderQuantity.QuantityOrdered;
                inventory.D30SQuantity = d30OrderQuantity == null ? 0 : d30OrderQuantity.QuantityOrdered;

                //freight_cost += inventory.Price * inventory.recommendedQuantity;
                profit_cost += inventory.Profit * inventory.recommendedQuantity;
            }
            
            object[] res_data = new object[3];

          //  if (freight_cost > vendor.PrepFee)
           //     freight_cost = 0;

            res_data[0] = inventoryProducts;
            res_data[1] = freight_cost;
            res_data[2] = profit_cost;
            return res_data;
        }
        private XElement getElement(XDocument xmlDocument, string elementName)
        {
            var element = xmlDocument.Descendants("items").Elements().Where(x => x.Name == elementName).FirstOrDefault();
            return element != null ? element : null;
        }

        // retrieve attribute by name
        private string attributeValue(XElement item, string attributeName)
        {
            var attribute = item.Attribute(attributeName);
            return attribute != null ? attribute.Value : string.Empty;
        }
        private List<Category> GetCategoryRankingList(string rankings)
        {
            List<Category> categoryList = new List<Category>();

            try
            {
                string strXML = StringHelper.EscapeXMLValue(rankings);

                XDocument xmlDoc = XDocument.Parse(strXML);

                IEnumerable<XElement> categories = xmlDoc.Root
                    //.Elements().Where(x => x.Name.LocalName == "Categories")
                    .Elements().Where(x => x.Name.LocalName == "Category");

                int itemsCount = categories.Count();
                if (itemsCount > 0)
                {
                    foreach (XElement iCategory in categories)
                    {
                        Category category = new Category();

                        category.Name = attributeValue(iCategory, "Name");

                        try
                        {
                            string maxRanking = iCategory.Elements().Where(x => x.Name == "MaxRanking").FirstOrDefault().Value;
                            category.MaxRanking = int.Parse(maxRanking);
                        } catch (Exception e) { }

                        try
                        {
                            IEnumerable<XElement> iRanges0 = iCategory.Elements().Where(x => x.Name == @"Ranges");
                            IEnumerable<XElement> iRanges = iRanges0.Elements()
                                .Where(x => x.Name.LocalName == @"Range");

                            foreach (XElement iRange in iRanges)
                            {
                                CategoryRange range = new CategoryRange();

                                try
                                {
                                    string High = iRange.Elements().Where(x => x.Name == "High").FirstOrDefault().Value;
                                    range.High = int.Parse(High);
                                }
                                catch (Exception e) { }
                                try
                                {
                                    string Low = iRange.Elements().Where(x => x.Name == "Low").FirstOrDefault().Value;
                                    range.Low = int.Parse(Low);
                                }
                                catch (Exception e) { }
                                try
                                {
                                    string QTYToOrder = iRange.Elements().Where(x => x.Name == "QTYToOrder").FirstOrDefault().Value;
                                    range.QTYToOrder = int.Parse(QTYToOrder);
                                }
                                catch (Exception e) { }

                                category.Ranges.Add(range);
                            }
                        } catch (Exception e) { }

                        categoryList.Add(category);
                    }
                }
            }
            catch (Exception e)
            {
                return new List<Category>();
            }


            return categoryList;
        }
        public object[] ASAApply(BRApplyParamemters param)
        {
            Vendor vendor = DataAccessLayer.GetVendorByName(param.VendorName);

            var awsSettings = AwsSettings.GetSettings(param.MarketPlace);

            object[] resOrderQuantity;
            resOrderQuantity = GetOrdersQuantity(param.MarketPlace, vendor);
            List<OrderQuantity> pastOrderQuantityList = (List<OrderQuantity>)resOrderQuantity[0];
            QuantityRecommedFactor factor = (QuantityRecommedFactor)resOrderQuantity[1];

            resOrderQuantity = GetDOrdersQuantity(param.MarketPlace, vendor);
            List<OrderQuantity> D7OrderQuantityList = (List<OrderQuantity>)resOrderQuantity[0];
            List<OrderQuantity> D30OrderQuantityList = (List<OrderQuantity>)resOrderQuantity[1];

            List<Category> categoryList = GetCategoryRankingList(factor.CategoryRankings);

            //////////////////////////////////////
            var totalInventoryProducts = GetASAInventoryProduct(vendor.InventoryFrom);

            List<InventoryData> noAsinInventoryList = new List<InventoryData>();
            List<InventoryData> noAsinInventoryList2 = new List<InventoryData>();
            List<InventoryData> asinInventoryList = new List<InventoryData>();

            foreach (InventoryData item in totalInventoryProducts)
            {
                if (string.IsNullOrEmpty(item.asin))
                    noAsinInventoryList.Add(item);
                else
                    asinInventoryList.Add(item);
            }

            // noAsinInventoryData processing
            foreach (InventoryData item in noAsinInventoryList)
            {
                List<ProductData> products = new List<ProductData>();

                SearchData searchData = new SearchData();
                searchData.Barcode = item.UPC;
                searchData.Cost = item.Price.ToString();
                searchData.ShippingWeight = item.ShippingWeight;
                var productList = SearchProductsByQuery(searchData, awsSettings, param.MarketPlace);
                int delayTime = int.Parse(ConfigurationManager.AppSettings["DelayTime"]);
                Thread.Sleep(delayTime);

                //ProductData product = productList[0];
                foreach (var product in productList)
                {

                    product.FirstCategory = string.IsNullOrEmpty(product.FirstCategory)
                        ? "\\N"
                        : product.FirstCategory.Replace("_display_on_website", "");
                    product.FirstCategoryRank = string.IsNullOrEmpty(product.FirstCategoryRank)
                        ? "-"
                        : product.FirstCategoryRank;

                   
                    bool add = false;
                    if (!string.IsNullOrEmpty(product.InputBarCode))
                    {
                        add = product.OutputBarCode.Equals(product.InputBarCode);
                    }
                    else
                    {
                        add = (product.OutputManufactureName.IndexOf(product.InputManufactureName,
                            StringComparison.OrdinalIgnoreCase) != -1) &&
                              (product.OutputManufacturePart.IndexOf(product.OutputManufacturePart,
                                  StringComparison.OrdinalIgnoreCase) != -1);
                    }

                    if (add)
                    {
                        products.Add(product);
                    }
                }

                /////
                searchData.ManufactureName = item.Manufacturer;
                searchData.ManufacturePart = item.ItemCode;
                searchData.Cost = item.Price.ToString();
                searchData.ShippingWeight = item.ShippingWeight;
                var productList2 = SearchProductsByQuery(searchData, awsSettings, param.MarketPlace);
                delayTime = int.Parse(ConfigurationManager.AppSettings["DelayTime"]);
                Thread.Sleep(delayTime);
                foreach (var product in productList2)
                {
                    product.FirstCategory = string.IsNullOrEmpty(product.FirstCategory)
                        ? "\\N"
                        : product.FirstCategory.Replace("_display_on_website", "");
                    product.FirstCategoryRank = string.IsNullOrEmpty(product.FirstCategoryRank)
                        ? "-"
                        : product.FirstCategoryRank;

                    bool add = false;
                    if (!string.IsNullOrEmpty(product.InputBarCode))
                    {
                        add = product.OutputBarCode.Equals(product.InputBarCode);
                    }
                    else
                    {
                        add = (product.OutputManufactureName.IndexOf(product.InputManufactureName,
                            StringComparison.OrdinalIgnoreCase) != -1) &&
                              (product.OutputManufacturePart.IndexOf(product.OutputManufacturePart,
                                  StringComparison.OrdinalIgnoreCase) != -1);
                    }

                    if (add)
                    {
                        products.Add(product);
                    }
                }
                ///////////
                foreach (ProductData selProduct in products)
                {
                    InventoryData newInventory = new InventoryData();

                    newInventory.asin = selProduct.Asin;
                    newInventory.SKU = item.ItemCode;
                    newInventory.itemDescription = selProduct.OutputProductName;
                    newInventory.Price = item.Price;
                    newInventory.Quantity = item.Quantity;
                    newInventory.Manufacturer = selProduct.OutputManufactureName;
                    newInventory.ShippingWeight = selProduct.ShippingWeight;
                    newInventory.DSFee = item.DSFee;
                    newInventory.CasePack = item.CasePack;
                    newInventory.ActualWeight = item.ActualWeight;
                    newInventory.Category = selProduct.FirstCategory;
                    newInventory.CategoryRanking = selProduct.FirstCategoryRank;

                    try
                    {
                        newInventory.AmazonWeight = decimal.Parse(selProduct.AmazoneShippingWeight);
                    }
                    catch (Exception e)
                    {
                        newInventory.AmazonWeight = 0;
                    }

                    newInventory.ShippingWeight = (selProduct.ShippingWeight);
                    try
                    {
                        newInventory.SalesValue = decimal.Parse(selProduct.SalesValue);
                    }
                    catch (Exception e)
                    {
                        newInventory.SalesValue = 0;
                    }
                    {
                        decimal bbPrice = 0, bbShipping = 0;
                        try
                        {
                            bbPrice = decimal.Parse(selProduct.BBPrice);
                        }
                        catch (Exception e) { }
                        try
                        {
                            bbShipping = decimal.Parse(selProduct.BBShipping);
                        }
                        catch (Exception e) { }

                        newInventory.SellingPrice = bbPrice + bbShipping;
                    }

                    newInventory.SQuantity = GetSQuantityFromDB(newInventory);

                    newInventory.recommendedQuantity = GetSQuantityByCategory(selProduct.FirstCategory, selProduct.FirstCategoryRank, categoryList);

                    newInventory.Profit = newInventory.SalesValue - newInventory.Price;

                    string asin = newInventory.asin;
                    if (string.IsNullOrEmpty(asin))
                    {
                        continue;
                    }

                    OrderQuantity d7OrderQuantity = FindOrderQuantityByAsin(asin, D7OrderQuantityList);
                    OrderQuantity d30OrderQuantity = FindOrderQuantityByAsin(asin, D30OrderQuantityList);

                    newInventory.D7SQuantity = d7OrderQuantity == null ? 0 : d7OrderQuantity.QuantityOrdered;
                    newInventory.D30SQuantity = d30OrderQuantity == null ? 0 : d30OrderQuantity.QuantityOrdered;

                    noAsinInventoryList2.Add(newInventory);
                }
            }

            foreach (InventoryData item in asinInventoryList)
            {
                item.SKU = item.ItemCode;

                try
                {
                    SearchData searchData = new SearchData();
                    searchData.ASIN = item.asin;
                    awsSettings = AwsSettings.GetSettings(param.MarketPlace);
                    List<ProductData> products = SearchProductsByAsin(searchData, awsSettings, param.MarketPlace);

                    int delayTime = int.Parse(ConfigurationManager.AppSettings["DelayTime"]);
                    Thread.Sleep(delayTime);

                    if (products.Count > 0)
                    {
                        decimal bbPrice = 0, bbShipping = 0;
                        try
                        {
                            bbPrice = decimal.Parse(products[0].BBPrice);
                        }
                        catch (Exception e) { }
                        try
                        {
                            bbShipping = decimal.Parse(products[0].BBShipping);
                        }
                        catch (Exception e) { }

                        item.SellingPrice = bbPrice + bbShipping;

                        item.SalesValue = decimal.Parse(products[0].SalesValue);
                        item.Price = decimal.Parse(products[0].Cost);
                    }
                }
                catch (Exception e) { }

                item.SQuantity = GetSQuantityFromDB(item);

                item.Profit = item.SalesValue - item.Price;

                item.recommendedQuantity = 0;

                string asin = item.asin;
                if (string.IsNullOrEmpty(asin))
                {
                    asinInventoryList.Remove(item);
                    continue;
                }

                OrderQuantity pastOrderQuantity = FindOrderQuantityByAsin(asin, pastOrderQuantityList);

                OrderQuantity d7OrderQuantity = FindOrderQuantityByAsin(asin, D7OrderQuantityList);
                OrderQuantity d30OrderQuantity = FindOrderQuantityByAsin(asin, D30OrderQuantityList);

                if (pastOrderQuantity != null && factor.HowFarBack > 0)
                {
                    int pastAmount = pastOrderQuantity.QuantityOrdered;

                    int recommend = (pastAmount * (factor.HowlongToStockFor + (int)vendor.AvgLeadTimeToShip + (int)vendor.AvgDaysInTransit + (int)vendor.AvgDaysToAcceptGoods) / factor.HowFarBack) - item.SQuantity;

                    if (recommend > 0)
                    {
                        item.recommendedQuantity = recommend;
                    }
                    try
                    {
                        if ((item.Profit / item.Cost * 100) < factor.MinProfit)
                            item.recommendedQuantity = 0;
                    }
                    catch (DivideByZeroException ex) { }
                }

                item.D7SQuantity = d7OrderQuantity == null ? 0 : d7OrderQuantity.QuantityOrdered;
                item.D30SQuantity = d30OrderQuantity == null ? 0 : d30OrderQuantity.QuantityOrdered;
            }
            // asinInventoryData Processing

            totalInventoryProducts.Clear();
            totalInventoryProducts.AddRange(asinInventoryList);
            totalInventoryProducts.AddRange(noAsinInventoryList2);

            decimal freight_cost = 0;
            decimal profit_cost = 0;
            foreach (InventoryData inventory in totalInventoryProducts)
            {
                profit_cost += inventory.Profit * inventory.recommendedQuantity;
            }

            object[] res_data = new object[3];

            if (freight_cost > vendor.PrepFee)
                freight_cost = 0;

            res_data[0] = totalInventoryProducts;
            res_data[1] = freight_cost;
            res_data[2] = profit_cost;
            return res_data;
        }
        private int GetSQuantityByCategory(string category, string categoryRank, List<Category> categoryList)
        {
            try
            {
                int rank = int.Parse(categoryRank);

                foreach (Category categoryItem in categoryList)
                {
                    if (category.Equals(categoryItem.Name))
                    {
                        foreach (CategoryRange range in categoryItem.Ranges)
                        {
                            if ((rank >= range.High) && (rank <= range.Low))
                                return range.QTYToOrder;
                        }

                        return 0;
                    }
                }
            } catch (Exception e)
            {
                return 0;
            }
            return 0;
        }
        private string GetRealItemCode(string itemCode)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["AMAZON"].ConnectionString;
            using (var connection = new SqlConnection(connectionString))
            {
                using (var command = new SqlCommand())
                {
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "dbo.get_real_item_code";

                    command.Parameters.AddWithValue("@item_code", itemCode);

                    var returnValue = command.Parameters.Add("@RETURN_VALUE", SqlDbType.VarChar);
                    returnValue.Direction = ParameterDirection.ReturnValue;

                    connection.Open();
                    command.ExecuteNonQuery();
                    connection.Close();

                    var result = (string)returnValue.Value;
                    return result;
                }
            }
        }

        public List<InventoryData> GetASAInventoryProduct(string inventoryForm)
        {
            List<InventoryData> result = new List<InventoryData>();


            string cmdText = "SELECT TOP (20) a.*, null as AmazonWeight, null as AmazonWeightBasedFee, '' as asin  FROM[OrderManager].[dbo].[InventoryMatrix] a";
                          cmdText += "  WHERE a.Manufacturer = '" + inventoryForm + "'" + " ";
                          cmdText += "UNION ALL ";
                          cmdText += "SELECT TOP (20) a.*, b.AmazonWeight, b.AmazonWeightBasedFee, c.asin ";
                          cmdText += " FROM [OrderManager].[dbo].[InventoryMatrix] a";
                          cmdText += " INNER JOIN [OrderManager].[dbo].[FBAStock] b ON a.ItemCode = dbo.get_real_item_code(b.SKU)";
                          cmdText += " INNER JOIN [AMAZON].[dbo].[AMAZON_ASINS] c ON a.ItemCode=dbo.get_real_item_code(c.sku)";
                          cmdText += " WHERE a.Manufacturer='" + inventoryForm + "'";

            string _connectionString = ConfigurationManager.ConnectionStrings["AMAZON"].ConnectionString;
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var command = new SqlCommand(cmdText, connection))
                {
                    command.CommandType = CommandType.Text;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var product = new InventoryData();

                            product.SQuantity = 0;
                            try
                            {
                                product.ItemID = reader["ItemID"].ToString();
                            }
                            catch (Exception e)
                            {
                                product.ItemID = "";
                            }
                            try
                            {
                                product.ItemCode = reader["ItemCode"].ToString();
                            }
                            catch (Exception e)
                            {
                                product.ItemCode = "";
                            }
                            try
                            {
                                product.itemDescription = reader["itemDescription"].ToString();
                            }
                            catch (Exception e)
                            {
                                product.itemDescription = "";
                            }
                            try
                            {
                                product.Price = decimal.Parse(reader["Price"].ToString());
                            }
                            catch (Exception e)
                            {
                                product.Price = 0;
                            }
                            try
                            {
                                product.Quantity = int.Parse(reader["Quantity"].ToString());
                            }
                            catch (Exception e)
                            {
                                product.Quantity = 0;
                            }
                            try
                            {
                                product.quantity = int.Parse(reader["quantity"].ToString());
                            }
                            catch (Exception e)
                            {
                                product.quantity = 0;
                            }
                            try
                            {
                                product.Manufacturer = reader["Manufacturer"].ToString();
                            }
                            catch (Exception e)
                            {
                                product.Manufacturer = "";
                            }
                            try
                            {
                                product.AlternateItemCode = reader["AlternateItemCode"].ToString();
                            }
                            catch (Exception e)
                            {
                                product.AlternateItemCode = "";
                            }
                            try
                            {
                                product.UPC = reader["UPC"].ToString();
                            }
                            catch (Exception e)
                            {
                                product.UPC = "";
                            }
                            try
                            {
                                product.ShippingWeight = reader["ShippingWeight"].ToString();
                            }
                            catch (Exception e)
                            {
                                product.ShippingWeight = "";
                            }
                            try
                            {
                                product.ItemLocation = reader["ItemLocation"].ToString();

                            }
                            catch (Exception e)
                            {
                                product.ItemLocation = "";
                            }
                            try
                            {
                                product.BoxSize = reader["BoxSize"].ToString();

                            }
                            catch (Exception e)
                            {
                                product.BoxSize = "";
                            }
                            try
                            {
                                product.OpenOrders = int.Parse(reader["OpenOrders"].ToString());

                            }
                            catch (Exception e)
                            {
                                product.OpenOrders = 0;
                            }
                            try
                            {
                                product.DontFollow = reader["DontFollow"].ToString();

                            }
                            catch (Exception e)
                            {
                                product.DontFollow = "";
                            }
                            try
                            {
                                product.FollowAsIs = reader["FollowAsIs"].ToString();

                            }
                            catch (Exception e)
                            {
                                product.FollowAsIs = "";
                            }
                            try
                            {
                                product.DSFee = decimal.Parse(reader["DSFee"].ToString());

                            }
                            catch (Exception e)
                            {
                                product.DSFee = 0;
                            }
                            try
                            {
                                product.CasePack = reader["CasePack"].ToString();


                            }
                            catch (Exception e)
                            {
                                product.CasePack = "";
                            }
                            try
                            {
                                product.ActualWeight = reader["ActualWeight"].ToString();

                            }
                            catch (Exception e)
                            {
                                product.ActualWeight = "";
                            }
                            try
                            {
                                product.Map = reader["Map"].ToString();

                            }
                            catch (Exception e)
                            {
                                product.Map = "";
                            }
                            try
                            {
                                product.brand = reader["brand"].ToString();

                            }
                            catch (Exception e)
                            {
                                product.brand = "";
                            }

                            // AsinData
                            try
                            {
                                product.asin = reader["asin"].ToString();
                            }
                            catch (Exception e)
                            {
                                product.asin = "";
                            }
                            try
                            {
                                product.price = reader["price"].ToString();
                            }
                            catch (Exception e)
                            {
                                product.price = "";
                            }
                           /* try
                            {
                                product.Business_Price = reader["Business Price"].ToString();
                            }
                            catch (Exception e)
                            {
                                product.Business_Price = "";
                            }
                            try
                            {
                                product.Quantity_Price_Type = reader["Quantity Price Type"].ToString();
                            }
                            catch (Exception e)
                            {
                                product.Quantity_Price_Type = "";
                            }
                            try
                            {
                                product.Quantity_Lower_Bound_1 = reader["Quantity Lower Bound 1"].ToString();
                            }
                            catch (Exception e)
                            {
                                product.Quantity_Lower_Bound_1 = "";
                            }
                            try
                            {
                                product.Quantity_Price_1 = reader["Quantity Price 1"].ToString();
                            }
                            catch (Exception e)
                            {
                                product.Quantity_Price_1 = "";
                            }
                            try
                            {
                                product.Quantity_Lower_Bound_2 = reader["Quantity Lower Bound 2"].ToString();
                            }
                            catch (Exception e)
                            {
                                product.Quantity_Lower_Bound_2 = "";
                            }
                            try
                            {
                                product.Quantity_Price_2 = reader["Quantity Price 2"].ToString();
                            }
                            catch (Exception e)
                            {
                                product.Quantity_Price_2 = "";
                            }
                            try
                            {
                                product.Quantity_Lower_Bound_3 = reader["Quantity Lower Bound 3"].ToString();
                            }
                            catch (Exception e)
                            {
                                product.Quantity_Lower_Bound_3 = "";
                            }
                            try
                            {
                                product.Quantity_Price_3 = reader["Quantity Price 3"].ToString();
                            }
                            catch (Exception e)
                            {
                                product.Quantity_Price_3 = "";
                            }
                            try
                            {
                                product.Quantity_Lower_Bound_4 = reader["Quantity Lower Bound 4"].ToString();
                            }
                            catch (Exception e)
                            {
                                product.Quantity_Lower_Bound_4 = "";
                            }
                            try
                            {
                                product.Quantity_Price_4 = reader["Quantity Price 4"].ToString();
                            }
                            catch (Exception e)
                            {
                                product.Quantity_Price_4 = "";
                            }
                            try
                            {
                                product.Quantity_Lower_Bound_5 = reader["Quantity Lower Bound 5"].ToString();
                            }
                            catch (Exception e)
                            {
                                product.Quantity_Lower_Bound_5 = "";
                            }
                            try
                            {
                                product.Quantity_Price_5 = reader["Quantity Price 5"].ToString();
                            }
                            catch (Exception e)
                            {
                                product.Quantity_Price_5 = "";
                            }
                            */

                            // Stock Data
                            try
                            {
                                product.SKU = "";
                                //product.SKU = reader["sku"].ToString();
                            }
                            catch (Exception e)
                            {
                                product.SKU = "";
                            }
                            try
                            {
                                //product.Cost = decimal.Parse(reader["Cost"].ToString());
                                product.Cost = decimal.Parse(reader["price"].ToString());
                            }
                            catch (Exception e)
                            {
                                product.Cost = 0;
                            }
                            try
                            {
                                //We need to read that sp get_fba_listing_sale_value (after calling the MWS to get selling price + category)
                                //product.SalesValue = decimal.Parse(reader["SalesValue"].ToString());
                                //We may actually need to update the value for it in the calling function to this...after calling the MWS API
                                product.SalesValue = 0;
                            }
                            catch (Exception e)
                            {
                                product.SalesValue = 0;
                            }
                            try
                            {
                                //We need to read that from MWS call - For now keeping it at 0
                                product.SellingPrice = 0;
                                ////We may actually need to update the value for it in the calling function to this...after calling the MWS API as 
                                //we can't update to real value just from our DB...
                                //product.SellingPrice = decimal.Parse(reader["SellingPrice"].ToString());
                            }
                            catch (Exception e)
                            {
                                product.SellingPrice = 0;
                            }
                            try
                            {
                                product.AmazonWeight = decimal.Parse(reader["AmazonWeight"].ToString());
                            }
                            catch (Exception e)
                            {
                                product.AmazonWeight = 0;
                            }
                            try
                            {
                                product.Profit = product.SellingPrice - product.SalesValue;
                            }
                            catch (Exception e)
                            {
                                product.Profit = 0;
                            }
                            try
                            {
                                product.AmazonWeightBasedFee = decimal.Parse(reader["AmazonWeightBasedFee"].ToString());
                            }
                            catch (Exception e)
                            {
                                product.AmazonWeightBasedFee = 0;
                            }
                            /*try
                            {
                                product.Approved = int.Parse(reader["Approved"].ToString());
                            }
                            catch (Exception e)
                            {
                                product.Approved = 0;
                            }
                            try
                            {
                                product.isDirty = int.Parse(reader["isDirty"].ToString());

                            }
                            catch (Exception e)
                            {
                                product.isDirty = 0;
                            }

                            try
                            {
                                product.Deduct = reader["Deduct"].ToString();
                            }
                            catch (Exception e)
                            {
                                product.Deduct = "";
                            }
                            */
                            result.Add(product);
                        }
                    }
                }

                connection.Close();
            }

            return result;
        }
        public List<InventoryData> GetInventoryProduct(string inventoryForm)
        {
            List<InventoryData> result = new List<InventoryData>();

            string cmdText = "SELECT a.*, b.*, c.* ";
            cmdText += " FROM [OrderManager].[dbo].[InventoryMatrix] a";
            cmdText += " INNER JOIN [OrderManager].[dbo].[FBAStock] b ON a.ItemCode = dbo.get_real_item_code(b.SKU)";
            cmdText += " INNER JOIN [AMAZON].[dbo].[AMAZON_ASINS] c ON a.ItemCode=dbo.get_real_item_code(c.sku)";
            cmdText += " WHERE a.Manufacturer='" + inventoryForm + "'";

            string _connectionString = ConfigurationManager.ConnectionStrings["AMAZON"].ConnectionString;
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var command = new SqlCommand(cmdText, connection))
                {
                    command.CommandType = CommandType.Text;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var product = new InventoryData();

                            product.SQuantity = 0;
                            try
                            {
                                product.ItemID = reader["ItemID"].ToString();
                            }
                            catch (Exception e)
                            {
                                product.ItemID = "";
                            }
                            try
                            {
                                product.ItemCode = reader["ItemCode"].ToString();
                            }
                            catch (Exception e)
                            {
                                product.ItemCode = "";
                            }
                            try
                            {
                                product.itemDescription = reader["itemDescription"].ToString();
                            }
                            catch (Exception e)
                            {
                                product.itemDescription = "";
                            }
                            try
                            {
                                product.Price = decimal.Parse(reader["Price"].ToString());
                            }
                            catch (Exception e)
                            {
                                product.Price = 0;
                            }
                            try
                            {
                                product.Quantity = int.Parse(reader["Quantity"].ToString());
                            }
                            catch (Exception e)
                            {
                                product.Quantity = 0;
                            }
                            try
                            {
                                product.quantity = int.Parse(reader["quantity"].ToString());
                            }
                            catch (Exception e)
                            {
                                product.quantity = 0;
                            }
                            try
                            {
                                product.Manufacturer = reader["Manufacturer"].ToString();
                            }
                            catch (Exception e)
                            {
                                product.Manufacturer = "";
                            }
                            try
                            {
                                product.AlternateItemCode = reader["AlternateItemCode"].ToString();
                            }
                            catch (Exception e)
                            {
                                product.AlternateItemCode = "";
                            }
                            try
                            {
                                product.UPC = reader["UPC"].ToString();
                            }
                            catch (Exception e)
                            {
                                product.UPC = "";
                            }
                            try
                            {
                                product.ShippingWeight = reader["ShippingWeight"].ToString();
                            }
                            catch (Exception e)
                            {
                                product.ShippingWeight = "";
                            }
                            try
                            {
                                product.ItemLocation = reader["ItemLocation"].ToString();

                            }
                            catch (Exception e)
                            {
                                product.ItemLocation = "";
                            }
                            try
                            {
                                product.BoxSize = reader["BoxSize"].ToString();

                            }
                            catch (Exception e)
                            {
                                product.BoxSize = "";
                            }
                            try
                            {
                                product.OpenOrders = int.Parse(reader["OpenOrders"].ToString());

                            }
                            catch (Exception e)
                            {
                                product.OpenOrders = 0;
                            }
                            try
                            {
                                product.DontFollow = reader["DontFollow"].ToString();

                            }
                            catch (Exception e)
                            {
                                product.DontFollow = "";
                            }
                            try
                            {
                                product.FollowAsIs = reader["FollowAsIs"].ToString();

                            }
                            catch (Exception e)
                            {
                                product.FollowAsIs = "";
                            }
                            try
                            {
                                product.DSFee = decimal.Parse(reader["DSFee"].ToString());

                            }
                            catch (Exception e)
                            {
                                product.DSFee = 0;
                            }
                            try
                            {
                                product.CasePack = reader["CasePack"].ToString();


                            }
                            catch (Exception e)
                            {
                                product.CasePack = "";
                            }
                            try
                            {
                                product.ActualWeight = reader["ActualWeight"].ToString();

                            }
                            catch (Exception e)
                            {
                                product.ActualWeight = "";
                            }
                            try
                            {
                                product.Map = reader["Map"].ToString();

                            }
                            catch (Exception e)
                            {
                                product.Map = "";
                            }
                            try
                            {
                                product.brand = reader["brand"].ToString();

                            }
                            catch (Exception e)
                            {
                                product.brand = "";
                            }

                            // AsinData
                            try
                            {
                                product.asin = reader["asin"].ToString();
                            }
                            catch (Exception e)
                            {
                                product.asin = "";
                            }
                            try
                            {
                                product.price = reader["price"].ToString();
                            }
                            catch (Exception e)
                            {
                                product.price = "";
                            }
                            try
                            {
                                product.Business_Price = reader["Business Price"].ToString();
                            }
                            catch (Exception e)
                            {
                                product.Business_Price = "";
                            }
                            try
                            {
                                product.Quantity_Price_Type = reader["Quantity Price Type"].ToString();
                            }
                            catch (Exception e)
                            {
                                product.Quantity_Price_Type = "";
                            }
                            try
                            {
                                product.Quantity_Lower_Bound_1 = reader["Quantity Lower Bound 1"].ToString();
                            }
                            catch (Exception e)
                            {
                                product.Quantity_Lower_Bound_1 = "";
                            }
                            try
                            {
                                product.Quantity_Price_1 = reader["Quantity Price 1"].ToString();
                            }
                            catch (Exception e)
                            {
                                product.Quantity_Price_1 = "";
                            }
                            try
                            {
                                product.Quantity_Lower_Bound_2 = reader["Quantity Lower Bound 2"].ToString();
                            }
                            catch (Exception e)
                            {
                                product.Quantity_Lower_Bound_2 = "";
                            }
                            try
                            {
                                product.Quantity_Price_2 = reader["Quantity Price 2"].ToString();
                            }
                            catch (Exception e)
                            {
                                product.Quantity_Price_2 = "";
                            }
                            try
                            {
                                product.Quantity_Lower_Bound_3 = reader["Quantity Lower Bound 3"].ToString();
                            }
                            catch (Exception e)
                            {
                                product.Quantity_Lower_Bound_3 = "";
                            }
                            try
                            {
                                product.Quantity_Price_3 = reader["Quantity Price 3"].ToString();
                            }
                            catch (Exception e)
                            {
                                product.Quantity_Price_3 = "";
                            }
                            try
                            {
                                product.Quantity_Lower_Bound_4 = reader["Quantity Lower Bound 4"].ToString();
                            }
                            catch (Exception e)
                            {
                                product.Quantity_Lower_Bound_4 = "";
                            }
                            try
                            {
                                product.Quantity_Price_4 = reader["Quantity Price 4"].ToString();
                            }
                            catch (Exception e)
                            {
                                product.Quantity_Price_4 = "";
                            }
                            try
                            {
                                product.Quantity_Lower_Bound_5 = reader["Quantity Lower Bound 5"].ToString();
                            }
                            catch (Exception e)
                            {
                                product.Quantity_Lower_Bound_5 = "";
                            }
                            try
                            {
                                product.Quantity_Price_5 = reader["Quantity Price 5"].ToString();
                            }
                            catch (Exception e)
                            {
                                product.Quantity_Price_5 = "";
                            }


                            // Stock Data
                            try
                            {
                                product.SKU = reader["SKU"].ToString();
                            }
                            catch (Exception e)
                            {
                                product.SKU = "";
                            }
                            try
                            {
                                product.Cost = decimal.Parse(reader["Cost"].ToString());
                            }
                            catch (Exception e)
                            {
                                product.Cost = 0;
                            }
                            try
                            {
                                product.SalesValue = decimal.Parse(reader["SalesValue"].ToString());
                            }
                            catch (Exception e)
                            {
                                product.SalesValue = 0;
                            }
                            try
                            {
                                product.SellingPrice = decimal.Parse(reader["SellingPrice"].ToString());
                            }
                            catch (Exception e)
                            {
                                product.SellingPrice = 0;
                            }
                            try
                            {
                                product.AmazonWeight = decimal.Parse(reader["AmazonWeight"].ToString());
                            }
                            catch (Exception e)
                            {
                                product.AmazonWeight = 0;
                            }
                            try
                            {
                                product.Profit = decimal.Parse(reader["Profit"].ToString());
                            }
                            catch (Exception e)
                            {
                                product.Profit = 0;
                            }
                            try
                            {
                                product.AmazonWeightBasedFee = decimal.Parse(reader["AmazonWeightBasedFee"].ToString());
                            }
                            catch (Exception e)
                            {
                                product.AmazonWeightBasedFee = 0;
                            }
                            try
                            {
                                product.Approved = int.Parse(reader["Approved"].ToString());
                            }
                            catch (Exception e)
                            {
                                product.Approved = 0;
                            }
                            try
                            {
                                product.isDirty = int.Parse(reader["isDirty"].ToString());

                            }
                            catch (Exception e)
                            {
                                product.isDirty = 0;
                            }

                            try
                            {
                                product.Deduct = reader["Deduct"].ToString();
                            }
                            catch (Exception e)
                            {
                                product.Deduct = "";
                            }

                            result.Add(product);
                        }
                    }
                }

                connection.Close();
            }

            return result;
        }
        /*
        private int GetSQuantityFromDB(InventoryData inventory)
        {
            int result = 0;

            string cmdText = "SELECT dbo.get_fba_listing_sale_value(";
            cmdText += inventory.ShippingWeight;
            cmdText += ", " + inventory.Price;
            cmdText += ", 'home'";
            cmdText += ", '" + inventory.SKU + "')";

            string _connectionString = ConfigurationManager.ConnectionStrings["REPRICING"].ConnectionString;
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var command = new SqlCommand(cmdText, connection))
                {
                    command.CommandType = CommandType.Text;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            try
                            {
                                result = int.Parse((string)reader["ItemID"]);
                            }
                            catch (Exception e)
                            {
                                result = 0;
                            }
                        }
                    }
                }

                connection.Close();
            }

            return result;
        }
        //*/
        
        private int GetSQuantityFromDB(InventoryData inventory)
        {
            int result = 0;

            string cmdText = "SELECT [QuantityAvailable] FROM [AMAZON].[dbo].[FBAInventory] ";
            cmdText += " WHERE Asin='" + inventory.asin + "'";

            string _connectionString = ConfigurationManager.ConnectionStrings["REPRICING"].ConnectionString;
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var command = new SqlCommand(cmdText, connection))
                {
                    command.CommandType = CommandType.Text;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            try
                            {
                                result = int.Parse(reader["QuantityAvailable"].ToString());
                            }
                            catch (Exception e)
                            {
                                result = 0;
                            }
                        }
                    }
                }

                connection.Close();
            }

            return result;
        }

        public List<Product> GetFbaProduct(string prefix)
        {
            List<Product> result = new List<Product>();

            string cmdText = "SELECT ";
            cmdText += " a.[SKU]";
            cmdText += " , a.[Cost]";
            cmdText += " ,a.[SalesValue]";
            cmdText += " ,a.[SellingPrice]";
            cmdText += " ,a.[AmazonWeight]";
            cmdText += " ,a.[Profit]";
            cmdText += " ,a.[AmazonWeightBasedFee]";
            cmdText += " ,a.[Deduct]";
            cmdText += " ,a.[Approved]";
            cmdText += " ,a.[isDirty]";
            cmdText += " ,b.[Asin]";
            cmdText += " ,b.[QuantityInStock]";
            cmdText += " ,b.[CasePack]";
            cmdText += " ,b.[FirstCategory]";
            cmdText += " ,b.[FirstCategoryRank]";
            cmdText += " ,b.[Manufacturer]";
            cmdText += " ,b.[Cost]";
            cmdText += " ,b.[SalesValue]";
            cmdText += " ,b.[Profit]";
            cmdText += " ,b.[BB-Price]";
            cmdText += " ,b.[BB-Shipping]";
            cmdText += " ,b.[BB-Delivered]";
            cmdText += " ,b.[Seller]";
            cmdText += " ,b.[Name]";
            cmdText += " ,b.[AmazonShippingWeight]";
            cmdText += " ,b.[ShippingWeight]";
            cmdText += " ,b.[FBA]";
            cmdText += " ,b.[AVGRuns]";
            cmdText += " ,b.[AVGRanking]";
            cmdText += " ,b.[WorseRanking]";
            cmdText += " ,b.[BestRanking]";
            cmdText += " ,b.[Likes]";

            cmdText += " FROM [OrderManager].[dbo].[FBAStock] a";
            cmdText += " LEFT JOIN [REPRICING].[dbo].[SCRAPPER_ANALYSIS] b ON a.SKU=b.SKU";
            cmdText += " WHERE a.SKU LIKE '";
            cmdText += prefix;
            cmdText += "%' AND a.SKU NOT IN (SELECT SKU FROM [OrderManager].[dbo].[FBACloseouts])";

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
                        {
                            var product = new Product();

                            try
                            {
                                product.Asin = (string)reader["Asin"];
                            }
                            catch (Exception e)
                            {
                                product.Asin = "";
                                continue;
                            }
                            try
                            {
                                product.Sku = (string)reader["SKU"];
                            }
                            catch (Exception e)
                            {
                                product.Sku = "";
                                continue;
                            }


                            try
                            {
                                product.SalesValue = Convert.ToDecimal(reader["SalesValue"]);
                            }
                            catch (Exception e)
                            {
                                product.SalesValue = 0;
                            }
                            try
                            {
                                product.Profit = Convert.ToDecimal(reader["Profit"]);
                            }
                            catch (Exception e)
                            {
                                product.Profit = 0;
                            }

                            product.Seller = (string)reader["Seller"];
                            product.Manufacturer = (string)reader["Manufacturer"];

                            try
                            {
                                if (!reader.IsDBNull(reader.GetOrdinal("QuantityInStock")))
                                    product.QuantityInStock = Convert.ToInt32(reader["QuantityInStock"]);
                            }
                            catch (Exception e)
                            {
                                product.QuantityInStock = 0;
                            }

                            try
                            {
                                if (!reader.IsDBNull(reader.GetOrdinal("CasePack")))
                                    product.CasePack = Convert.ToInt32(reader["CasePack"]);
                            }
                            catch (Exception e)
                            {
                                product.CasePack = 0;
                            }

                            if (!reader.IsDBNull(reader.GetOrdinal("FirstCategory")))
                                product.FirstCategory = (string)reader["FirstCategory"];

                            try
                            {
                                if (!reader.IsDBNull(reader.GetOrdinal("FirstCategoryRank")))
                                    product.FirstCategoryRank = Convert.ToInt32(reader["FirstCategoryRank"]);
                            }
                            catch (Exception e)
                            {
                                product.FirstCategoryRank = 0;
                            }
                            try
                            {
                                if (!reader.IsDBNull(reader.GetOrdinal("Cost")))
                                    product.Cost = Convert.ToDecimal(reader["Cost"]);
                            }
                            catch (Exception e)
                            {
                                product.Cost = 0;
                            }


                            try
                            {
                                if (!reader.IsDBNull(reader.GetOrdinal("BB-Price")))
                                    product.BBPrice = Convert.ToDecimal(reader["BB-Price"]);
                            }
                            catch (Exception e)
                            {
                                product.BBPrice = 0;
                            }


                            try
                            {
                                if (!reader.IsDBNull(reader.GetOrdinal("BB-Shipping")))
                                    product.BBShipping = Convert.ToDecimal(reader["BB-Shipping"]);
                            }
                            catch (Exception e)
                            {
                                product.BBShipping = 0;
                            }

                            try
                            {
                                if (!reader.IsDBNull(reader.GetOrdinal("BB-Delivered")))
                                    product.BBDelivered = Convert.ToDecimal(reader["BB-Delivered"]);
                            }
                            catch (Exception e)
                            {
                                product.BBDelivered = 0;
                            }


                            if (!reader.IsDBNull(reader.GetOrdinal("Name")))
                                product.Name = (string)reader["Name"];

                            if (!reader.IsDBNull(reader.GetOrdinal("AmazonShippingWeight")))
                                product.AmazonShippingWeight = (string)reader["AmazonShippingWeight"];

                            if (!reader.IsDBNull(reader.GetOrdinal("ShippingWeight")))
                                product.ShippingWeight = (string)reader["ShippingWeight"];

                            try
                            {
                                if (!reader.IsDBNull(reader.GetOrdinal("AVGRanking")))
                                    product.AverageRanking = Convert.ToInt32(reader["AVGRanking"]);
                            }
                            catch (Exception e)
                            {
                                product.AverageRanking = 0;
                            }
                            try
                            {
                                if (!reader.IsDBNull(reader.GetOrdinal("Likes")))
                                    product.Likes = Convert.ToInt32(reader["Likes"]);
                            }
                            catch (Exception e)
                            {
                                product.Likes = 0;
                            }

                            result.Add(product);
                        }
                    }
                }

                connection.Close();
            }

            return result;
        }

        public List<FBAStockData> GetFBAStockList(string prefix)
        {
            List<FBAStockData> result = new List<FBAStockData>();

            string cmdText = "SELECT * FROM [OrderManager].[dbo].[FBAStock] WHERE SKU LIKE '" + prefix + "%'";
            cmdText += " AND SKU NOT IN(SELECT[SKU] FROM [OrderManager].[dbo].[FBACloseouts])";

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
                            var fbaStockData = new FBAStockData();

                            try
                            {
                                fbaStockData.SKU = reader["SKU"].ToString();
                            }
                            catch (Exception e) {
                                fbaStockData.SKU = "";
                            }

                            try
                            {
                                fbaStockData.realItemCode = GetRealItemCode(fbaStockData.SKU);
                            }catch(Exception e)
                            {
                                fbaStockData.realItemCode = "";
                            }

                            try { fbaStockData.Cost = decimal.Parse(reader["Cost"].ToString()); }
                            catch (Exception e)
                            {
                                fbaStockData.Cost = 0;
                            }
                            try
                            {
                                fbaStockData.SalesValue = decimal.Parse(reader["SalesValue"].ToString());
                            }
                            catch (Exception e)
                            {
                                fbaStockData.SalesValue = 0;
                            }
                            try { fbaStockData.SellingPrice = decimal.Parse(reader["SellingPrice"].ToString()); }
                            catch (Exception e)
                            {
                                fbaStockData.SellingPrice = 0;
                            }
                            try { fbaStockData.AmazonWeight = decimal.Parse(reader["AmazonWeight"].ToString()); }
                            catch (Exception e)
                            {
                                fbaStockData.AmazonWeight = 0;
                            }
                            try { fbaStockData.Profit = decimal.Parse(reader["Profit"].ToString()); }
                            catch (Exception e)
                            {
                                fbaStockData.Profit = 0;
                            }
                            try { fbaStockData.AmazonWeightBasedFee = decimal.Parse(reader["AmazonWeightBasedFee"].ToString()); }
                            catch (Exception e)
                            {
                                fbaStockData.AmazonWeightBasedFee = 0;
                            }
                            try { fbaStockData.Deduct = reader["Deduct"].ToString(); }
                            catch (Exception e)
                            {
                                fbaStockData.Deduct = "";
                            }
                            try { fbaStockData.Approved = int.Parse(reader["Approved"].ToString()); }
                            catch (Exception e)
                            {
                                fbaStockData.Approved = 0;
                            }
                            try { fbaStockData.isDirty = int.Parse(reader["isDirty"].ToString()); }
                            catch (Exception e)
                            {
                                fbaStockData.isDirty = 0;
                            }

                            result.Add(fbaStockData);
                        }
                    }
                }

                connection.Close();
            }

            return result;
        }
        private QuantityRecommedFactor GetQuantityRecommendFactor(Vendor vendor)
        {
            QuantityRecommedFactor result = new QuantityRecommedFactor();

            result.MinProfit = 0;
            result.HowlongToStockFor = 0;
            result.HowFarBack = 0;
            result.CategoryRankings = "";

            string cmdText = "SELECT [MinProfit],[HowlongToStockFor],[HowFarBack],[CategoryRankings] FROM [AMAZON].[dbo].[AnalysisConfig]";

            string _connectionString = ConfigurationManager.ConnectionStrings["AMAZON"].ConnectionString;
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var command = new SqlCommand(cmdText, connection))
                {
                    command.CommandType = CommandType.Text;
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            try
                            {
                                result.HowlongToStockFor = int.Parse(reader["HowlongToStockFor"].ToString());
                            }
                            catch (Exception e)
                            {
                                result.HowlongToStockFor = 0;
                            }

                            try
                            {
                                result.HowFarBack = int.Parse(reader["HowFarBack"].ToString());
                            }
                            catch (Exception e)
                            {
                                result.HowFarBack = 0;
                            }

                            try
                            {
                                result.MinProfit = decimal.Parse(reader["MinProfit"].ToString());
                            }
                            catch (Exception e)
                            {
                                result.MinProfit = 0;
                            }
                            try
                            {
                                result.CategoryRankings = reader["CategoryRankings"].ToString();
                            } catch (Exception e)
                            {
                                result.CategoryRankings = "";
                            }
                        }
                    }
                }

                connection.Close();
            }

            //*
            result.AvgDaysInTransit = vendor.AvgDaysInTransit ?? 0;
            result.AvgLeadTimeToShip = vendor.AvgLeadTimeToShip ?? 0;
            result.AvgDaysToAcceptGoods = vendor.AvgDaysToAcceptGoods ?? 0;

            return result;
        }

        private object[] GetDOrdersQuantity(string marketPlace, Vendor vendor)
        {
            List<OrderQuantity> orderQuantityList = new List<OrderQuantity>();

            var awsSettings = AwsSettings.GetSettings(marketPlace);

            var dtToday = DateTime.Now;

            var date0 = dtToday.ToString("dd/MM/yyyy");
            DateTime date = DateTime.ParseExact(date0, "dd/MM/yyyy", null);

            //int beforeTotal = quantityRecommendFactor.HowFarBack + quantityRecommendFactor.AvgDaysInTransit + quantityRecommendFactor.AvgDaysToAcceptGoods+ quantityRecommendFactor.AvgLeadTimeToShip;

            var dtFrom = date.AddDays(-7);
            var dtTo = date;

            var dtPastFrom = date.AddDays(-30);
            var dtPastTo = date;//.AddDays(-1 * (quantityRecommendFactor.AvgDaysInTransit + quantityRecommendFactor.AvgDaysToAcceptGoods + quantityRecommendFactor.AvgLeadTimeToShip));

            string strDtFrom = dtFrom.ToString("yyyy-MM-dd");
            string strDtTo = dtTo.ToString("yyyy-MM-dd");
            //string strDtTo = StringHelper.EscapeString(dtTo.ToString("yyyy-MM-dd"));

            string strDtPastFrom = dtPastFrom.ToString("yyyy-MM-dd");
            string strDtPastTo = dtPastTo.ToString("yyyy-MM-dd");
            //string strDtPastTo = StringHelper.EscapeString(dtPastTo.ToString("yyyy-MM-dd"));

            List<OrderQuantity> D30OrderQuantityList = GetOrderItemsFromTo(strDtPastFrom, strDtPastTo, vendor.VendorId.ToString(), marketPlace);
            List<OrderQuantity> D7OrderQuantityList = GetOrderItemsFromTo(strDtFrom, strDtTo, vendor.VendorId.ToString(), marketPlace);

            object[] res = new object[2];

            res[0] = D7OrderQuantityList;
            res[1] = D30OrderQuantityList;

            return res;
        }

        private object[] GetOrdersQuantity(string marketPlace, Vendor vendor)
        {
            List<OrderQuantity> orderQuantityList = new List<OrderQuantity>();

            QuantityRecommedFactor quantityRecommendFactor = GetQuantityRecommendFactor(vendor);

            var awsSettings = AwsSettings.GetSettings(marketPlace);

            var dtToday = DateTime.Now;

            var date0 = dtToday.ToString("dd/MM/yyyy");
            DateTime date = DateTime.ParseExact(date0, "dd/MM/yyyy", null);

            //int beforeTotal = quantityRecommendFactor.HowFarBack + quantityRecommendFactor.AvgDaysInTransit + quantityRecommendFactor.AvgDaysToAcceptGoods+ quantityRecommendFactor.AvgLeadTimeToShip;

            var dtFrom = date.AddDays(-1 * quantityRecommendFactor.HowlongToStockFor);
            var dtTo = date;

            var dtPastFrom = date.AddDays(-1 * quantityRecommendFactor.HowFarBack);
            var dtPastTo = date;//.AddDays(-1 * (quantityRecommendFactor.AvgDaysInTransit + quantityRecommendFactor.AvgDaysToAcceptGoods + quantityRecommendFactor.AvgLeadTimeToShip));

            string strDtFrom = dtFrom.ToString("yyyy-MM-dd");
            string strDtTo = dtTo.ToString("yyyy-MM-dd");
            //string strDtTo = StringHelper.EscapeString(dtTo.ToString("yyyy-MM-dd"));

            string strDtPastFrom = dtPastFrom.ToString("yyyy-MM-dd");
            string strDtPastTo = dtPastTo.ToString("yyyy-MM-dd");
            //string strDtPastTo = StringHelper.EscapeString(dtPastTo.ToString("yyyy-MM-dd"));

            List<OrderQuantity> pastOrderQuantityList = GetOrderItemsFromTo(strDtPastFrom, strDtPastTo, vendor.VendorId.ToString(), marketPlace);
            //curOrderQuantityList = GetOrderItemsFromTo(strDtFrom, strDtTo, vendor.VendorId.ToString(), marketPlace);

            object[] res = new object[2];

            res[0] = pastOrderQuantityList;
            res[1] = quantityRecommendFactor;

            return res;
        }
        private List<OrderQuantity> GetOrderItemsFromTo(string dtFrom, string dtTo, string vendorId, string marketplace)
        {
            List<OrderQuantity> orderQuantityList = new List<OrderQuantity>();

            string cmdText = "SELECT Asin, SUM([Quantity]) AS sum_quantity ";
            cmdText += " FROM[OrderManager].[dbo].[AmazonFulfillmentOrderItem] a";
            cmdText += " LEFT JOIN[OrderManager].[dbo].[AmazonFulfillmentOrders] b ON a.[AmazonOrderId]=b.[AmazonOrderId] ";
            cmdText += " WHERE b.[LastUpdateDate]>='" + dtFrom + "' AND b.[LastUpdateDate]<='" + dtTo + "' ";
            cmdText += " GROUP BY Asin";
            
            string _connectionString = ConfigurationManager.ConnectionStrings["AMAZON"].ConnectionString;
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var command = new SqlCommand(cmdText, connection))
                {
                    command.CommandType = CommandType.Text;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            OrderQuantity orderQuantity = new OrderQuantity();
                            try
                            {
                                orderQuantity.ASIN = reader["Asin"].ToString();
                            }
                            catch (Exception e)
                            {
                                orderQuantity.ASIN = "";
                            }

                            try
                            {
                                orderQuantity.QuantityOrdered = int.Parse(reader["sum_quantity"].ToString());
                            }
                            catch (Exception e)
                            {
                                orderQuantity.QuantityOrdered = 0;
                            }

                            orderQuantityList.Add(orderQuantity);
                        }
                    }
                }

                connection.Close();
            }

            return orderQuantityList;
        }
        /*
        private void SetOrderItemInfo(List<String> orderIdList, AwsSettings awsSettings, List<OrderQuantity> orderQuantityList)
        {
            foreach (string orderId in orderIdList)
            {
                if (string.IsNullOrEmpty(orderId))
                    continue;

                var param = "AWSAccessKeyId=" + awsSettings.AccessKey;
                param += $"&Action=ListOrderItems";
                param += "&SellerId=" + awsSettings.MerchantId;
                param += "&SignatureMethod=HmacSHA256";
                param += "&SignatureVersion=2";
                param += "&Timestamp=" + AwsSettings.TimeStamp;
                param += "&Version=" + AwsSettings.ApiVersion;
                param += "&MarketplaceId=" + awsSettings.MarketPlaceId;
                param += "&AmazonOrderId=" + orderId;

                var signature = AmazonHelper.SignRequest(param, awsSettings.SecretKey, awsSettings.Url,
                   AwsSettings.ServiceName, AwsSettings.ApiVersion)
                   .Replace("=", "%3D").Replace("+", "%2B").Replace("/", "%2F");

                param += "&Signature=" + signature;
                var request = $"https://{awsSettings.Url}{AwsSettings.ServiceName}{AwsSettings.ApiVersion}?{param}";
                var response = AmazonHelper.SendRequest(request);

                if (!string.IsNullOrEmpty(response))
                {
                    ParseOrderItemListData(response, $"ListOrderItemsResult", orderQuantityList);
                }

                var delayTime = int.Parse(ConfigurationManager.AppSettings["DelayTime"]);
                Thread.Sleep(delayTime);
            }
        }
        //*/
        /*
        private void ParseOrderItemListData(string responseData, string resultElementName, List<OrderQuantity> orderQuantityList)
        {
            int result = 0;
            XDocument xmlDoc = XDocument.Parse(responseData);

            List<String> orderIdList = new List<String>();

            if (xmlDoc.Root == null)
                return;

            IEnumerable<XElement> orderItems = xmlDoc.Root
                .Elements().Where(x => x.Name.LocalName == resultElementName)
                .Elements().Where(x => x.Name.LocalName == "OrderItems");

            int itemsCount = orderItems.Count();
            if (itemsCount > 0)
            {
                foreach (XElement item in orderItems)
                {
                    string ASIN = "";
                    string SKU = "";
                    int QuantityOrdered = 0;
                    int QuantityShipped = 0;

                    try
                    {
                        XElement nodeASIN = item
                            .Elements().Where(x => x.Name.LocalName == "ASIN")
                            .First();
                        ASIN = nodeASIN.Value;

                        XElement nodeSKU = item
                            .Elements().Where(x => x.Name.LocalName == "SellerSKU")
                            .First();
                        SKU = nodeSKU.Value;

                        try
                        {
                            XElement nodeQuantityOrdered = item
                                .Elements().Where(x => x.Name.LocalName == "QuantityOrdered")
                                .First();
                            QuantityOrdered = int.Parse(nodeQuantityOrdered.Value);
                        }
                        catch (Exception e) { }

                        try
                        {
                            XElement nodeQuantityShipped = item
                                .Elements().Where(x => x.Name.LocalName == "QuantityShipped")
                                .First();
                            QuantityShipped = int.Parse(nodeQuantityShipped.Value);
                        }
                        catch (Exception e) { }
                    }
                    catch (Exception ex) { }

                    OrderQuantity orderQuantity = FindOrderQuantityByAsin(orderQuantityList, ASIN);

                    if (orderQuantity != null)
                    {
                        orderQuantity.QuantityOrdered += QuantityOrdered;
                    }
                }
            }
        }
        //*/
        private OrderQuantity FindOrderQuantityByAsin(List<OrderQuantity> orderQuantityList, string ASIN)
        {
            foreach (OrderQuantity orderQuantity in orderQuantityList)
            {
                if (orderQuantity.ASIN.Equals(ASIN))
                    return orderQuantity;
            }

            return null;
        }
        private List<AmazonFulfilmentOrder> ParseListOrdersData(string responseData, string resultElementName, AwsSettings awsSettings, string marketPlace)
        {
            XDocument xmlDoc = XDocument.Parse(responseData);

            List<AmazonFulfilmentOrder> orderList = new List<AmazonFulfilmentOrder>();

            if (xmlDoc.Root == null)
                return new List<AmazonFulfilmentOrder>();

            IEnumerable<XElement> orders = xmlDoc.Root
                .Elements().Where(x => x.Name.LocalName == resultElementName)
                .Elements().Where(x => x.Name.LocalName == "Orders")
                .Elements().Where(x => x.Name.LocalName == "Order"); ;

            int itemsCount = orders.Count();
            if (itemsCount > 0)
            {
                foreach (XElement iOrder in orders)
                {
                    AmazonFulfilmentOrder order = new AmazonFulfilmentOrder();

                    try
                    {
                        XElement nodeAmazonOrderId = iOrder
                            .Elements().Where(x => x.Name.LocalName == "AmazonOrderId")
                            .First();
                        order.AmazonOrderId = nodeAmazonOrderId.Value;
                    }
                    catch (Exception ex)
                    {
                        order.AmazonOrderId = "";
                        continue;
                    }
                    try
                    {
                        XElement nodeAmazonOrderId = iOrder
                            .Elements().Where(x => x.Name.LocalName == "SellerOrderId")
                            .First();
                        order.MerchantOrderId = nodeAmazonOrderId.Value;
                    }
                    catch (Exception ex)
                    {
                        order.MerchantOrderId = "";
                    }
                    try
                    {
                        XElement nodeAmazonOrderId = iOrder
                            .Elements().Where(x => x.Name.LocalName == "PurchaseDate")
                            .First();
                        order.PurchaseDate = nodeAmazonOrderId.Value;
                    }
                    catch (Exception ex)
                    {
                        order.PurchaseDate = "";
                    }
                    try
                    {
                        XElement nodeAmazonOrderId = iOrder
                            .Elements().Where(x => x.Name.LocalName == "LastUpdateDate")
                            .First();
                        order.LastUpdateDate = nodeAmazonOrderId.Value;
                    }
                    catch (Exception ex)
                    {
                        order.LastUpdateDate = "";
                    }
                    try
                    {
                        XElement nodeAmazonOrderId = iOrder
                            .Elements().Where(x => x.Name.LocalName == "OrderStatus")
                            .First();
                        order.OrderStatus = nodeAmazonOrderId.Value;
                    }
                    catch (Exception ex)
                    {
                        order.OrderStatus = "";
                    }
                    try
                    {
                        XElement nodeAmazonOrderId = iOrder
                            .Elements().Where(x => x.Name.LocalName == "SalesChannel")
                            .First();
                        order.SalesChannel = nodeAmazonOrderId.Value;
                    }
                    catch (Exception ex)
                    {
                        order.SalesChannel = "";
                    }
                    try
                    {
                        XElement nodeAmazonOrderId = iOrder
                            .Elements().Where(x => x.Name.LocalName == "")
                            .First();
                        order.Url = nodeAmazonOrderId.Value;
                    }
                    catch (Exception ex)
                    {
                        order.Url = "";
                    }
                    try
                    {
                        XElement nodeAmazonOrderId = iOrder
                            .Elements().Where(x => x.Name.LocalName == "FulfillmentChannel")
                            .First();
                        order.FulfillmentChannel = nodeAmazonOrderId.Value;
                    }
                    catch (Exception ex)
                    {
                        order.FulfillmentChannel = "";
                    }
                    try
                    {
                        XElement nodeAmazonOrderId = iOrder
                            .Elements().Where(x => x.Name.LocalName == "ShipServiceLevel")
                            .First();
                        order.ShipServiceLevel = nodeAmazonOrderId.Value;
                    }
                    catch (Exception ex)
                    {
                        order.ShipServiceLevel = "";
                    }
                    try
                    {
                        XElement nodeAddress = iOrder
                            .Elements().Where(x => x.Name.LocalName == "ShippingAddress")
                            .First();

                        order.Ship_City = nodeAddress
                            .Elements().Where(x => x.Name.LocalName == "City")
                            .First().Value;
                    }
                    catch (Exception ex)
                    {
                        order.Ship_City = "";
                    }
                    try
                    {
                        XElement nodeAmazonOrderId = iOrder
                            .Elements().Where(x => x.Name.LocalName == "")
                            .First();
                        order.Ship_State = nodeAmazonOrderId.Value;
                    }
                    catch (Exception ex)
                    {
                        order.Ship_State = "";
                    }
                    try
                    {
                        XElement nodeAddress = iOrder
                            .Elements().Where(x => x.Name.LocalName == "ShippingAddress")
                            .First();

                        order.Ship_PostalCode = nodeAddress
                            .Elements().Where(x => x.Name.LocalName == "PostalCode")
                            .First().Value; 
                    }
                    catch (Exception ex)
                    {
                        order.Ship_PostalCode = "";
                    }
                    try
                    {
                        XElement nodeAddress = iOrder
                            .Elements().Where(x => x.Name.LocalName == "ShippingAddress")
                            .First();

                        order.Ship_Country = nodeAddress
                            .Elements().Where(x => x.Name.LocalName == "CountryCode")
                            .First().Value;
                    }
                    catch (Exception ex)
                    {
                        order.Ship_Country = "";
                    }
                    
                    orderList.Add(order);
                }
            }

            return orderList;
        }
        public List<MarketPlace> GetAllMarketPlaces()
        {
            return DataAccessLayer.GetAllMarketPlaces();
        }

        public List<ProductData> ScanProduct(string marketPlace, Stream fileStream)
        {
            List<ProductData> result = new List<ProductData>();

            using (var streamReader = new StreamReader(fileStream))
            {
                string line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    var products = ScanProduct(marketPlace, line);
                    foreach (var product in products)
                    {
                        product.FirstCategory = string.IsNullOrEmpty(product.FirstCategory)
                            ? "\\N"
                            : product.FirstCategory.Replace("_display_on_website", "");
                        product.FirstCategoryRank = string.IsNullOrEmpty(product.FirstCategoryRank)
                            ? "-"
                            : product.FirstCategoryRank;

                        bool add = false;
                        if (!string.IsNullOrEmpty(product.InputBarCode))
                        {
                            add = product.OutputBarCode.Equals(product.InputBarCode);
                        }
                        else
                        {
                            add = (product.OutputManufactureName.IndexOf(product.InputManufactureName,
                                StringComparison.OrdinalIgnoreCase) != -1) &&
                                  (product.OutputManufacturePart.IndexOf(product.OutputManufacturePart,
                                      StringComparison.OrdinalIgnoreCase) != -1);
                        }

                        if (add)
                        {
                            result.Add(product);
                        }
                    }

                    int delayTime = int.Parse(ConfigurationManager.AppSettings["DelayTime"]);
                    Thread.Sleep(delayTime);
                }

                streamReader.Close();
                streamReader.Dispose();
            }

            return result;
        }

        private List<ProductData> ScanProduct(string marketPlace, string line)
        {
            //remove special characters
            line = line.Replace("'", "").Replace("/", "").Replace("&", "").Replace("?", "");

            //each line contains information about product name, cost,...
            //format: BarCode   [tab]   ManufactureName [tab]   ManufacturePart [tab]   ProductName [tab]   Cost    [tab]   ShippingWeight  [tab] ASIN
            string[] product = line.Split('\t');

            var searchData = new SearchData();
            if (product.Length > 0)
                searchData.Barcode = product[0];
            if (product.Length > 1)
                searchData.ManufactureName = product[1];
            if (product.Length > 2)
                searchData.ManufacturePart = product[2];
            if (product.Length > 3)
                searchData.ProductName = product[3];
            if (product.Length > 4)
                searchData.Cost = product[4];
            if (product.Length > 5)
                searchData.ShippingWeight = product[5];
            if (product.Length > 6)
                searchData.ASIN = product[6];

            //remove non digit numbers from barcode
            //*
            if (!string.IsNullOrEmpty(searchData.Barcode))
            {
                var digitsOnly = new Regex(@"[^\d]");
                searchData.Barcode = digitsOnly.Replace(searchData.Barcode, "");
                searchData.Barcode.Replace("\"", "");
            }//*/

            var awsSettings = AwsSettings.GetSettings(marketPlace);

            if (!string.IsNullOrEmpty(searchData.ASIN))
            {
                //if we have the ASIN in the input file, then search for products by this ASIN to save requests
                return SearchProductsByAsin(searchData, awsSettings, marketPlace);
            }
            else
            {
                //...otherwiser, search by the ManufactureName, ManufacturePart, etc.
                return SearchProductsByQuery(searchData, awsSettings, marketPlace);
            }
        }

        private List<ProductData> SearchProductsByAsin(SearchData searchData, AwsSettings awsSettings, string marketPlace)
        {
            string param = "IdList.Id.1=" + searchData.ASIN;
            param += "&IdType=ASIN";
            param += "&MarketplaceId=" + awsSettings.MarketPlaceId;

            return SearchProducts(searchData, param, "GetMatchingProductForId", awsSettings, marketPlace);
        }
        private List<ProductData> SearchProductsByQuery(SearchData searchData, AwsSettings awsSettings, string marketPlace)
        {
            string query = searchData.Barcode;

            if (!string.IsNullOrEmpty(searchData.ManufactureName))
            {
                query += " " + searchData.ManufactureName;
                    

                if (!string.IsNullOrEmpty(searchData.ManufacturePart))
                {
                    query += " " + searchData.ManufacturePart;
                }
            }

            query = StringHelper.EscapeString(query.Trim());

            string param = "MarketplaceId=" + awsSettings.MarketPlaceId;
            param += "&Query=" + query;

            return SearchProducts(searchData, param, "ListMatchingProducts", awsSettings, marketPlace);
        }

        private List<ProductData> SearchProducts(SearchData searchData, string additionalParam, string action,
            AwsSettings awsSettings, string marketPlace)
        {
            var param = "AWSAccessKeyId=" + awsSettings.AccessKey;
            param += $"&Action={action}";
            param += $"&{additionalParam}";
            param += "&SellerId=" + awsSettings.MerchantId;
            param += "&SignatureMethod=HmacSHA256";
            param += "&SignatureVersion=2";
            param += "&Timestamp=" + AwsSettings.TimeStamp;
            param += "&Version=" + AwsSettings.ApiVersion;

            string signature = AmazonHelper.SignRequest(param, awsSettings.SecretKey, awsSettings.Url,
               AwsSettings.ServiceName, AwsSettings.ApiVersion)
               .Replace("=", "%3D").Replace("+", "%2B").Replace("/", "%2F");

            param += "&Signature=" + signature;
            string request = $"https://{awsSettings.Url}{AwsSettings.ServiceName}{AwsSettings.ApiVersion}?{param}";
            string response = AmazonHelper.SendRequest(request);

            if (string.IsNullOrEmpty(response))
                return new List<ProductData>();

            return ParseProductData(response, $"{action}Result", searchData, awsSettings, marketPlace);
        }
        private bool SearchOrders(string additionalParam,
            AwsSettings awsSettings, string marketPlace)
        {
            string action = "ListOrders";
            string ServiceName = "Orders";

            var param = "AWSAccessKeyId=" + awsSettings.AccessKey;
            param += $"&Action={action}";
            param += $"&{additionalParam}";
            param += "&SellerId=" + awsSettings.MerchantId;
            param += "&SignatureMethod=HmacSHA256";
            param += "&SignatureVersion=2";
            param += "&Timestamp=" + AwsSettings.TimeStamp;
            param += "&Version=" + AwsSettings.ApiVersion;

            string signature = AmazonHelper.SignRequest(param, awsSettings.SecretKey, awsSettings.Url,
               ServiceName, AwsSettings.ApiVersion)
               .Replace("=", "%3D").Replace("+", "%2B").Replace("/", "%2F");

            param += "&Signature=" + signature;
            string request = $"https://{awsSettings.Url}{ServiceName}{AwsSettings.ApiVersion}?{param}";
            string response = AmazonHelper.SendRequest(request);

            //string request = $"https://{awsSettings.Url}{AwsSettings.ServiceName}{AwsSettings.ApiVersion}?{param}";

            if (string.IsNullOrEmpty(response))
                return false;

            return true;
            //return ParseProductData(response, $"{action}Result", searchData, awsSettings, marketPlace);
        }

        private List<ProductData> ParseProductData(string responseData, string resultElementName, SearchData searchData, 
            AwsSettings awsSettings, string marketPlace)
        {
            List<ProductData> result = new List<ProductData>();
            XDocument xmlDoc = XDocument.Parse(responseData);

            if (xmlDoc.Root == null)
                return new List<ProductData>();

            IEnumerable<XElement> products = xmlDoc.Root
                .Elements().Where(x => x.Name.LocalName == resultElementName)
                .Elements().Where(x => x.Name.LocalName == "Products")
                .Elements().Where(x => x.Name.LocalName == "Product");

            int itemsCount = products.Count();
            if (itemsCount > 0)
            {
                foreach (XElement iProduct in products)
                {
                    string manufactureName = "";
                    string manufacturePart = "";
                    string productName = "";
                    string asin = "";
                    string amazoneShippingWeight = "";
                    string firstCategoryId = "";
                    string firstCategoryRank = "";

                    try
                    {
                        XElement manufactureNameElement = iProduct
                            .Elements().Where(x => x.Name.LocalName == "AttributeSets")
                            .Elements().Where(x => x.Name.LocalName == "ItemAttributes")
                            .Elements().Where(x => x.Name.LocalName == "Manufacturer").First();

                        manufactureName = manufactureNameElement.Value;
                    }
                    catch (Exception ex) { }

                    try
                    {
                        XElement manufacturePartElement = iProduct
                            .Elements().Where(x => x.Name.LocalName == "AttributeSets")
                            .Elements().Where(x => x.Name.LocalName == "ItemAttributes")
                            .Elements().Where(x => x.Name.LocalName == "PartNumber").First();

                        manufacturePart = manufacturePartElement.Value;
                    }
                    catch (Exception ex) { }

                    try
                    {
                        XElement productNameElement = iProduct
                            .Elements().Where(x => x.Name.LocalName == "AttributeSets")
                            .Elements().Where(x => x.Name.LocalName == "ItemAttributes")
                            .Elements().Where(x => x.Name.LocalName == "Title").First();

                        productName = productNameElement.Value;

                    }
                    catch (Exception ex) { }

                    try
                    {
                        XElement Asin = iProduct
                            .Elements().Where(x => x.Name.LocalName == "Identifiers")
                            .Elements().Where(x => x.Name.LocalName == "MarketplaceASIN")
                            .Elements().Where(x => x.Name.LocalName == "ASIN").First();

                        asin = Asin.Value;
                    }
                    catch (Exception ex) { }

                    try
                    {
                        XElement amazoneShippingWeightElement = iProduct
                            .Elements().Where(x => x.Name.LocalName == "AttributeSets")
                            .Elements().Where(x => x.Name.LocalName == "ItemAttributes")
                            .Elements().Where(x => x.Name.LocalName == "PackageDimensions")
                            .Elements().Where(x => x.Name.LocalName == "Weight").First();

                        amazoneShippingWeight = amazoneShippingWeightElement.Value;
                    }
                    catch (Exception ex) { }

                    try
                    {
                        XElement firstCategoryIDElement = iProduct
                            .Elements().Where(x => x.Name.LocalName == "SalesRankings")
                            .Elements().Where(x => x.Name.LocalName == "SalesRank").First()
                            .Elements().Where(x => x.Name.LocalName == "ProductCategoryId").First();

                        firstCategoryId = firstCategoryIDElement.Value;
                    }
                    catch (Exception ex) { }

                    try
                    {
                        XElement firstCategoryRankElement = iProduct
                            .Elements().Where(x => x.Name.LocalName == "SalesRankings")
                            .Elements().Where(x => x.Name.LocalName == "SalesRank").First()
                            .Elements().Where(x => x.Name.LocalName == "Rank").First();

                        firstCategoryRank = firstCategoryRankElement.Value;
                    }
                    catch (Exception ex) { }

                    ProductData product = new ProductData();
                    product.InputManufactureName = searchData.ManufactureName;
                    product.InputManufacturePart = searchData.ManufacturePart;
                    product.InputProductName = searchData.ProductName;
                    product.InputBarCode = searchData.Barcode;

                    product.OutputBarCode = searchData.Barcode;
                    product.OutputManufactureName = manufactureName;
                    product.OutputManufacturePart = manufacturePart;
                    product.OutputProductName = productName;
                    product.Asin = asin;
                    product.Cost = searchData.Cost;
                    product.ShippingWeight = searchData.ShippingWeight;
                    product.AmazoneShippingWeight = amazoneShippingWeight;
                    product.FirstCategory = firstCategoryId;
                    product.FirstCategoryRank = firstCategoryRank;

                    var param = "ASINList.ASIN.1=" + product.Asin;
                    param += "&AWSAccessKeyId=" + awsSettings.AccessKey;
                    param += "&Action=GetCompetitivePricingForASIN";
                    param += "&ExcludeMe=true";
                    param += "&ItemCondition=New";
                    param += "&MarketplaceId=" + awsSettings.MarketPlaceId;
                    param += "&SellerId=" + awsSettings.MerchantId;
                    param += "&SignatureMethod=HmacSHA256";
                    param += "&SignatureVersion=2";
                    param += "&Timestamp=" + AwsSettings.TimeStamp;
                    param += "&Version=" + AwsSettings.ApiVersion;

                    string signature = AmazonHelper.SignRequest(param, awsSettings.SecretKey, awsSettings.Url,
                        AwsSettings.ServiceName, AwsSettings.ApiVersion)
                        .Replace("=", "%3D").Replace("+", "%2B").Replace("/", "%2F");

                    param += "&Signature=" + signature;
                    var request = $"https://{awsSettings.Url}{AwsSettings.ServiceName}{AwsSettings.ApiVersion}?{param}";
                    var response = AmazonHelper.SendRequest(request);

                    Seller seller = ParseSellerData(response);

                    if (seller != null)
                    {
                        product.BBPrice = seller.BBPrice;
                        product.BBShipping = seller.BBShipping;
                        product.BBDelivered = seller.BBDelivered;
                        product.Seller = seller.Name;
                    }

                    //Get SalesValue & Profit
                    double shippingWeight = string.IsNullOrEmpty(product.AmazoneShippingWeight)
                        ? product.ShippingWeight.SafeCastToDouble()
                        : product.AmazoneShippingWeight.SafeCastToDouble();

                    double price = product.BBDelivered.SafeCastToDouble();

                    double salesValue = DataAccessLayer.GetSalesValue(marketPlace, shippingWeight, price,
                        product.FirstCategory, string.Empty);
                    product.SalesValue = salesValue.ToString();

                    double profit = Math.Round(salesValue - product.Cost.SafeCastToDouble(), 2);
                    product.Profit = profit.ToString();

                    product.AmazonLink = AmazonHelper.GetAmazoneLink(marketPlace, asin);
                    
                    result.Add(product);
                }
            }
            else
            {
                ProductData product = new ProductData();
                product.InputManufactureName = searchData.ManufactureName;
                product.InputManufacturePart = searchData.ManufacturePart;
                product.InputProductName = searchData.ProductName;
                product.InputBarCode = searchData.Barcode;
                product.Asin = NotFound;
                product.OutputBarCode = "";
                product.Cost = searchData.Cost;
                product.ShippingWeight = searchData.ShippingWeight;

                product.BBPrice = "";
                product.BBShipping = "";
                product.BBDelivered = "";
                product.Seller = "";

                result.Add(product);
            }

            bool notNull = false;
            if (result.Count() > 1)
            {
                foreach (var prod in result)
                {
                    if (prod.Asin != NotFound) notNull = true;
                }

                if (notNull)
                {
                    for (int i = 0; i < result.Count; i++)
                    {
                        if (result[i].Asin == NotFound)
                        {
                            result.RemoveAt(i);
                            i--;
                        }
                    }
                }
                else
                {
                    for (int i = 1; i < result.Count; i++)
                    {
                        result.RemoveAt(i);
                        i--;
                    }
                }
            }

            return result;
        }

        private Seller ParseSellerData(string responseData)
        {
            if (string.IsNullOrEmpty(responseData))
                return null;

            Seller seller = new Seller();
            XDocument xmlDoc = XDocument.Parse(responseData);

            XElement sellerElement = null;

            try
            {
                if (xmlDoc.Root != null)
                    sellerElement = xmlDoc.Root
                        .Elements().Where(x => x.Name.LocalName == "GetCompetitivePricingForASINResult")
                        .Elements().Where(x => x.Name.LocalName == "Product")
                        .Elements().Where(x => x.Name.LocalName == "CompetitivePricing")
                        .Elements().Where(x => x.Name.LocalName == "CompetitivePrices")
                        .Elements().First(x => x.Name.LocalName == "CompetitivePrice");
            }
            catch (Exception ex)
            {
            }

            try
            {
                XElement sellerName = sellerElement
                    .Elements().Where(x => x.Name.LocalName == "Qualifiers")
                    .Elements().First(x => x.Name.LocalName == "FulfillmentChannel");
                seller.Name = sellerName.Value;
            }
            catch (Exception ex)
            {
            }

            if (sellerElement != null)
            {
                try
                {
                    XElement BBPrice = sellerElement
                        .Elements().Where(x => x.Name.LocalName == "Price")
                        .Elements().Where(x => x.Name.LocalName == "ListingPrice")
                        .Elements().First(x => x.Name.LocalName == "Amount");
                    seller.BBPrice = BBPrice.Value;
                }
                catch (Exception ex)
                {
                }

                try
                {
                    XElement BBShipping = sellerElement
                        .Elements().Where(x => x.Name.LocalName == "Price")
                        .Elements().Where(x => x.Name.LocalName == "Shipping")
                        .Elements().First(x => x.Name.LocalName == "Amount");
                    seller.BBShipping = BBShipping.Value;
                }
                catch (Exception ex)
                {
                }

                try
                {
                    XElement BBDelivered = sellerElement
                        .Elements().Where(x => x.Name.LocalName == "Price")
                        .Elements().Where(x => x.Name.LocalName == "LandedPrice")
                        .Elements().First(x => x.Name.LocalName == "Amount");
                    seller.BBDelivered = BBDelivered.Value;
                }
                catch (Exception ex)
                {
                }
            }

            return seller;
        }
    }
}
