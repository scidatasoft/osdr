using Sds.Domain;
using Sds.Osdr.RecordsFile.Domain;
using Sds.Osdr.Spectra.Domain.Events;
using System;
using System.Collections.Generic;

namespace Sds.Osdr.Spectra.Domain
{
    public class Spectrum : Record
    {
        private void Apply(SpectrumCreated e)
        {
        }

        protected Spectrum()
        {
        }

        public Spectrum(Guid id, string bucket, Guid blobId, Guid userId, Guid fileId, long index, IEnumerable<Field> fields = null)
            : base(id, bucket, blobId, userId, RecordType.Spectrum, fileId, index, fields)
        {
            ApplyChange(new SpectrumCreated(Id, userId));
        }
    }
}
