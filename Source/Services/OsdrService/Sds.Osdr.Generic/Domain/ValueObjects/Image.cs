using Sds.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sds.Osdr.Generic.Domain.ValueObjects
{
    public class Image : ValueObject<Image>
    {
        public Guid Id { get; private set; }
        public string Format { get; private set; }
        public string MimeType { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public string Exception { get; private set; }
        public string Bucket { get; private set; }
        public Image(string bucket, Guid id, string format, string mimetype, int width, int height, string exception)
        {
            Id = id;
            Format = format;
            MimeType = mimetype;
            Width = width;
            Height = height;
            Exception = exception;
            Bucket = bucket;
        }

        protected override IEnumerable<object> GetAttributesToIncludeInEqualityCheck()
        {
            return new List<Object>() { Id };
        }
    }
}
