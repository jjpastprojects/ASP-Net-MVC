using System;
using System.Collections.Generic;
using WebAnalyzerScanner.Common;
using WebScannerAnalyzer.DataAccessLayer;
using WebScannerAnalyzer.Entities;
using WebScannerAnalyzer.Interfaces;

namespace WebScannerAnalyzer.BusinessLayer
{
    public class ScheduledBusinessLayer: IScheduledBusinessLayer
    {
        //apply DI?
        private IScheduledDataAccessLayer _dataAccessLayer;
        private IScheduledDataAccessLayer DataAccessLayer
        {
            get
            {
                if (_dataAccessLayer == null)
                    _dataAccessLayer = new ScheduledDataAccessLayer();

                return _dataAccessLayer;
            }
        }

    }
}
