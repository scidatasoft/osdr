using System.Text.RegularExpressions;

namespace Sds.Osdr.WebApi.Extensions
{
    public static class StringExtension
    {
        public static string ToPascalCase(this string s)
        {
            char[] a = Regex.Replace(s, @"(?<=\.)\w", m => char.ToUpper(m.Value[0]).ToString()).ToCharArray();
            a[0] = char.ToUpper(a[0]);
            return new string(a);
        }
    }
}
