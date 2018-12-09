using MongoDB.Bson.Serialization.Attributes;
using Sds.Osdr.WebApi.Models;
using System;
using System.Collections.Generic;

namespace Sds.Osdr.WebApi.Responses
{
    [BsonIgnoreExtraElements]
    public class Node
    {
        public Guid Id { get; set; }
        public Guid ParentId { get; set; }
        public string Name { get; set; }
        public int Version { get; set; }
        public dynamic Status { get; set; }
        public dynamic SubType { get; set; }
        public string Type { get; set; } 
        public DateTime CreatedDateTime { get; set; }
        public DateTime? UpdatedDateTime { get; set; }
        public Blob Blob { get; set; }
        public UserInfo OwnedBy { get; set; }
        public UserInfo CreatedBy { get; set; }
        public UserInfo UpdatedBy { get; set; }
        public IEnumerable<Image> Images { get; set; }
        public int? TotalRecords { get; set; }
}
}
