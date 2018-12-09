using System;
using System.ComponentModel.DataAnnotations;

namespace Sds.Osdr.WebApi.Requests
{
    public class ImportWebPageRequest
    {
        [Required]
        public string Uri { get; set; }
        public Guid ParentId { get; set; }
        public string Bucket { get; set; }
    }
}
