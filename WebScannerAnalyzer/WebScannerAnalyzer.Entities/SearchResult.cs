
using System.Collections.Generic;

namespace WebScannerAnalyzer.Entities
{
    public class SearchResult<T>
    {
        public List<T> Data { get; set; }
        public int TotalRecords { get; set; }
    }
}
