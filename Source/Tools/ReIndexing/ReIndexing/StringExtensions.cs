using System;
using System.Text.RegularExpressions;

namespace ReIndexing
{
    public static class StringExtensions
    {
        public static string ToPascalCase(this string s)
        {
            char[] a = s.ToCharArray();
            a[0] = char.ToUpper(a[0]);
            return new string(a);
        }

        public static string ToConnectionString(this string s)
        {
            return Regex.Match(s, @"^%\w+%$").Success ? Environment.ExpandEnvironmentVariables(s) : s;
        }
    }
}
