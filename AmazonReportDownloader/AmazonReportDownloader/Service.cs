using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using Quartz;
using Quartz.Impl;
using System.Configuration;
using System.Reflection;
using System.IO;
using log4net.Config;
using log4net;
using System.Data.SqlClient;

namespace AmazonReportDownloader
{
    public partial class Service : ServiceBase
    {
        private ISchedulerFactory _SchedulerFactory;
        private IScheduler _AmazonJobScheduler;
        private readonly ILog _Log;

        public Service()
        {
            InitializeComponent();

            //string configFileDirectoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase);
            //string configFileName = "app.config";
            //string configFile = Path.Combine(configFileDirectoryName, configFileName);
            
            _Log = LogManager.GetLogger(typeof(Service));
        }

        public void Debug()
        {
            _Log.Info("Debug");
            LoadSettings();
            AmazonAllOrdersReportJob job = new AmazonAllOrdersReportJob();
            job.Execute(null);
        }

        //[Conditional("DEBUG_SERVICE")]
        protected override void OnStart(string[] args)
        {
            _Log.Info("OnStart");
            //Debugger.Launch();

            /* load the settings from the configuration file */
            LoadSettings();

            _SchedulerFactory = new StdSchedulerFactory();
            _AmazonJobScheduler = _SchedulerFactory.GetScheduler();

            _AmazonJobScheduler.Start();

            Trigger trigger = TriggerUtils.MakeHourlyTrigger(Settings.Interval);
            JobDetail amazonReportJob = new JobDetail("AmazonAllOrdersReport", typeof(AmazonAllOrdersReportJob));

            //when the trigger will start working,
            //TODO: change it to now, its just for testing purpose,
            trigger.StartTimeUtc = DateTime.UtcNow;
            trigger.Name = "AmazonAllOrdersReportTrigger";

            _AmazonJobScheduler.ScheduleJob(amazonReportJob, trigger);
        }

        protected override void OnStop()
        {
            if (!_AmazonJobScheduler.IsShutdown && _AmazonJobScheduler.IsStarted)
            {
                _AmazonJobScheduler.Shutdown();
                _Log.Info("OnStop");
            }
        }

        private void LoadSettings()
        {
            _Log.Info("Load Settings");
            try
            {
                Settings.exceptionsDelay = int.Parse(ConfigurationManager.AppSettings["exceptionsDelay"]);

                Settings.Interval = int.Parse(ConfigurationManager.AppSettings["Interval"]);
                Settings.AmazonReportServiceUrl = ConfigurationManager.AppSettings["AmazonReportServiceUrl"];
                Settings.ApplicationName = ConfigurationManager.AppSettings["ApplicationName"];
                Settings.ApplicationVersion = ConfigurationManager.AppSettings["ApplicationVersion"];
                Settings.AwsAccessKeyId = ConfigurationManager.AppSettings["AwsAccessKeyId"];
                Settings.AwsSecretAccessKey = ConfigurationManager.AppSettings["AwsSecretAccessKey"];
                Settings.SellerId = ConfigurationManager.AppSettings["SellerId"];

                Settings.DatabaseConnectionString = ConfigurationManager.ConnectionStrings["OrderManager"].ConnectionString;

                // Read howFarBack days
                //*
                string readValue = ConfigurationManager.AppSettings["requestDays"];
                int parsedValue;

                if (int.TryParse(readValue, out parsedValue))
                    Settings.requestDays = parsedValue;
                else
                {
                    Settings.requestDays = -7;
                }
                //*/
                /*
                int requestDays = GetRequestDays();
                Settings.requestDays = requestDays == 0 ? 7 : requestDays;
                //*/
            }
            catch (Exception ex)
            {
                _Log.Error(ex.Message);
                throw ex;
            }
        }

        private int GetRequestDays()
        {
            int result = 0;

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
                                result = int.Parse(reader["HowFarBack"].ToString());
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
    }
}
