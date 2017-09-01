using System;
using System.Configuration;

namespace WebScannerAnalyzer.BusinessLayer.AWS
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
            get { return ConfigurationManager.AppSettings["SERVICE_NAME_PRODUCTS"]; }
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
}
