using System;
using System.IO;

namespace Sds.FileStorage.DiskStorage
{
	/// <summary>
	/// A file saved on a physical disk
	/// </summary>
	public class DiskFile : IFile
	{
		public DiskFile(string path)
		{
			Path = path;
			Name = System.IO.Path.GetFileName(path);
			Folder = System.IO.Path.GetDirectoryName(path);
		}

		/// <summary>
		/// Gets or sets unique file ID
		/// </summary>
		public string Id { get { return Path; } set { Path = value; } }

		/// <summary>
		/// Gets full file's path including file name
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets file extention
		/// </summary>
		public string Extension { get { return System.IO.Path.GetExtension(Name); } }

		/// <summary>
		/// Gets full file's path including file name
		/// </summary>
		public string Path { get; private set; }

		/// <summary>
		/// Gets the folder that contains the file
		/// </summary>
		public string Folder { get; private set; }

		/// <summary>
		/// Date and time when file was created
		/// </summary>
		public DateTime Created { get; set; }


		/// <summary>
		/// File deleted
		/// </summary>
		public bool Deleted { get; set; }

		/// <summary>
		/// Returns Stream with the file's data
		/// </summary>
		/// <returns></returns>
		public Stream GetContent()
		{
			return File.OpenRead(Path);
		}
	}
}
