using System;
using System.Collections.Generic;
using WebAnalyzerScanner.Common;
using WebScannerAnalyzer.DataAccessLayer;
using WebScannerAnalyzer.Entities;
using WebScannerAnalyzer.Interfaces;

namespace WebScannerAnalyzer.BusinessLayer
{
    public class OrderBusinessLayer: IOrderBusinessLayer
    {
        //apply DI?
        private IOrderDataAccessLayer _dataAccessLayer;
        private IOrderDataAccessLayer DataAccessLayer
        {
            get
            {
                if (_dataAccessLayer == null)
                    _dataAccessLayer = new OrderDataAccessLayer();

                return _dataAccessLayer;
            }
        }

        public List<Vendor> GetVendorNameList()
        {
            //TODO: Add caching

            List<Vendor> vendors = DataAccessLayer.GetVendorNameList();
            
            //Add blank item to clear the selection
            vendors.Insert(0, new Vendor());

            return vendors;
        }

        public bool ValidateUser(string userName, string password)
        {
            return DataAccessLayer.ValidateUser(userName, password);
        }

        public SearchResult<Vendor> SearchVendor(string vendorName, int pageNumber, int pageSize, string sortBy, string sortDirection)
        {
            return DataAccessLayer.SearchVendor(vendorName, pageNumber, pageSize, sortBy, sortDirection);
        }

        public Vendor GetVendorById(int vendorId)
        {
            return DataAccessLayer.GetVendorById(vendorId);
        }

        public void UpdateVendor(Vendor vendor)
        {
            DataAccessLayer.UpdateVendor(vendor);
        }
    }
}
