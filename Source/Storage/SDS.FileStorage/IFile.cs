using System;
using System.IO;

namespace Sds.FileStorage
{
	/// <summary>
	/// File Interface
	/// </summary>
	public interface IFile
	{
		/// <summary>
		/// Gets or sets unique file ID
		/// </summary>
		string Id { get; set; }

		/// <summary>
		/// Gets or sets file name
		/// </summary>
		string Name { get; set; }

		/// <summary>
		/// Gets file extention
		/// </summary>
		string Extension { get; }

		/// <summary>
		/// Gets full file's path including file name
		/// </summary>
		string Path { get; }

		/// <summary>
		/// Gets the folder that contains the file
		/// </summary>
		string Folder { get; }

		/// <summary>
		/// Date and time when file was created
		/// </summary>
		DateTime Created { get; set; }

		/// <summary>
		/// Returns Stream with the file's data
		/// </summary>
		/// <returns></returns>
		Stream GetContent();
	}
}