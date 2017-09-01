using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using WebScannerAnalyzer.Entities;
using WebScannerAnalyzer.Interfaces;

namespace WebScannerAnalyzer.DataAccessLayer
{
    public class OrderDataAccessLayer: IOrderDataAccessLayer
    {
        public string ConnectionString
        {
            get { return ConfigurationManager.ConnectionStrings["OrderManager"].ConnectionString; }
        }

        public List<Vendor> GetVendorNameList()
        {
            string cmdText = "SELECT VendorId, VendorName FROM [OrderManager].[dbo].[Vendors]  WHERE [isActive]=1 ORDER BY VendorName";
            List<Vendor> result = new List<Vendor>();

            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                using (var command = new SqlCommand(cmdText, connection))
                {
                    command.CommandType = CommandType.Text;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var vendor = new Vendor();
                            vendor.VendorId = int.Parse(reader["VendorID"].ToString());
                            vendor.VendorName = reader["VendorName"].ToString();

                            result.Add(vendor);
                        }
                    }
                }

                connection.Close();
            }
            
            return result;
        }


        public bool ValidateUser(string userName, string password)
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                using (var command = new SqlCommand())
                {
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "dbo.Membership_ValidateUser";

                    command.Parameters.AddWithValue("@userName", userName);
                    command.Parameters.AddWithValue("@password", password);

                    var returnValue = command.Parameters.Add("@RETURN_VALUE", SqlDbType.Int);
                    returnValue.Direction = ParameterDirection.ReturnValue;

                    connection.Open();
                    command.ExecuteNonQuery();
                    connection.Close();

                    int result = Convert.ToInt32(returnValue.Value);
                    return result == 1;
                }
            }
        }

        public SearchResult<Vendor> SearchVendor(string vendorName, int pageNumber, int pageSize, string sortBy, string sortDirection)
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                using (var command = new SqlCommand())
                {
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "usp_search_vendor";

                    if (!string.IsNullOrEmpty(vendorName))
                        command.Parameters.AddWithValue("@VendorName", vendorName);

                    command.Parameters.AddWithValue("@SortBy", sortBy);
                    command.Parameters.AddWithValue("@SortDirection", sortDirection);
                    command.Parameters.AddWithValue("@PageNumber", pageNumber);
                    command.Parameters.AddWithValue("@PageSize", pageSize);

                    List<Vendor> vendors = new List<Vendor>();
                    int totalRecords = 0;

                    connection.Open();
                    var reader = command.ExecuteReader();

                    //read data
                    while (reader.Read())
                    {
                        if (totalRecords == 0)
                            totalRecords = int.Parse(reader["TotalRows"].ToString());

                        var vendor = new Vendor();
                        vendor.VendorId = int.Parse(reader["VendorID"].ToString());
                        vendor.VendorName = reader["VendorName"].ToString();

                        if (!reader.IsDBNull(reader.GetOrdinal("ContactPerson")))
                            vendor.ContactPerson = reader["ContactPerson"].ToString();

                        if (!reader.IsDBNull(reader.GetOrdinal("Email")))
                            vendor.Email = reader["Email"].ToString();

                        if (!reader.IsDBNull(reader.GetOrdinal("DaysToFollow")))
                            vendor.DaysToFollow = int.Parse(reader["DaysToFollow"].ToString());

                        if (!reader.IsDBNull(reader.GetOrdinal("IsDropShipper")))
                            vendor.IsDropShipper = bool.Parse(reader["IsDropShipper"].ToString());

                        if (!reader.IsDBNull(reader.GetOrdinal("Zip")))
                            vendor.Zip = reader["Zip"].ToString();

                        if (!reader.IsDBNull(reader.GetOrdinal("StreetAddress")))
                            vendor.StreetAddress = reader["StreetAddress"].ToString();

                        if (!reader.IsDBNull(reader.GetOrdinal("OptionalAddress")))
                            vendor.OptionalAddress = reader["OptionalAddress"].ToString();

                        if (!reader.IsDBNull(reader.GetOrdinal("City")))
                            vendor.City = reader["City"].ToString();

                        if (!reader.IsDBNull(reader.GetOrdinal("State")))
                            vendor.State = reader["State"].ToString();

                        if (!reader.IsDBNull(reader.GetOrdinal("Country")))
                            vendor.Country = reader["Country"].ToString();

                        if (!reader.IsDBNull(reader.GetOrdinal("CanBarcode")))
                            vendor.CanBarcode = bool.Parse(reader["CanBarcode"].ToString());

                        if (!reader.IsDBNull(reader.GetOrdinal("FreeFreightProgram")))
                            vendor.FreeFreightProgram = reader["FreeFreightProgram"].ToString();

                        if (!reader.IsDBNull(reader.GetOrdinal("DiscountProgram")))
                            vendor.DiscountProgram = reader["DiscountProgram"].ToString();

                        if (!reader.IsDBNull(reader.GetOrdinal("WillUseAmazonUPSLabel")))
                            vendor.WillUseAmazonUPSLabel = reader["WillUseAmazonUPSLabel"].ToString();

                        if (!reader.IsDBNull(reader.GetOrdinal("AvgLeadTimeToShip")))
                            vendor.AvgLeadTimeToShip = int.Parse(reader["AvgLeadTimeToShip"].ToString());

                        if (!reader.IsDBNull(reader.GetOrdinal("AvgDaysInTransit")))
                            vendor.AvgDaysInTransit = int.Parse(reader["AvgDaysInTransit"].ToString());

                        if (!reader.IsDBNull(reader.GetOrdinal("AvgDaysToAcceptGoods")))
                            vendor.AvgDaysToAcceptGoods = int.Parse(reader["AvgDaysToAcceptGoods"].ToString());

                        vendors.Add(vendor);
                    }

                   

                    connection.Close();

                    var result = new SearchResult<Vendor>()
                    {
                        Data = vendors,
                        TotalRecords = totalRecords
                    };

                    return result;
                }
            }
        }

        public Vendor GetVendorById(int vendorId)
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                using (var command = new SqlCommand())
                {
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "usp_get_vendor";

                    command.Parameters.AddWithValue("@VendorId", vendorId);
                    
                    connection.Open();
                    var reader = command.ExecuteReader();

                    Vendor vendor = null;
                    if (reader.Read())
                    {
                        vendor = new Vendor();

                        vendor.VendorId = vendorId;
                        vendor.VendorName = reader["VendorName"].ToString();

                        if (!reader.IsDBNull(reader.GetOrdinal("ContactPerson")))
                            vendor.ContactPerson = reader["ContactPerson"].ToString();

                        if (!reader.IsDBNull(reader.GetOrdinal("Email")))
                            vendor.Email = reader["Email"].ToString();

                        if (!reader.IsDBNull(reader.GetOrdinal("DaysToFollow")))
                            vendor.DaysToFollow = int.Parse(reader["DaysToFollow"].ToString());

                        if (!reader.IsDBNull(reader.GetOrdinal("IsDropShipper")))
                            vendor.IsDropShipper = bool.Parse(reader["IsDropShipper"].ToString());

                        if (!reader.IsDBNull(reader.GetOrdinal("Zip")))
                            vendor.Zip = reader["Zip"].ToString();

                        if (!reader.IsDBNull(reader.GetOrdinal("StreetAddress")))
                            vendor.StreetAddress = reader["StreetAddress"].ToString();

                        if (!reader.IsDBNull(reader.GetOrdinal("OptionalAddress")))
                            vendor.OptionalAddress = reader["OptionalAddress"].ToString();

                        if (!reader.IsDBNull(reader.GetOrdinal("City")))
                            vendor.City = reader["City"].ToString();

                        if (!reader.IsDBNull(reader.GetOrdinal("State")))
                            vendor.State = reader["State"].ToString();

                        if (!reader.IsDBNull(reader.GetOrdinal("Country")))
                            vendor.Country = reader["Country"].ToString();

                        if (!reader.IsDBNull(reader.GetOrdinal("Prefix")))
                            vendor.Prefix = reader["Prefix"].ToString();

                        if (!reader.IsDBNull(reader.GetOrdinal("Method")))
                            vendor.Method = reader["Method"].ToString();

                        if (!reader.IsDBNull(reader.GetOrdinal("InventoryFrom")))
                            vendor.InventoryFrom = reader["InventoryFrom"].ToString();

                        if (!reader.IsDBNull(reader.GetOrdinal("ProcessWith")))
                            vendor.ProcessWith = reader["ProcessWith"].ToString();

                        if (!reader.IsDBNull(reader.GetOrdinal("UseOurAccount")))
                            vendor.UseOurAccount = bool.Parse(reader["UseOurAccount"].ToString());

                        if (!reader.IsDBNull(reader.GetOrdinal("Carrier")))
                            vendor.Carrier = reader["Carrier"].ToString();

                        if (!reader.IsDBNull(reader.GetOrdinal("WSURL")))
                            vendor.WSURL = reader["WSURL"].ToString();

                        if (!reader.IsDBNull(reader.GetOrdinal("WSUser")))
                            vendor.WSUser = reader["WSUser"].ToString();

                        if (!reader.IsDBNull(reader.GetOrdinal("WSPassword")))
                            vendor.WSPassword = reader["WSPassword"].ToString();

                        if (!reader.IsDBNull(reader.GetOrdinal("FTPURL")))
                            vendor.FTPUrl = reader["FTPURL"].ToString();

                        if (!reader.IsDBNull(reader.GetOrdinal("FTPUser")))
                            vendor.FTPUser = reader["FTPUser"].ToString();

                        if (!reader.IsDBNull(reader.GetOrdinal("FTPPassword")))
                            vendor.FTPPassword = reader["FTPPassword"].ToString();

                        if (!reader.IsDBNull(reader.GetOrdinal("Instructions")))
                            vendor.Instructions = reader["Instructions"].ToString();

                        if (!reader.IsDBNull(reader.GetOrdinal("RANeeded")))
                            vendor.RANeeded = bool.Parse(reader["RANeeded"].ToString());

                        if (!reader.IsDBNull(reader.GetOrdinal("CanBarcode")))
                            vendor.CanBarcode = bool.Parse(reader["CanBarcode"].ToString());

                        if (!reader.IsDBNull(reader.GetOrdinal("FreeFreightProgram")))
                            vendor.FreeFreightProgram = reader["FreeFreightProgram"].ToString();

                        if (!reader.IsDBNull(reader.GetOrdinal("DiscountProgram")))
                            vendor.DiscountProgram = reader["DiscountProgram"].ToString();

                        if (!reader.IsDBNull(reader.GetOrdinal("WillUseAmazonUPSLabel")))
                            vendor.WillUseAmazonUPSLabel = reader["WillUseAmazonUPSLabel"].ToString();

                        if (!reader.IsDBNull(reader.GetOrdinal("GeneralGuidlines")))
                            vendor.GeneralGuidlines = reader["GeneralGuidlines"].ToString();

                        if (!reader.IsDBNull(reader.GetOrdinal("AvgLeadTimeToShip")))
                            vendor.AvgLeadTimeToShip = int.Parse(reader["AvgLeadTimeToShip"].ToString());

                        if (!reader.IsDBNull(reader.GetOrdinal("AvgDaysInTransit")))
                            vendor.AvgDaysInTransit = int.Parse(reader["AvgDaysInTransit"].ToString());

                        if (!reader.IsDBNull(reader.GetOrdinal("AvgDaysToAcceptGoods")))
                            vendor.AvgDaysToAcceptGoods = int.Parse(reader["AvgDaysToAcceptGoods"].ToString());

                        if (!reader.IsDBNull(reader.GetOrdinal("CanPrep")))
                            vendor.CanPrep = bool.Parse(reader["CanPrep"].ToString());

                        if (!reader.IsDBNull(reader.GetOrdinal("BarcodeFee")))
                            vendor.BarcodeFee = decimal.Parse(reader["BarcodeFee"].ToString());

                        if (!reader.IsDBNull(reader.GetOrdinal("PrepFee")))
                            vendor.PrepFee = decimal.Parse(reader["PrepFee"].ToString());

                    }

                    connection.Close();
                    return vendor;
                }
            }
        }

        public void UpdateVendor(Vendor vendor)
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                using (var command = new SqlCommand())
                {
                    command.Connection = connection;
                    command.CommandText = "usp_update_vendor";
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue("@VendorId", vendor.VendorId);
                    command.Parameters.AddWithValue("@VendorName", vendor.VendorName);

                    if (!string.IsNullOrEmpty(vendor.ContactPerson))
                        command.Parameters.AddWithValue("@ContactPerson", vendor.ContactPerson);

                    if (!string.IsNullOrEmpty(vendor.Email))
                        command.Parameters.AddWithValue("@Email", vendor.Email);

                    if (vendor.DaysToFollow.HasValue)
                        command.Parameters.AddWithValue("@DaysToFollow", vendor.DaysToFollow.Value);

                    if (vendor.IsDropShipper.HasValue)
                        command.Parameters.AddWithValue("@IsDropShipper", vendor.IsDropShipper.Value);

                    if (!string.IsNullOrEmpty(vendor.Zip))
                        command.Parameters.AddWithValue("@Zip", vendor.Zip);

                    if (!string.IsNullOrEmpty(vendor.StreetAddress))
                        command.Parameters.AddWithValue("@StreetAddress", vendor.StreetAddress);

                    if (!string.IsNullOrEmpty(vendor.OptionalAddress))
                        command.Parameters.AddWithValue("@OptionalAddress", vendor.OptionalAddress);

                    if (!string.IsNullOrEmpty(vendor.City))
                        command.Parameters.AddWithValue("@City", vendor.City);

                    if (!string.IsNullOrEmpty(vendor.State))
                        command.Parameters.AddWithValue("@State", vendor.State);

                    if (!string.IsNullOrEmpty(vendor.Country))
                        command.Parameters.AddWithValue("@Country", vendor.Country);

                    if (!string.IsNullOrEmpty(vendor.Prefix))
                        command.Parameters.AddWithValue("@Prefix", vendor.Prefix);

                    if (!string.IsNullOrEmpty(vendor.Method))
                        command.Parameters.AddWithValue("@Method", vendor.Method);

                    if (!string.IsNullOrEmpty(vendor.InventoryFrom))
                        command.Parameters.AddWithValue("@InventoryFrom", vendor.InventoryFrom);

                    if (!string.IsNullOrEmpty(vendor.ProcessWith))
                        command.Parameters.AddWithValue("@ProcessWith", vendor.ProcessWith);

                    if (vendor.UseOurAccount.HasValue)
                        command.Parameters.AddWithValue("@UseOurAccount", vendor.UseOurAccount.Value);

                    if (!string.IsNullOrEmpty(vendor.Carrier))
                        command.Parameters.AddWithValue("@Carrier", vendor.Carrier);

                    if (!string.IsNullOrEmpty(vendor.WSURL))
                        command.Parameters.AddWithValue("@WSURL", vendor.WSURL);

                    if (!string.IsNullOrEmpty(vendor.WSUser))
                        command.Parameters.AddWithValue("@WSUser", vendor.WSUser);

                    if (!string.IsNullOrEmpty(vendor.WSPassword))
                        command.Parameters.AddWithValue("@wSPassword", vendor.WSPassword);

                    if (!string.IsNullOrEmpty(vendor.FTPUrl))
                        command.Parameters.AddWithValue("@FTPUrl", vendor.FTPUrl);

                    if (!string.IsNullOrEmpty(vendor.FTPUser))
                        command.Parameters.AddWithValue("@FTPUser", vendor.FTPUser);

                    if (!string.IsNullOrEmpty(vendor.FTPPassword))
                        command.Parameters.AddWithValue("@FTPPassword", vendor.FTPPassword);

                    if (!string.IsNullOrEmpty(vendor.Instructions))
                        command.Parameters.AddWithValue("@Instructions", vendor.Instructions);

                    if (vendor.RANeeded.HasValue)
                        command.Parameters.AddWithValue("@RANeeded", vendor.RANeeded.Value);

                    if (vendor.CanBarcode.HasValue)
                        command.Parameters.AddWithValue("@CanBarcode", vendor.CanBarcode.Value);

                    if (!string.IsNullOrEmpty(vendor.FreeFreightProgram))
                        command.Parameters.AddWithValue("@FreeFreightProgram", vendor.FreeFreightProgram);

                    if (!string.IsNullOrEmpty(vendor.DiscountProgram))
                        command.Parameters.AddWithValue("@DiscountProgram", vendor.DiscountProgram);

                    if (!string.IsNullOrEmpty(vendor.WillUseAmazonUPSLabel))
                        command.Parameters.AddWithValue("@WillUseAmazonUPSLabel", vendor.WillUseAmazonUPSLabel);

                    if (!string.IsNullOrEmpty(vendor.GeneralGuidlines))
                        command.Parameters.AddWithValue("@GeneralGuidlines", vendor.GeneralGuidlines);

                    if (vendor.AvgLeadTimeToShip.HasValue)
                        command.Parameters.AddWithValue("@AvgLeadTimeToShip", vendor.AvgLeadTimeToShip.Value);

                    if (vendor.AvgDaysInTransit.HasValue)
                        command.Parameters.AddWithValue("@AvgDaysInTransit", vendor.AvgDaysInTransit.Value);

                    if (vendor.AvgDaysToAcceptGoods.HasValue)
                        command.Parameters.AddWithValue("@AvgDaysToAcceptGoods", vendor.AvgDaysToAcceptGoods.Value);

                    if (vendor.CanPrep.HasValue)
                        command.Parameters.AddWithValue("@CanPrep", vendor.CanPrep);

                    if (vendor.BarcodeFee.HasValue)
                        command.Parameters.AddWithValue("@BarcodeFee", vendor.BarcodeFee);

                    if (vendor.PrepFee.HasValue)
                        command.Parameters.AddWithValue("@PrepFee", vendor.PrepFee);

                    connection.Open();
                    command.ExecuteNonQuery();
                    connection.Close();
                }
            }
        }
    }
}
