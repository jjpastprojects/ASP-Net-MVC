using System;

namespace AmazonReportDownloader
{
    /// <summary>
    /// host the necessary configuration properties
    /// </summary>
    public static class Settings
    {
        /// <summary>
        /// The address of the Amazon Report Service
        /// </summary>
        public static string AmazonReportServiceUrl { get; set; }

        public static int Interval { get; set; }

        public static string AwsAccessKeyId { get; set; }

        public static string AwsSecretAccessKey { get; set; }

        public static string SellerId { get; set; }

        public static string ApplicationName { get; set; }

        public static string ApplicationVersion { get; set; }

        public static string DatabaseConnectionString { get; set; }

        public static int exceptionsDelay { get; set; }

        public static int requestDays { get; set; }
    }
}
