using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sds.Imaging.Domain.Models
{
    public class FileImages
    {
        public Guid Id { get; set; }
        public string Bucket { get; set; }
        public IList<Image> Images { get; set; }
    }
}
