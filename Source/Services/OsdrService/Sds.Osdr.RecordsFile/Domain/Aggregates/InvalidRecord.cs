using CQRSlite.Domain;
using Sds.Osdr.RecordsFile.Domain.Events.Records;
using System;

namespace Sds.Osdr.RecordsFile.Domain
{
    public class InvalidRecord : AggregateRoot
    {
        /// <summary>
        /// User Id of person who currently owns the file
        /// </summary>
        public Guid OwnedBy { get; private set; }

        /// <summary>
        /// User Id of the person who created the file
        /// </summary>
        public Guid CreatedBy { get; private set; }

        /// <summary>
        /// Date and time when file was created
        /// </summary>
        public DateTimeOffset CreatedDateTime { get; private set; }

        /// <summary>
        /// User Id of person who changed the file last time
        /// </summary>
        public Guid UpdatedBy { get; private set; }

        /// <summary>
        /// Date and time when file was changed last time
        /// </summary>
        public DateTimeOffset UpdatedDateTime { get; private set; }

        /// <summary>
        /// File Id where the record belongs to
        /// </summary>
        public Guid ParentId { get; private set; }

        /// <summary>
        /// Current record's status (Processing, Processed, Failed etc.)
        /// </summary>
        public RecordStatus Status { get; protected set; }

        /// <summary>
        /// Record's type (Substance, Reaction etc.)
        /// </summary>
        public RecordType RecordType { get; protected set; }

        /// <summary>
        /// Record's order number in file
        /// </summary>
        public long Index { get; protected set; }

        /// <summary>
        /// Error message that indicate the reason why the record is invalid
        /// </summary>
        public string Error { get; protected set; }

        /// <summary>
        /// Mark that record was deleted
        /// </summary>
        public bool IsDeleted { get; set; }

        private void Apply(InvalidRecordCreated e)
        {
            ParentId = e.FileId;
            OwnedBy = e.UserId;
            CreatedBy = e.UserId;
            CreatedDateTime = e.TimeStamp;
            UpdatedBy = e.UserId;
            UpdatedDateTime = e.TimeStamp;
            Index = e.Index;
			RecordType = e.RecordType;
            Status = RecordStatus.Failed;
			Error = e.Error;
		}

        private void Apply(RecordDeleted e)
        {
            IsDeleted = true;
            UpdatedBy = e.UserId;
            UpdatedDateTime = e.TimeStamp;
        }

        protected InvalidRecord()
        {
        }

		public InvalidRecord(Guid id, Guid userId, RecordType type, Guid fileId, long index, string error)
        {
            Id = id;
            ApplyChange(new InvalidRecordCreated(Id, userId, type, fileId, index, error));
        }

        public void Delete(Guid id, Guid userId, bool force = false)
        {
            ApplyChange(new RecordDeleted(id, RecordType, userId, force));
        }
    }
}
