using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using WebAnalyzerScanner.Common;

namespace WebScannerAnalyzer.BusinessLayer.AWS
{
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
                        Logger.Error("AmazonHelper::SendRequest timeout.");
                    }
                    else
                    {
                        HttpWebResponse httpResponse = (HttpWebResponse)response;
                        Console.WriteLine("Error code: {0}\n", httpResponse.StatusCode);

                        using (Stream data = response.GetResponseStream())
                        using (var reader = new StreamReader(data))
                        {
                            string text = reader.ReadToEnd();
                            Logger.Error(text);
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
