using System;

namespace Sds.Osdr.WebApi.Requests
{
    public class CreateFolderRequest
    {
        public string Name { get; set; }
        public Guid? ParentId { get; set; }
    }
}
