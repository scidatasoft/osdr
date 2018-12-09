using System.Collections.Generic;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Sds.Osdr.WebApi.Swagger
{
    public class LowercaseDocumentFilter : IDocumentFilter
    {
        public void Apply(SwaggerDocument swaggerDoc, DocumentFilterContext context)
        {
            var paths = swaggerDoc.Paths;

            //	generate the new keys
            var newPaths = new Dictionary<string, PathItem>();
            var keysForRemove = new List<string>();
            foreach (var path in paths)
            {
                var newKey = path.Key.ToLower();
                if (newKey != path.Key)
                {
                    keysForRemove.Add(path.Key);
                    newPaths.Add(newKey, path.Value);
                }
            }

            //	add the new keys
            foreach (var path in newPaths)
            {
                if (!path.Key.Contains("proxy"))
                    swaggerDoc.Paths.Add(path.Key, path.Value);
            }

            //	remove the old keys
            foreach (var key in keysForRemove)
            {
                swaggerDoc.Paths.Remove(key);
            }
        }
    }
}