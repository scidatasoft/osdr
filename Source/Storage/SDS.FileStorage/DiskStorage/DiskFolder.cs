using System;

namespace Sds.FileStorage.DiskStorage
{
	public class DiskFolder : IFolder
	{
		public DiskFolder(string path)
		{
			Path = path;
			System.IO.Path.GetDirectoryName(path);
		}

		/// <summary>
		/// Gets or sets unique folder ID
		/// </summary>
		public string Id { get { return Path; } set { Path = value; } }

		/// <summary>
		/// Gets or sets folder name
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets full folder's path including name
		/// </summary>
		public string Path { get; set; }

		/// <summary>
		/// Date and time when file was created
		/// </summary>
		public DateTime Created { get; set; }

		/// <summary>
		/// Folder deleted
		/// </summary>
		public bool Deleted { get; set; }
	}
}
