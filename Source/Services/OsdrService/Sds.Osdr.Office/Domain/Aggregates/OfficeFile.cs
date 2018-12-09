using Sds.Domain;
using Sds.Osdr.Generic.Domain;
using Sds.Osdr.Generic.Domain.Events.Files;
using Sds.Osdr.Office.Domain.Events;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sds.Osdr.Office.Domain
{
    public class OfficeFile : File
    {
        /// <summary>
        /// Blob storage bucket where converted PDF file was loaded to
        /// </summary>
        public string PdfBucket { get; private set; }

        /// <summary>
        /// PDF Blob Id in Bucket
        /// </summary>
        public Guid PdfBlobId { get; private set; }

        public IList<Property> Metadata { get; private set; } = new List<Property>();

        private void Apply(OfficeFileCreated e)
        {
        }

        private void Apply(ImageAdded e)
        {
            if (!Images.Contains(e.Image))
            {
                Images.Add(e.Image);
            }

            UpdatedBy = e.UserId;
            UpdatedDateTime = e.TimeStamp;
        }

        private void Apply(PdfBlobUpdated e)
        {
            PdfBucket = e.Bucket;
            PdfBlobId = e.BlobId;
            UpdatedBy = e.UserId;
            UpdatedDateTime = e.TimeStamp;
        }

        private void Apply(MetadataAdded e)
        {
            e.Metadata.ToList().ForEach(x => Metadata.Add(x));

            UpdatedBy = e.UserId;
            UpdatedDateTime = e.TimeStamp;
        }

        protected OfficeFile()
        {
        }

		public OfficeFile(Guid id, Guid userId, Guid? parentId, string fileName, FileStatus fileStatus, string bucket, Guid blobId, long length, string md5)
            : base(id, userId, parentId, fileName, fileStatus, bucket, blobId, length, md5, FileType.Office)
        {
            Id = id;
			ApplyChange(new OfficeFileCreated(Id));
		}

		public void UpdatePdf(Guid userId, string bucket, Guid blobId)
        {
            ApplyChange(new PdfBlobUpdated(Id, userId, bucket, blobId));
        }

        public void AddMetadata(Guid userId, IEnumerable<Property> metadata)
        {
            ApplyChange(new MetadataAdded(Id, userId, metadata));
        }
    }
}
