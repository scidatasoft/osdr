using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Sds.Osdr.WebApi.Responses
{
    [BsonIgnoreExtraElements]
    public class UserPublicInfo
    {
        public Guid Id { get; set; }
        public string DisplayName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Avatar { get; set; }
    }
}
