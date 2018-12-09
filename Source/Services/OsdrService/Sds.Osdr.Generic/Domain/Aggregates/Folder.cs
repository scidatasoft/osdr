using CQRSlite.Domain;
using Sds.Osdr.Generic.Domain.Events.Folders;
using Sds.Osdr.Generic.Domain.ValueObjects;
using System;
using System.Collections.Generic;

namespace Sds.Osdr.Generic.Domain
{
    public enum FolderStatus
	{
		Created = 1,
		Processing,
		Processed,
        Failed
	}

	public class Folder : AggregateRoot
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
        public Guid UpdatedBy { get; protected set; }

        /// <summary>
        /// Date and time when file was changed last time
        /// </summary>
        public DateTimeOffset UpdatedDateTime { get; protected set; }
        public Guid? ParentId { get; private set; }
        public string Name { get; private set; }
        public bool IsDeleted { get; private set; }

		/// <summary>
		/// Current folder status
		/// </summary>
		public FolderStatus Status { get; private set; } = FolderStatus.Created;

        public AccessPermissions Permissions { get; private set; }

        private void Apply(FolderCreated e)
        {
            ParentId = e.ParentId;
            OwnedBy = e.UserId;
            CreatedBy = e.UserId;
            CreatedDateTime = e.TimeStamp;
            UpdatedBy = e.UserId;
            UpdatedDateTime = e.TimeStamp;
            Name = e.Name;
			Status = e.Status;
		}

		private void Apply(FolderNameChanged e)
        {
            Name = e.NewName;
            UpdatedBy = e.UserId;
            UpdatedDateTime = e.TimeStamp;
        }

        private void Apply(FolderMoved e)
        {
            ParentId = e.NewParentId;
            UpdatedBy = e.UserId;
            UpdatedDateTime = e.TimeStamp;
        }

        private void Apply(FolderDeleted e)
        {
            IsDeleted = true;
            UpdatedBy = e.UserId;
            UpdatedDateTime = e.TimeStamp;
        }

        private void Apply(StatusChanged e)
		{
			Status = e.Status;
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

        protected Folder()
        {
            Permissions = new AccessPermissions
            {
                Users = new HashSet<Guid>(),
                Groups = new HashSet<Guid>()
            };
        }

        public Folder(Guid id, Guid correlationId, Guid userId, Guid? parentId, string name, Guid sessionId, FolderStatus status = FolderStatus.Created)
        {
            Id = id;
            ApplyChange(new FolderCreated(Id, correlationId, userId, parentId, name, sessionId, status));
        }

        public void Rename(Guid userId, string newName)
        {
            ApplyChange(new FolderNameChanged(Id, userId, Name, newName));
        }

        public void ChangeParent(Guid userId, Guid? newParentId, Guid sessionId)
        {
            ApplyChange(new FolderMoved(Id, userId, ParentId, newParentId, sessionId));
        }

        public void Delete(Guid userId, Guid correlationId, bool force = false)
        {
            ApplyChange(new FolderDeleted(Id, correlationId, userId, force));
        }

		public void ChangeStatus(Guid userId, FolderStatus status)
		{
			ApplyChange(new StatusChanged(Id, userId, status));
		}

        public void GrantAccess(Guid userId, AccessPermissions accessPermissions)
        {
            ApplyChange(new PermissionsChanged(Id, userId, accessPermissions));
        }
    }
}
