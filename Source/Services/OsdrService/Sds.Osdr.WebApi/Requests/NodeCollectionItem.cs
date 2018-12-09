using System;

namespace Sds.Osdr.WebApi.Requests
{
    public class NodeCollectionItem
    {
        public Guid Id { get; set; }
        public int Version { get; set; }
        public string Type { get; set; }
		public Guid CorrelationId { get; set; }
	}
}