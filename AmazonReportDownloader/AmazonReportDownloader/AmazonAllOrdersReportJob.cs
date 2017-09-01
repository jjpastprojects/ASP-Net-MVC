using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MarketplaceWebService.Model;
using MarketplaceWebService;
using Quartz;
using System.IO;
using System.Xml.Linq;
using System.Data.SqlClient;
using System.Data;
using System.Threading;
using System.Xml;
using log4net;
using System.Globalization;
using System.Configuration;

namespace AmazonReportDownloader
{
    public class AmazonAllOrdersReportJob : IJob
    {
        private MarketplaceWebServiceClient _Client;
        ILog _Log;

        public AmazonAllOrdersReportJob()
        {
            var config = new MarketplaceWebServiceConfig
            {
                //ServiceURL = "https://mws.amazonservices.com"
                ServiceURL = Settings.AmazonReportServiceUrl
            };
            config.SetUserAgentHeader(Settings.ApplicationName, Settings.ApplicationVersion, "C#");
            _Client = new MarketplaceWebServiceClient(Settings.AwsAccessKeyId, Settings.AwsSecretAccessKey, config);

            _Log = LogManager.GetLogger(typeof(AmazonAllOrdersReportJob));
        }

        public void Execute(JobExecutionContext context)
        {
            _Log.Info("Enter Execute.");

            DownloadOrders("US");

            _Log.Info("Exit Execute");
        }

        public bool DownloadOrders(string marketPlace)
        {
            var awsSettings = AwsSettings.GetSettings(marketPlace);

            var dtToday = DateTime.Now;
            var date0 = dtToday.ToString("dd/MM/yyyy");
            DateTime date = DateTime.ParseExact(date0, "dd/MM/yyyy", null);
            var dtFrom = date.AddDays(Settings.requestDays);

            string nextToken = "";
            
            // Get Orders Quantity
            {
                List<AmazonFulfilmentOrder> orderList = new List<AmazonFulfilmentOrder>();

                string strDtFrom = StringHelper.EscapeString(dtFrom.ToString("yyyy-MM-ddTHH:mm:ssZ"));

                string param0 = "CreatedAfter=" + strDtFrom;

                string response = SearchOrders(param0, $"ListOrders", awsSettings);

                if (string.IsNullOrEmpty(response))
                    return false;
                else
                    nextToken = ParseListOrdersData(response, $"ListOrdersResult", awsSettings, marketPlace, orderList);

                int delayTime = int.Parse(ConfigurationManager.AppSettings["DelayTime"]);
                Thread.Sleep(delayTime);

                SaveData(orderList, awsSettings);
            }
            // Getting data with token
            while(!string.IsNullOrEmpty(nextToken))
            {
                List<AmazonFulfilmentOrder> orderList = new List<AmazonFulfilmentOrder>();

                string param0 = $"NextToken=" + nextToken.Replace("=", "%3D").Replace("+", "%2B").Replace("/", "%2F");

                string response = SearchOrdersByToken(param0, $"ListOrdersByNextToken", awsSettings);

                if (string.IsNullOrEmpty(response))
                    break;
                else
                    nextToken = ParseListOrdersData(response, $"ListOrdersByNextTokenResult", awsSettings, marketPlace, orderList);

                int delayTime = int.Parse(ConfigurationManager.AppSettings["DelayTime"]);
                Thread.Sleep(delayTime);

                SaveData(orderList, awsSettings);
            }

            return true;
        }
        private void SaveData(List<AmazonFulfilmentOrder> orderList, AwsSettings awsSettings)
        {
            // Save AmazonOrderId to AmazonOrderList
            foreach (AmazonFulfilmentOrder order in orderList)
            {
                //string orderDate = dtAfter.ToString("yyyy-MM-dd");

                SaveAmazonOrder(order);

                List<AmazonFulfilmentOrderItem> orderItemList = new List<AmazonFulfilmentOrderItem>();

                orderItemList = GetOrderItemScan(order, awsSettings);

                SaveOrderItem(orderItemList);
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
        private void SaveOrderItem(List<AmazonFulfilmentOrderItem> orderItemList)
        {
            foreach (AmazonFulfilmentOrderItem orderItem in orderItemList)
            {
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
                                        }
                                        catch (Exception e) { }
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
                                        }
                                        catch (Exception e) { }
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

        private string ParseListOrdersData(string responseData, string resultElementName, AwsSettings awsSettings, string marketPlace, List<AmazonFulfilmentOrder> orderList)
        {
            string nextToken = "";

            XDocument xmlDoc = XDocument.Parse(responseData);

            if (xmlDoc.Root == null)
                return "";

            // Getting Token
            
            try
            {
                XElement nextTokenNode = xmlDoc.Root
                .Elements().Where(x => x.Name.LocalName == resultElementName)
                .Elements().Where(x => x.Name.LocalName == "NextToken")
                .First();

                nextToken = nextTokenNode.Value;
            }
            catch (Exception ex)
            {
                nextToken = "";
            }

            // Getting Orders
            IEnumerable<XElement> orders = xmlDoc.Root
                .Elements().Where(x => x.Name.LocalName == resultElementName)
                .Elements().Where(x => x.Name.LocalName == "Orders")
                .Elements().Where(x => x.Name.LocalName == "Order");

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

            return nextToken;
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
        private string SearchOrdersByToken(string additionalParam, string action,
            AwsSettings awsSettings)
        {
            var serviceName = $"/Orders/";
            string apiVersion = $"2013-09-01";

            var param = $"AWSAccessKeyId=" + awsSettings.AccessKey;
            param += $"&Action={action}";
            param += $"&{additionalParam}";
            param += $"&SellerId=" + awsSettings.MerchantId;
            param += $"&SignatureMethod=HmacSHA256";
            param += $"&SignatureVersion=2";
            param += $"&Timestamp=" + AwsSettings.TimeStamp;
            param += $"&Version=" + apiVersion;

            string signature = AmazonHelper.SignRequest(param, awsSettings.SecretKey, awsSettings.Url,
               serviceName, AwsSettings.ApiVersion)
               .Replace("=", "%3D").Replace("+", "%2B").Replace("/", "%2F");

            param += "&Signature=" + signature;
            string request = $"https://{awsSettings.Url}{serviceName}{apiVersion}?{param}";
            return AmazonHelper.SendRequest(request);
        }
        private void RequestReport()
        {
            _Log.Info("Enter RequestReport");
            var requestReportRequest = new RequestReportRequest 
            { 
                Merchant = Settings.SellerId, 
                ReportType = ReportTypes._GET_XML_ALL_ORDERS_DATA_BY_ORDER_DATE_, 
                StartDate = DateTime.Now.AddDays(Settings.requestDays),
                EndDate = DateTime.Now
            };

            _Client.RequestReport(requestReportRequest);
            _Log.Info("Exit RequestReport");
        }
                
        private class AmazonFulfilmentOrder
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
        
        private class AmazonFulfilmentOrderItem
        {
            public string AmazonOrderId { get; set; }
            public string Asin { get; set; }
            public string Sku { get; set; }
            public int Quantity { get; set; }
            public string ProductName { get; set; }
            public decimal Price { get; set; }
        }
        
        internal static class ReportTypes
        {
            internal static string _GET_XML_ALL_ORDERS_DATA_BY_ORDER_DATE_ = "_GET_XML_ALL_ORDERS_DATA_BY_ORDER_DATE_";
        }

        internal static class Schedules
        {
            internal static string _12_HOURS_ = "_12_HOURS_";
        }

        
    }
}