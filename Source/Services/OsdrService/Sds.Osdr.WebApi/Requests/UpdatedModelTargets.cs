using System;
using System.Collections.Generic;

namespace Sds.Osdr.WebApi.Requests
{
    public class UpdatedModelTargets
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public double ConsensusWeight { get; set; }
        public IEnumerable<string> Targets { get; set; }
    }
}
