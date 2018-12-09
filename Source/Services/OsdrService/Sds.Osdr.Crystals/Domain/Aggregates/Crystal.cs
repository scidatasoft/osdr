using Sds.Domain;
using Sds.Osdr.Crystals.Domain.Events;
using Sds.Osdr.RecordsFile.Domain;
using System;
using System.Collections.Generic;

namespace Sds.Osdr.Crystals.Domain
{
    public class Crystal : Record
    {
        private void Apply(CrystalCreated e)
        {
        }

        protected Crystal()
        {
        }

        public Crystal(Guid id, string bucket, Guid blobId, Guid userId, Guid fileId, long index, IEnumerable<Field> fields = null)
            : base(id, bucket, blobId, userId, RecordType.Crystal, fileId, index, fields)
        {
            ApplyChange(new CrystalCreated(Id, userId));
        }
    }
}
