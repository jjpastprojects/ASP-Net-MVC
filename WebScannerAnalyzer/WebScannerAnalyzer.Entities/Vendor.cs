using System.ComponentModel.DataAnnotations;

namespace WebScannerAnalyzer.Entities
{
    public class Vendor
    {
        [Key]
        public int VendorId { get; set; }

        [Required]
        [MaxLength(50)]
        public string VendorName { get; set; }

        [MaxLength(50)]
        public string ContactPerson { get; set; }

        [MaxLength(150)]
        public string Email { get; set; }

        public int? DaysToFollow { get; set; }
        public bool? IsDropShipper { get; set; }

        [MaxLength(7)]
        public string Zip { get; set; }

        [MaxLength(255)]
        public string StreetAddress { get; set; }

        [MaxLength(255)]
        public string OptionalAddress { get; set; }

        [MaxLength(255)]
        public string City { get; set; }

        [MaxLength(50)]
        public string State { get; set; }

        [MaxLength(50)]
        public string Country { get; set; }

        [MaxLength(50)]
        public string Prefix { get; set; }

        [MaxLength(50)]
        public string Method { get; set; }

        [MaxLength(50)]
        public string InventoryFrom { get; set; }

        [MaxLength(50)]
        public string ProcessWith { get; set; }

        public bool? UseOurAccount { get; set; }

        [MaxLength(50)]
        public string Carrier { get; set; }

        [MaxLength(150)]
        public string WSURL { get; set; }

        [MaxLength(50)]
        public string WSUser { get; set; }

        [MaxLength(50)]
        public string WSPassword { get; set; }

        [MaxLength(150)]
        public string FTPUrl { get; set; }

        [MaxLength(50)]
        public string FTPUser { get; set; }

        [MaxLength(50)]
        public string FTPPassword { get; set; }

        [MaxLength(2000)]
        public string Instructions { get; set; }

        public bool? RANeeded { get; set; }
        public bool? CanBarcode { get; set; }

        [MaxLength(50)]
        public string FreeFreightProgram { get; set; }

        [MaxLength(100)]
        public string DiscountProgram { get; set; }

        [MaxLength(15)]
        public string WillUseAmazonUPSLabel { get; set; }

        [MaxLength(2000)]
        public string GeneralGuidlines { get; set; }

        public int? AvgLeadTimeToShip { get; set; }
        public int? AvgDaysInTransit { get; set; }
        public int? AvgDaysToAcceptGoods { get; set; }

        public bool? CanPrep { get; set; }
        public decimal? BarcodeFee { get; set; }
        public decimal? PrepFee { get; set; }
        
        public int TotalDaysFromOrderToSell
        {
            get
            {
                int result = 0;
                result += AvgDaysInTransit ?? 0;
                result += AvgLeadTimeToShip ?? 0;
                result += AvgDaysToAcceptGoods ?? 0;

                return result;
            }
        }

        public Vendor()
        {
            VendorId = 0;
            VendorName = "";
            ContactPerson = "";
            Email = "";
            DaysToFollow = 0;
            IsDropShipper = false;
            Zip = "";
            StreetAddress = "";
            OptionalAddress = "";
            City = "";
            State = "";
            Country = "";
            Prefix = "";
            Method = "";
            InventoryFrom = "";
            ProcessWith = "";
            UseOurAccount = false;
            Carrier = "";
            WSURL = "";
            WSUser = "";
            WSPassword = "";
            FTPUrl = "";
            FTPUser = "";
            FTPPassword = "";
            Instructions = "";
            RANeeded = false;
            CanBarcode = false;
            FreeFreightProgram = "";
            DiscountProgram = "";
            WillUseAmazonUPSLabel = "";
            GeneralGuidlines = "";
            AvgLeadTimeToShip = 0;
            AvgDaysInTransit = 0;
            AvgDaysToAcceptGoods = 0;
            CanPrep = false;
            BarcodeFee = 0;
            PrepFee = 0;
        }
    }
    
}
