using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using WebScannerAnalyzer.Entities;
using WebScannerAnalyzer.Interfaces;

namespace WebScannerAnalyzer.DataAccessLayer
{
    public class ScheduledDataAccessLayer: IScheduledDataAccessLayer
    {
        public string ConnectionString
        {
            get { return ConfigurationManager.ConnectionStrings["OrderManager"].ConnectionString; }
        }
        
    }
}
