using System.Collections.Generic;
using WebScannerAnalyzer.Entities;

namespace WebScannerAnalyzer.Interfaces
{
    public interface IOrderDataAccessLayer
    {
        bool ValidateUser(string userName, string password);
        List<Vendor> GetVendorNameList();
        SearchResult<Vendor> SearchVendor(string vendorName, int pageNumber, int pageSize, string sortBy, string sortDirection);
        Vendor GetVendorById(int vendorId);
        void UpdateVendor(Vendor vendor);
        //ScheduledGeneralSettings GetScheduledGeneralSettings();
    }
}
