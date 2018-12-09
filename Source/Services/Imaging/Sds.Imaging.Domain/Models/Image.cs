using System;

namespace Sds.Imaging.Domain.Models
{
    public class Image
    {
        public Guid Id { get;  set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string Format { get; set; }
        public string MimeType { get; set; }
        public string Exception { get; set; }
    }
}
