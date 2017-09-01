using System;
using System.Configuration;
using log4net;
using log4net.Appender;
using log4net.Config;

namespace WebAnalyzerScanner.Common
{
    public static class Logger
    {
        //For better performance, we should check to see if the log level is enabled before logging.
        //if(_logger.is***Enabled)
        //    _logger.***();
        //See https://logging.apache.org/log4net/release/manual/internals.html for more details

        private static ILog _logger;
        static Logger()
        {
            XmlConfigurator.Configure();
            string loggerName = ConfigurationManager.AppSettings["LoggerName"];
            if (string.IsNullOrEmpty(loggerName))
                loggerName = "Logger"; //default logger's name

            _logger = LogManager.GetLogger(loggerName);
        }

        public static void Error(Exception exception)
        {
            if (_logger.IsErrorEnabled)
            {
                _logger.Error(exception);
            }

        }

        public static void Error(string format, params object[] args)
        {
            if (_logger.IsErrorEnabled)
                _logger.ErrorFormat(format, args);
        }

        public static void Info(Exception exception)
        {
            if (_logger.IsInfoEnabled)
            {
                _logger.Info(exception);
            }
        }

        public static void Info(string format, params object[] args)
        {
            if (_logger.IsInfoEnabled)
                _logger.InfoFormat(format, args);
        }

        public static void Warn(Exception exception)
        {
            if (_logger.IsWarnEnabled)
            {
                _logger.Warn(exception);
            }

        }

        public static void Warn(string format, params object[] args)
        {
            if (_logger.IsWarnEnabled)
                _logger.WarnFormat(format, args);
        }

        public static void Fatal(Exception exception)
        {
            if (_logger.IsFatalEnabled)
            {
                _logger.Fatal(exception);
            }

        }

        public static void Fatal(string format, params object[] args)
        {
            if (_logger.IsFatalEnabled)
                _logger.FatalFormat(format, args);
        }

        public static void Debug(Exception exception)
        {
            if (_logger.IsDebugEnabled)
            {
                _logger.Debug(exception);
            }

        }

        public static void Debug(string format, params object[] args)
        {
            if (_logger.IsDebugEnabled)
                _logger.DebugFormat(format, args);
        }

        public static void AddAppender(IAppender appender)
        {
            ((log4net.Repository.Hierarchy.Logger)_logger.Logger).AddAppender(appender);
        }
    }
}
