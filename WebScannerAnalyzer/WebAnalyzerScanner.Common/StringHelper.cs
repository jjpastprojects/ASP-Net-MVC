using System;

namespace WebAnalyzerScanner.Common
{
    public static class StringHelper
    {
        public static string UnescapeXMLValue(string xmlString)
        {
            if (xmlString == null)
                throw new ArgumentNullException("xmlString");

            return xmlString.Replace("&apos;", "'").Replace("&quot;", "\"").Replace("&gt;", ">").Replace("&lt;", "<").Replace("&amp;", "&");
        }

        public static string EscapeXMLValue(string xmlString)
        {

            if (xmlString == null)
                throw new ArgumentNullException("xmlString");
       
            return xmlString.Replace("'", "&apos;").Replace("&", "&amp;");
        }

        public static string EscapeString(string source)
        {
            source = source.Replace("%", "%25");
            source = source.Replace(" ", "%20");
            //source = source.Replace("!", "%21");
            source = source.Replace("\"", "%22");
            source = source.Replace("#", "%23");
            source = source.Replace("$", "%24");
            source = source.Replace("&", "%26");
            source = source.Replace("'", "%27");
            source = source.Replace("(", "%28");
            source = source.Replace(")", "%29");
            source = source.Replace("*", "%2A");
            source = source.Replace("+", "%2B");
            source = source.Replace(",", "%2C");
            //source = source.Replace("-", "%2D");
            //source = source.Replace(".", "%2E");
            source = source.Replace("/", "%2F");
            source = source.Replace("\t", "%09");
            source = source.Replace(":", "%3A");
            source = source.Replace(";", "%3B");
            source = source.Replace("<", "%3C");
            source = source.Replace("=", "%3D");
            source = source.Replace(">", "%3E");
            source = source.Replace("?", "%3F");
            source = source.Replace("@", "%40");
            source = source.Replace("\\", "%5C");
            source = source.Replace("[", "%5B");
            source = source.Replace("]", "%5D");
            source = source.Replace("^", "%5E");
            source = source.Replace("`", "%60");
            source = source.Replace("{", "%7B");
            source = source.Replace("|", "%7C");
            source = source.Replace("}", "%7D");

            if (source.IndexOf("%09", StringComparison.Ordinal) == 0)
            {
                source = source.Remove(0, 3);
            }

            return source;

        }
    }
}
