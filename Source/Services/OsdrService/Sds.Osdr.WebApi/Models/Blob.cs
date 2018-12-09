using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Sds.Osdr.WebApi.Models
{
    [BsonIgnoreExtraElements]
    public class Blob
    {
        public Guid Id { get; set; }
        public string Bucket { get; set; }
    }
}
