using MongoDB.Bson.Serialization.Attributes;
using Sds.Osdr.WebApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sds.Osdr.WebApi.Responses
{
    [BsonIgnoreExtraElements]
    public class FileResponse
    {
        public Guid Id { get; set; }
        public Guid? ParentId { get; set; }
        public Blob Blob { get; set; }
        public string FileName { get; set; }
        public int Version { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public string OwnerName { get; set; }
        public IEnumerable<Image> Images { get; set; }
    }
}
