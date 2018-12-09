using Sds.Osdr.Generic.Domain;
using Sds.Osdr.Images.Domain.Events;
using System;

namespace Sds.Osdr.Images.Domain
{
    public class ImageFile : File
    {
        private void Apply(ImageFileCreated e)
        {
        }

        protected ImageFile()
        {
        }

		public ImageFile(Guid id, Guid userId, Guid? parentId, string fileName, FileStatus fileStatus, string bucket, Guid blobId, long length, string md5)
            : base(id, userId, parentId, fileName, fileStatus, bucket, blobId, length, md5, FileType.Image)
        {
            Id = id;
			ApplyChange(new ImageFileCreated(Id));
		}
    }
}
