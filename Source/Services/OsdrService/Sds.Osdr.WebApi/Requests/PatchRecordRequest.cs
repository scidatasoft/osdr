using Microsoft.AspNetCore.JsonPatch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sds.Osdr.WebApi.Requests
{
    public class PatchRecordRequest
    {
        public JsonPatchDocument<dynamic> PatchDocument { get; set; }
        public int Version { get; set; }
    }
}
