using Sds.Osdr.Generic.Domain;
using Sds.Osdr.Tabular.Domain.Events;
using System;

namespace Sds.Osdr.Tabular.Domain
{
    public class TabularFile : File
    {
        private void Apply(TabularFileCreated e)
        {
        }

        protected TabularFile()
        {
        }

		public TabularFile(Guid id, Guid userId, Guid? parentId, string fileName, FileStatus fileStatus, string bucket, Guid blobId, long length, string md5)
            : base(id, userId, parentId, fileName, fileStatus, bucket, blobId, length, md5, FileType.Tabular)
        {
            Id = id;
			ApplyChange(new TabularFileCreated(Id));
		}
    }
}
