using Microsoft.AspNetCore.JsonPatch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sds.Osdr.WebApi.Requests
{
    public class PatchEntityRequest
    {
        public JsonPatchDocument<UpdatedEntityData> PatchDocument { get; set; }
        public int Version { get; set; }
    }
}
