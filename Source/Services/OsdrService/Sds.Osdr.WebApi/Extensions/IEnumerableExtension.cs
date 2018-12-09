using System.Collections.Generic;
using System.Linq;

namespace Sds.Osdr.WebApi.Extensions
{
    public static class IEnumerableExtension
    {
        public static string ToMongoDBProjection(this IEnumerable<string> strings)
        {
            return strings?.Any() ?? false ? $"{{{string.Join(",", strings.Select(s => $"{s.ToPascalCase()}:1"))}}}" : "{IsDeleted:0}";
        }
    }
}
