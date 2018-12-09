using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace Sds.Osdr.WebApi.Models
{
    [BsonIgnoreExtraElements]
    public class BaseNode
    {
        public Guid Id { get; set; }
        public Guid ParentId { get; set; }
        public string Type { get; set; }
        public Guid OwnedBy { get; set; }
        public string Name { get; set; }
    }
}
