using MongoDB.Bson.Serialization.Attributes;
using Sds.Osdr.WebApi.Models;
using System;
using System.Collections.Generic;

namespace Sds.Osdr.WebApi.Responses
{

    [BsonIgnoreExtraElements]
    public class FolderContentItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int Version { get; set; }
        public string Type { get; set; } = "Folder";
        public Guid OwnedBy { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public string OwnerName { get; set; }
        public IEnumerable<Image> Images { get; set; }
    }
}
