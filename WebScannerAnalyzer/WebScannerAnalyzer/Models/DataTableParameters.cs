
namespace WebScannerAnalyzer.Models
{
    public enum DataTableOrderDirection
    {
        ASC,
        DESC
    }

    public class DataTableOrder
    {
        public int Column { get; set; }
        public DataTableOrderDirection Dir { get; set; }
    }

    public class DataTableSearch
    {
        public string Value { get; set; }
        public bool Regex { get; set; }
    }

    public class DataTableColumn
    {
        public string Data { get; set; }
        public string Name { get; set; }
        public bool Searchable { get; set; }
        public bool Orderable { get; set; }
        public DataTableSearch Search { get; set; }
    }

    public class DataTableParameters
    {
        public int Draw { get; set; }
        public int Start { get; set; }
        public int Length { get; set; }
        public DataTableSearch Search { get; set; }
        public DataTableColumn[] Columns { get; set; }
        public DataTableOrder[] Order { get; set; }
        public bool IsTableInit { get; set; }
    }
}