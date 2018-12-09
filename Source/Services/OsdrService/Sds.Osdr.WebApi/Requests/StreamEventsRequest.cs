using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sds.Osdr.WebApi.Requests
{
    public class ItemsSequenceRequest
    {
        public int Start { get; set; } = 0;
        public int Count { get; set; } = -1;
    }
}
