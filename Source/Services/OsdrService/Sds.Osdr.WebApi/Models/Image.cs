using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sds.Osdr.WebApi.Models
{
    [BsonIgnoreExtraElements]
    public class Image
    {
        public Guid Id { get; private set; }
        public string Scale { get; private set; } 
        public string MimeType { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
    }
}
