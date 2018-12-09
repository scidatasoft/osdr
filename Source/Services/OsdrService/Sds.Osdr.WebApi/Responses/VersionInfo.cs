using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sds.Osdr.WebApi.Responses
{
    public class VersionInfo
    {
        string Version { get; set; }
        string CommitId { get; set; }
        string BuildTimeStamp { get; set; }
        string CommitAuthor { get; set; }
        //AssemblyCommitId, AssemblyCommitAuthor, AssemblyBuildTimespamp, AssemblyBuildId
    }
}
