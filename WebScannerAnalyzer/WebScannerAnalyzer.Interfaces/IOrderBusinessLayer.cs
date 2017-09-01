using System.Collections.Generic;
using WebScannerAnalyzer.Entities;

namespace WebScannerAnalyzer.Interfaces
{
    public interface IOrderBusinessLayer
    {
        List<Vendor> GetVendorNameList();
        bool ValidateUser(string userName, string password);
        SearchResult<Vendor> SearchVendor(string vendorName, int pageNumber, int pageSize, string sortBy, string sortDirection);
        Vendor GetVendorById(int vendorId);
        void UpdateVendor(Vendor vendor);
        //ScheduledGeneralSettings ScheduledGeneralSettings();
    }
}
