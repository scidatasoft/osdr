using Sds.Domain;
using Sds.Osdr.Reactions.Domain.Events;
using Sds.Osdr.RecordsFile.Domain;
using System;
using System.Collections.Generic;

namespace Sds.Osdr.Reactions.Domain
{
    public class Reaction : Record
    {
        private void Apply(ReactionCreated e)
        {
        }

        protected Reaction()
        {
        }

        public Reaction(Guid id, string bucket, Guid blobId, Guid userId, Guid fileId, long index, IEnumerable<Field> fields = null)
            : base(id, bucket, blobId, userId, RecordType.Reaction, fileId, index, fields)
        {
            ApplyChange(new ReactionCreated(Id, userId));
        }
    }
}
