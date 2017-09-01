
namespace WebAnalyzerScanner.Common
{
    public static class ExtensionMethods
    {
        public static double SafeCastToDouble(this string source)
        {
            double result;
            double.TryParse(source, out result);

            return result;
        }
    }
}
