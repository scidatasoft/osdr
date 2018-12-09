using MongoDB.Bson.Serialization.Attributes;
using Sds.Osdr.Generic.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sds.Osdr.WebApi.Requests
{
    public class UpdatedEntityData
    {
        public Guid Id { get; set; }
        public UpdatedEntityData() => Permissions = new AccessPermissions
        {
            Users = new HashSet<Guid>(),
            Groups = new HashSet<Guid>()
        };

        public Guid? ParentId { get; set; } = Guid.Empty;
        public string Name { get; set; } = null;
        public AccessPermissions Permissions { get; set; }
    }
}
