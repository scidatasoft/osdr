using CQRSlite.Domain;
using Sds.Osdr.Generic.Domain.Events.Files;
using Sds.Osdr.Generic.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.IO;

namespace Sds.Osdr.Generic.Domain
{
    public enum FileType
    {
        Generic = 1,
        Office,
        Model,
        Records,
        Tabular,
        Pdf,
        WebPage,
        Image
    }

    public enum FileStatus
    {
        Loading = 1,
        Loaded,
		Parsing,
        Parsed,
        Processing,
        Processed,
        ProcessedPartially,
        Failed
    }

    public class File : AggregateRoot
    {
        /// <summary>
        /// Blob storage bucket where file was loaded to
        /// </summary>
        public string Bucket { get; set; } 

        /// <summary>
        /// Blob's Id in Bucket
        /// </summary>
        public Guid BlobId { get; set; }

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

        /// <summary>
        /// Folder Id where the file belongs to. Root if null.
        /// </summary>
        //TODO: is it still possible that parentId is nul???
        public Guid? ParentId { get; private set; }

        /// <summary>
        /// File name
        /// </summary>
        public string FileName { get; protected set; }

        /// <summary>
        /// Length on the file in bytes
        /// </summary>
        public long Length { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        public string Md5 { get; protected set; }

        /// <summary>
        /// Marker that the file was deleted.
        /// </summary>
        public bool IsDeleted { get; private set; } 

        /// <summary>
        /// File type (Office, Pdf, Model etc.)
        /// </summary>
        public FileType Type { get; private set; }

        public IList<Image> Images { get; protected set; } = new List<Image>();

        /// <summary>
        /// Current file status
        /// </summary>
        public FileStatus Status { get; protected set; }

        public string Extension { get { return Path.GetExtension(FileName); } }

        public AccessPermissions Permissions { get; private set; }

        private void Apply(FileCreated e)
        {
            Bucket = e.Bucket;
            BlobId = e.BlobId;
            OwnedBy = e.UserId;
            CreatedBy = e.UserId;
            CreatedDateTime = e.TimeStamp;
            UpdatedBy = e.UserId;
            UpdatedDateTime = e.TimeStamp;
            ParentId = e.ParentId;
            FileName = e.FileName;
            Length = e.Length;
			Status = e.FileStatus;
            Md5 = e.Md5;
            Type = e.FileType;
        }

        private void Apply(StatusChanged e)
        {
            Status = e.Status;
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

        private void Apply(FileNameChanged e)
        {
            FileName = e.NewName;
            UpdatedBy = e.UserId;
            UpdatedDateTime = e.TimeStamp;
        }

        private void Apply(FileMoved e)
        {
            ParentId = e.NewParentId;
            UpdatedBy = e.UserId;
            UpdatedDateTime = e.TimeStamp;
        }

        private void Apply(FileDeleted e)
        {
            IsDeleted = true;
            UpdatedBy = e.UserId;
            UpdatedDateTime = e.TimeStamp;
        }

        private void Apply(BlobUpdated e)
        {
            Bucket = e.Bucket;
            BlobId = e.BlobId;
            FileName = e.FileName;
            Length = e.Length;
            Md5 = e.Md5;
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

        protected File()
        {
            Permissions = new AccessPermissions
            {
                Users = new HashSet<Guid>(),
                Groups = new HashSet<Guid>()
            };
        }

        public string GetExtension()
        {
            return Path.GetExtension(FileName);
        }

		public File(Guid id, Guid userId, Guid? parentId, string fileName, FileStatus fileStatus, string bucket, Guid blobId, long length, string md5, FileType type)
        {
            Id = id;
			ApplyChange(new FileCreated(Id, bucket, fileStatus, blobId, userId, parentId, fileName, length, md5, type));
		}

        public void ChangeStatus(Guid userId, FileStatus status)
        {
            ApplyChange(new StatusChanged(Id, userId, status));
        }

		public void UpdateBlob(Guid userId, string fileName, string bucket, Guid blobId, long length, string md5)
		{
			ApplyChange(new BlobUpdated(Id, userId, fileName, bucket, blobId, length, md5));
		}

        public void AddImage(Guid userId, Image img)
        {
            ApplyChange(new ImageAdded(Id, userId, img));
        }

        public void Rename(Guid userId, string newName)
        {
            ApplyChange(new FileNameChanged(Id, userId, FileName, newName));
        }

        public void ChangeParent(Guid userId, Guid? newParentId)
        {
            ApplyChange(new FileMoved(Id, userId, ParentId, newParentId));
        }

        public void Delete(Guid userId, bool force = false)
        {
            ApplyChange(new FileDeleted(Id, Type, userId, force));
        }

        public void GrantAccess(Guid userId, AccessPermissions accessPermissions)
        {
            ApplyChange(new PermissionsChanged(Id, userId, accessPermissions));
        }
    }
}
