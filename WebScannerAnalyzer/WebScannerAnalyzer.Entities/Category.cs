using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebScannerAnalyzer.Entities
{
    public class CategoryRange
    {
        public int High { get; set; }
        public int Low { get; set; }
        public int QTYToOrder { get; set; }

        public CategoryRange ()
        {
            High = 0;
            Low = 0;
            QTYToOrder = 0;
        }
    }

    public class Category
    {
        public string Name { get; set; }
        public int MaxRanking { get; set; }
        public List<CategoryRange> Ranges { get; set; }

        public Category()
        {
            Ranges = new List<CategoryRange>();
            Name = "";
            MaxRanking = 0;
        }
    }

}
