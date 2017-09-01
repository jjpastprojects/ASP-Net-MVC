using System;
using System.Collections.Generic;
using System.Linq;
using log4net;
using System.Threading;

namespace AmazonReportDownloader
{
    public static class Extensions
    {
        public static void RepeatUntillSuccess(Action action, ILog log)
        {
            bool actionResult = false;

            do
            {
                try
                {
                    action();
                    /* if no exception is throw then sucess */
                    actionResult = true;
                }
                catch (Exception ex)
                {
                    log.Error(ex.Message);
                    actionResult = false;
                    Thread.Sleep(TimeSpan.FromMinutes(Settings.exceptionsDelay));
                }
            } while (actionResult != true);
        }

        public static T RepeatUntillSuccess<T>(Func<T> func, ILog log)
        {
            bool funcResult = false;
            T result = default(T);
            do
            {
                try
                {
                    result = func();
                    funcResult = true;
                }
                catch (Exception ex)
                {
                    log.Error(ex.Message);
                    funcResult = false;
                    Thread.Sleep(TimeSpan.FromMinutes(Settings.exceptionsDelay));
                }
            } while (funcResult != true);

            return result;
        }
    }
}
