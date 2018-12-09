using Sds.Osdr.Generic.Domain;
using Sds.Osdr.Pdf.Domain.Events;
using System;

namespace Sds.Osdr.Pdf.Domain
{
    public class PdfFile : File
    {
        private void Apply(PdfFileCreated e)
        {
        }

        protected PdfFile()
        {
        }

		public PdfFile(Guid id, Guid userId, Guid? parentId, string fileName, FileStatus fileStatus, string bucket, Guid blobId, long length, string md5)
            : base(id, userId, parentId, fileName, fileStatus, bucket, blobId, length, md5, FileType.Pdf)
        {
            Id = id;
			ApplyChange(new PdfFileCreated(Id));
		}
    }
}
