using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Security.Cryptography;
using System.IO;
using System.Net;

namespace AmazonReportDownloader
{
    public class AwsSettings
    {
        private AwsSettings()
        {

        }

        public static string ApiVersion
        {
            get { return ConfigurationManager.AppSettings["API_VERSION"]; }
        }

        public static string ServiceName
        {
            get { return ConfigurationManager.AppSettings["SERVICE_NAME_ORDERS"]; }
        }

        public static string TimeStamp
        {
            get
            {
                return DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd") + "T" +
                       DateTime.Now.ToUniversalTime().ToString("HH:mm:ss").Replace(":", "%3A") + "Z";
            }
        }

        public string MerchantId { get; set; }
        public string MarketPlaceId { get; set; }
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
        public string Url { get; set; }

        public static AwsSettings GetSettings(string marketPlace)
        {
            var settings = new AwsSettings();

            switch (marketPlace.ToUpper())
            {
                case "US":
                    {
                        settings.MerchantId = ConfigurationManager.AppSettings["US_MERCHANT_ID"];
                        settings.MarketPlaceId = ConfigurationManager.AppSettings["US_MARKETPLACE_ID"];
                        settings.AccessKey = ConfigurationManager.AppSettings["US_AWSACCESSKEY_ID"];
                        settings.SecretKey = ConfigurationManager.AppSettings["US_SECRET_KEY"];
                        settings.Url = ConfigurationManager.AppSettings["AMAZON_US_URL"];
                        break;
                    }
                case "EU":
                    {
                        settings.MerchantId = ConfigurationManager.AppSettings["EU_MERCHANT_ID"];
                        settings.MarketPlaceId = ConfigurationManager.AppSettings["EU_MARKETPLACE_ID"];
                        settings.AccessKey = ConfigurationManager.AppSettings["EU_AWSACCESSKEY_ID"];
                        settings.SecretKey = ConfigurationManager.AppSettings["EU_SECRET_KEY"];
                        settings.Url = ConfigurationManager.AppSettings["AMAZON_EU_URL"];
                        break;
                    }
                case "CA":
                    {
                        settings.MerchantId = ConfigurationManager.AppSettings["CA_MERCHANT_ID"];
                        settings.MarketPlaceId = ConfigurationManager.AppSettings["CA_MARKETPLACE_ID"];
                        settings.AccessKey = ConfigurationManager.AppSettings["CA_AWSACCESSKEY_ID"];
                        settings.SecretKey = ConfigurationManager.AppSettings["CA_SECRET_KEY"];
                        settings.Url = ConfigurationManager.AppSettings["AMAZON_CA_URL"];
                        break;
                    }
                case "MX":
                    {
                        settings.MerchantId = ConfigurationManager.AppSettings["MX_MERCHANT_ID"];
                        settings.MarketPlaceId = ConfigurationManager.AppSettings["MX_MARKETPLACE_ID"];
                        settings.AccessKey = ConfigurationManager.AppSettings["MX_AWSACCESSKEY_ID"];
                        settings.SecretKey = ConfigurationManager.AppSettings["MX_SECRET_KEY"];
                        settings.Url = ConfigurationManager.AppSettings["AMAZON_MX_URL"];
                        break;
                    }
            }

            return settings;
        }
    }

    public static class StringHelper
    {
        public static string EscapeString(string source)
        {
            source = source.Replace("%", "%25");
            source = source.Replace(" ", "%20");
            //source = source.Replace("!", "%21");
            source = source.Replace("\"", "%22");
            source = source.Replace("#", "%23");
            source = source.Replace("$", "%24");
            source = source.Replace("&", "%26");
            source = source.Replace("'", "%27");
            source = source.Replace("(", "%28");
            source = source.Replace(")", "%29");
            source = source.Replace("*", "%2A");
            source = source.Replace("+", "%2B");
            source = source.Replace(",", "%2C");
            //source = source.Replace("-", "%2D");
            //source = source.Replace(".", "%2E");
            source = source.Replace("/", "%2F");
            source = source.Replace("\t", "%09");
            source = source.Replace(":", "%3A");
            source = source.Replace(";", "%3B");
            source = source.Replace("<", "%3C");
            source = source.Replace("=", "%3D");
            source = source.Replace(">", "%3E");
            source = source.Replace("?", "%3F");
            source = source.Replace("@", "%40");
            source = source.Replace("\\", "%5C");
            source = source.Replace("[", "%5B");
            source = source.Replace("]", "%5D");
            source = source.Replace("^", "%5E");
            source = source.Replace("`", "%60");
            source = source.Replace("{", "%7B");
            source = source.Replace("|", "%7C");
            source = source.Replace("}", "%7D");

            if (source.IndexOf("%09", StringComparison.Ordinal) == 0)
            {
                source = source.Remove(0, 3);
            }

            return source;

        }
    }

    internal class AmazonHelper
    {
        public static string SignRequest(string param, string key, string serviceUrl, string serviceName, string apiVersion)
        {
            var mac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            string fullRequest = $"POST\n{serviceUrl}\n{serviceName}{apiVersion}\n{param}";
            var hashed = mac.ComputeHash(Encoding.UTF8.GetBytes(fullRequest));

            return Convert.ToBase64String(hashed);
        }

        public static string SendRequest(string requestUrl)
        {
            try
            {
                WebRequest request = WebRequest.Create(requestUrl);
                request.Method = "POST";

                WebResponse response = request.GetResponse();
                StreamReader streamReader = new StreamReader(response.GetResponseStream());
                string responseContent = streamReader.ReadToEnd();
                streamReader.Close();

                return responseContent;
            }
            catch (WebException e)
            {
                using (WebResponse response = e.Response)
                {
                    if (e.Status == WebExceptionStatus.Timeout)
                    {
                        //Logger.Error("AmazonHelper::SendRequest timeout.");
                    }
                    else
                    {
                        HttpWebResponse httpResponse = (HttpWebResponse)response;
                        Console.WriteLine("Error code: {0}\n", httpResponse.StatusCode);

                        using (Stream data = response.GetResponseStream())
                        using (var reader = new StreamReader(data))
                        {
                            string text = reader.ReadToEnd();
                            //Logger.Error(text);
                        }
                    }
                }

                return null;
            }
        }

        public static string GetAmazoneLink(string marketPlace, string asin)
        {
            switch (marketPlace.ToUpper())
            {
                case "US":
                    {
                        return "http://www.amazon.com/gp/product/" + asin;
                    }
                case "EU":
                    {
                        return "http://www.amazon.co.uk/gp/product/" + asin;
                    }
                case "CA":
                    {
                        return "http://www.amazon.ca/gp/product/" + asin;
                    }
                case "MX":
                    {
                        return "http://www.amazon.com.mx/gp/product/" + asin;
                    }
                default:
                    return "http://www.amazon.com/gp/product/" + asin;
            }
        }
    }
}
