using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Sds.Osdr.WebApi.Models
{
    [BsonIgnoreExtraElements]
    public class User
    {
        public Guid Id { get; set; }
        public string DisplayName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string LoginName { get; set; }
        public string Email { get; set; }
        public string Avatar { get; set; }
        public int Version { get; set; }
    }
}
