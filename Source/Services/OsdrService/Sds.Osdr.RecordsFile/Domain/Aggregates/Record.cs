using CQRSlite.Domain;
using Sds.Domain;
using Sds.Osdr.Generic.Domain.ValueObjects;
using Sds.Osdr.RecordsFile.Domain.Events.Records;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sds.Osdr.RecordsFile.Domain
{
    public enum RecordType
    {
        Structure = 0,
        Spectrum,
        Reaction,
        Crystal
    }

    public enum RecordStatus
    {
        Created = 1,
        Processing,
        Processed,
        Failed
    }

    public class Record : AggregateRoot
    {
        /// <summary>
        /// Blob storage bucket where file was loaded to
        /// </summary>
        public string Bucket { get; private set; }

        /// <summary>
        /// Blob's Id in Bucket
        /// </summary>
        public Guid BlobId { get; private set; }

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
        /// Record's type (Substance, Reaction etc.)
        /// </summary>
        public RecordType RecordType { get; protected set; }

        /// <summary>
        /// Current record's status (Processing, Processed, Failed etc.)
        /// </summary>
        public RecordStatus Status { get; protected set; }

        /// <summary>
        /// Record's order number in file
        /// </summary>
        public long Index { get; protected set; }

        /// <summary>
        /// List of properties extracted during record's parsing
        /// </summary>
        public IList<Field> Fields { get; protected set; } = new List<Field>();

        public IList<Property> Properties { get; protected set; } = new List<Property>();

        public IList<Generic.Domain.ValueObjects.Issue> Issues { get; protected set; } = new List<Generic.Domain.ValueObjects.Issue>();

        /// <summary>
        /// Mark that the record was deleted
        /// </summary>
        public bool IsDeleted { get; set; }

        /// <summary>
        /// List of images associated with record
        /// </summary>
        public IList<Image> Images { get; protected set; } = new List<Image>();

        public AccessPermissions Permissions { get; private set; }

        private void Apply(RecordCreated e)
        {
            Bucket = e.Bucket;
            BlobId = e.BlobId;
            ParentId = e.FileId;
            OwnedBy = e.UserId;
            CreatedBy = e.UserId;
            CreatedDateTime = e.TimeStamp;
            UpdatedBy = e.UserId;
            UpdatedDateTime = e.TimeStamp;
            Index = e.Index;
            Status = RecordStatus.Created;
            e.Fields?.ToList().ForEach(f => Fields.Add(f));
			RecordType = e.RecordType;
		}

        private void Apply(StatusChanged e)
        {
            Status = e.Status;
            UpdatedBy = e.UserId;
            UpdatedDateTime = e.TimeStamp;
        }

        private void Apply(PropertiesAdded e)
        {
            var newProperties = e.Properties.Select(x => x.Name.ToLower());
            var existingProperties = Properties.Where(p => newProperties.Contains(p.Name.ToLower())).Select(p => p.Name).ToList();

            foreach (var name in existingProperties)
            {
                var property = Properties.First(p => p.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase));
                Properties.Remove(property);
            }

            foreach (var property in e.Properties)
            {
                Properties.Add(property);
            }

            UpdatedBy = e.UserId;
            UpdatedDateTime = e.TimeStamp;
        }

        private void Apply(PropertyAdded e)
        {
            if (Properties.Any(p => p.Name.Equals(e.Property.Name, StringComparison.CurrentCultureIgnoreCase)))
            {
                var property = Properties.First(p => p.Name.Equals(e.Property.Name, StringComparison.CurrentCultureIgnoreCase));
                Properties.Remove(property);
            }

            Properties.Add(e.Property);

            UpdatedBy = e.UserId;
            UpdatedDateTime = e.TimeStamp;
        }

        private void Apply(IssueAdded e)
        {
            if (!Issues.Contains(e.Issue))
            {
                Issues.Add(e.Issue);
            }

            UpdatedBy = e.UserId;
            UpdatedDateTime = e.TimeStamp;
        }

        private void Apply(IssuesAdded e)
        {
            foreach (var issue in e.Issues)
            {
                if (!Issues.Contains(issue))
                {
                    Issues.Add(issue);
                }
            }

            UpdatedBy = e.UserId;
            UpdatedDateTime = e.TimeStamp;
        }

        private void Apply(FieldsUpdated e)
        {
            Fields = e.Fields.ToList();
            UpdatedBy = e.UserId;
            UpdatedDateTime = e.TimeStamp;
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

        private void Apply(RecordDeleted e)
        {
            IsDeleted = true;
            UpdatedBy = e.UserId;
            UpdatedDateTime = e.TimeStamp;
        }

        private void Apply(PermissionsChanged e)
        {
            if (e.AccessPermissions.IsPublic.HasValue)
            {
                Permissions.IsPublic = e.AccessPermissions.IsPublic;
            }

            Permissions.Users = e.AccessPermissions.Users;
            Permissions.Groups = e.AccessPermissions.Groups;

            UpdatedBy = e.UserId;
            UpdatedDateTime = e.TimeStamp;
        }

        protected Record()
        {
            Permissions = new AccessPermissions
            {
                Users = new HashSet<Guid>(),
                Groups = new HashSet<Guid>()
            };
        }

        public Record(Guid id, string bucket, Guid blobId, Guid userId, RecordType type, Guid fileId, long index, IEnumerable<Field> fields = null)
        {
            Id = id;
            ApplyChange(new RecordCreated(Id, bucket, blobId, userId, type, fileId, index, fields));
        }

        public void ChangeStatus(Guid userId, RecordStatus status)
        {
            ApplyChange(new Events.Records.StatusChanged(Id, userId, ParentId, status));
        }

        public void AddProperty(Guid userId, Property property)
        {
            ApplyChange(new PropertyAdded(Id, userId, property));
        }

        public void AddProperties(Guid userId, IEnumerable<Property> properties)
        {
            ApplyChange(new PropertiesAdded(Id, userId, properties));
        }

        public void AddIssue(Guid userId, Generic.Domain.ValueObjects.Issue issue)
        {
            ApplyChange(new IssueAdded(Id, userId, issue));
        }

        public void AddIssues(Guid userId, IEnumerable<Generic.Domain.ValueObjects.Issue> issues)
        {
            ApplyChange(new IssuesAdded(Id, userId, issues));
        }

        public void AddImage(Guid userId, Image img)
        {
            ApplyChange(new ImageAdded(Id, userId, img));
        }

        public void Delete(Guid id, Guid userId, bool force = false)
        {
            ApplyChange(new RecordDeleted(id, RecordType, userId, force));
        }

        public void UpdateFields(Guid userId, IList<Field> fields)
        {
            ApplyChange(new FieldsUpdated(Id, userId, fields));
        }

        public void SetAccessPermissions(Guid userId, AccessPermissions accessPermissions)
        {
            ApplyChange(new PermissionsChanged(Id, userId, accessPermissions));
        }
    }
}
