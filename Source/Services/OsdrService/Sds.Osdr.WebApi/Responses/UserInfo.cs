using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sds.Osdr.WebApi.Responses
{
    [BsonIgnoreExtraElements]
    public class UserInfo
    {
        public Guid Id { get; set; }
        public string DisplayName { get; set; }
    }
}
