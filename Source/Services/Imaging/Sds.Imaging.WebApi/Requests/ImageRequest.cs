using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sds.Imaging.WebApi.Requests
{
    public class ImageRequest
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public string Format { get; set; }
    }
}
