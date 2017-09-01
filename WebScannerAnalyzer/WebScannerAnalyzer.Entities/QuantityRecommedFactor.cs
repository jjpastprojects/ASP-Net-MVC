using System.ComponentModel.DataAnnotations;

namespace WebScannerAnalyzer.Entities
{
    public class QuantityRecommedFactor
    {
        public int HowlongToStockFor { get; set; }
        public int HowFarBack { get; set; }
        public decimal MinProfit { get; set; }
        public string CategoryRankings { get; set; }
        public int AvgDaysInTransit { get; set; }
        public int AvgLeadTimeToShip { get; set; }
        public int AvgDaysToAcceptGoods { get; set; }
    }

}
