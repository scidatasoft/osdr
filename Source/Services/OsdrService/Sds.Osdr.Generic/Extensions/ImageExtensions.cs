using Sds.Osdr.Generic.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sds.Osdr.Generic.Extensions
{
    public static class ImageExtensions
    {
        public static string GetScale(this Image image)
        {
            var scale = new Dictionary<Func<Domain.ValueObjects.Image, bool>, string>
            {
                { x => x.MimeType.Contains("svg"), "Vector" },
                { x => !x.MimeType.Contains("svg") && x.Width <= 300 , "Small" },
                { x => !x.MimeType.Contains("svg") && x.Width > 300 && x.Width <= 600, "Medium" },
                { x => !x.MimeType.Contains("svg") && x.Width > 600 && x.Width <= 1200, "Large" },
                { x => !x.MimeType.Contains("svg") && x.Width > 1200, "Original" }
            };

            return scale.First(s => s.Key(image)).Value;
        }
    }
}
