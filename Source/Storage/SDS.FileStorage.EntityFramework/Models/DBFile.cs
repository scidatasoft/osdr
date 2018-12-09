using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace Sds.FileStorage.EntityFramework
{
    [Table("Files")]
    public class ef_DBFile
    {
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity), Key()]
        public int Id { get; set; }

        [Index(IsUnique = true)]
        [Required]
        [MaxLength(50)]
        public string FileId { get; set; }

        public string Name { get; set; }

        public string Extension { get; set; }

        [Required]
        public DateTime Created { get; set; }

		public bool Deleted { get; set; }

        [ForeignKey("Folder")]
        public int? FolderId { get; set; }
        public virtual ef_DBFolder Folder { get; set; }

        [Required]
        [ForeignKey("Blob")]
        public int BlobId { get; set; }
        public virtual ef_Blob Blob { get; set; }

		public string GetFullPath()
		{
			var fullFolderPath = GetFolderFullPath(this.Folder);
			var fullPath = fullFolderPath + "/" + this.Name;
			return fullPath;
		}

		private string GetFolderFullPath(ef_DBFolder folder)
		{
			var parentFolder = folder.Parent;
			if (parentFolder != null)
			{
				var parentPath = GetFolderFullPath(parentFolder);
				return parentPath += "/" + folder.Name;
			}
			return folder.Name;
		} 
	}
}
