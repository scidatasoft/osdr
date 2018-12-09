using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Runtime.Serialization;

namespace Sds.FileStorage.EntityFramework
{
	[Table("Folders")]
	public class ef_DBFolder
	{
		[DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity), Key()]
		public int Id { get; set; }

		[Required]
		public string Name { get; set; }

		[ForeignKey("Parent")]
		public int? ParentId { get; set; }
		public virtual ef_DBFolder Parent { get; set; }

		public bool Deleted { get; set; }

		[Required]
		public DateTime Created { get; set; }

		public string GetFullPath()
		{
			return this.GetFolderFullPath(this);
		}

		private string GetFolderFullPath(ef_DBFolder folder)
		{
			var parentFolder = folder.Parent;
			if (parentFolder != null)
			{
				var parentPath = GetFolderFullPath(parentFolder);
				return Path.Combine(parentPath, folder.Name);
			}
			return folder.Name;
		}
	}
}
