using MongoDB.Bson.Serialization.Attributes;
using Sds.Domain;
using System;
using System.Collections.Generic;

namespace Sds.Osdr.Generic.Domain.ValueObjects
{
    [BsonIgnoreExtraElements]
    public class AccessPermissions : ValueObject<AccessPermissions>
    {
        public bool? IsPublic { get; set; }
        public HashSet<Guid> Users { get; set; }
        public HashSet<Guid> Groups { get; set; }

        protected override IEnumerable<object> GetAttributesToIncludeInEqualityCheck()
        {
            return new object[] { IsPublic, Users, Groups };
        }
    }
}
