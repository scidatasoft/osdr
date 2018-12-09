using Sds.Domain;
using Sds.Osdr.Chemicals.Domain.Events;
using Sds.Osdr.RecordsFile.Domain;
using System;
using System.Collections.Generic;

namespace Sds.Osdr.Chemicals.Domain.Aggregates
{
    public class Substance : Record
    {
        public Guid StandardizedBlobId { get; protected set; }

        private void Apply(SubstanceCreated e)
        {
        }

        //private void Apply(StandardizedBlobIdChanged e)
        //{
        //    StandardizedBlobId = e.BlobId;
        //}

        protected Substance()
        {
        }

        public Substance(Guid id, string bucket, Guid blobId, Guid userId, Guid fileId, long index, IEnumerable<Field> fields = null)
            : base(id, bucket, blobId, userId, RecordType.Structure, fileId, index, fields)
        {
            ApplyChange(new SubstanceCreated(Id, userId));
        }

        //public void SetStandardizedBlobId(Guid userId, Guid blobId)
        //{
        //    ApplyChange(new StandardizedBlobIdChanged(Id, userId, blobId));
        //}
    }
}
