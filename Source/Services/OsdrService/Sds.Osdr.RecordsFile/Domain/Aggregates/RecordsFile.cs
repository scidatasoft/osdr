using Sds.Domain;
using Sds.Osdr.Generic.Domain;
using Sds.Osdr.RecordsFile.Domain.Events.Files;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sds.Osdr.RecordsFile.Domain
{
    public class RecordsFile : File
    {
        /// <summary>
        /// Number of total records parsed from file
        /// </summary>
        public long TotalRecords { get; private set; } = 0;

        /// <summary>
        /// List of fields extracted from file during parsing
        /// </summary>
        public IList<string> Fields { get; private set; } = new List<string>();

        public IDictionary<string, IList<string>> Properties { get; private set; } = new Dictionary<string, IList<string>>();

        private void Apply(RecordsFileCreated e)
        {
        }

        private void Apply(TotalRecordsUpdated e)
        {
            TotalRecords = e.TotalRecords;
            UpdatedBy = e.UserId;
            UpdatedDateTime = e.TimeStamp;
        }

        private void Apply(AggregatedPropertiesAdded e)
        {
            var properties = new List<string>();

            foreach(var p in e.Properties)
            {
                properties.Add(p);
            }

            if (!Properties.ContainsKey("Properties"))
            {
                Properties.Add("ChemicalProperties", properties);
            }
            UpdatedBy = e.UserId;
            UpdatedDateTime = e.TimeStamp;
        }

        private void Apply(FieldsAdded e)
        {
            foreach (var f in e.Fields)
            {
                if(!Fields.Contains(f))
                {
                    Fields.Add(f);
                }
            }
            UpdatedBy = e.UserId;
            UpdatedDateTime = e.TimeStamp;
        }

        protected RecordsFile()
        {
        }

		public RecordsFile(Guid id, Guid userId, Guid? parentId, string fileName, FileStatus fileStatus, string bucket, Guid blobId, long length, string md5)
            : base(id, userId, parentId, fileName, fileStatus, bucket, blobId, length, md5, FileType.Records)
        {
            Id = id;
			ApplyChange(new RecordsFileCreated(Id, userId));
		}

		public void UpdateTotalRecords(Guid userId, long total)
        {
            ApplyChange(new TotalRecordsUpdated(Id, userId, total));
        }

        public void AddChemicalProperties(Guid userId, IEnumerable<string> properties)
        {
            ApplyChange(new AggregatedPropertiesAdded(Id, userId, properties));
        }

        public void AddFields(Guid userId, IEnumerable<string> fields)
        {
            if (fields != null)
            {
                ApplyChange(new FieldsAdded(Id, userId, fields));
            }
        }
    }
}
