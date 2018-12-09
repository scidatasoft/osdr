using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sds.Osdr.WebApi.Requests
{
    public class UpdateNodeCollectionRequest
    {
        public Guid? ParentId { get; set; }
        public IEnumerable<NodeCollectionItem> Deleted { get; set; } = new NodeCollectionItem[] { };
        public IEnumerable<NodeCollectionItem> Moved { get; set; } = new NodeCollectionItem[] { };
        public IEnumerable<NodeCollectionItem> ForceDeleted { get; set; } = new NodeCollectionItem[] { };
    }
}
