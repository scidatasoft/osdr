using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sds.FileStorage.EntityFramework
{
	/// <summary>
	/// A file saved on a physical disk
	/// </summary>
	public class EFFile : IFile
	{
		public EFFile()
		{
		}

		public EFFile(int id)
		{
			Id = id.ToString();
		}

		/// <summary>
		/// Gets or sets unique file ID
		/// </summary>
		public string Id { get; set; }

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
		public string Path { get; set; }

		/// <summary>
		/// Gets the folder that contains the file
		/// </summary>
		public string Folder { get; set; }

		/// <summary>
		/// Date and time when file was created
		/// </summary>
		public DateTime Created { get; set; }

		/// <summary>
		/// Returns Stream with the file's data
		/// </summary>
		/// <returns></returns>
		public Stream GetContent()
		{
			using (var db = new FileStorageContext()) // FileStorageContext.GlobalConfig.ConnectionString: this needs to be redesigned
			{
				byte[] data = db.Files.Where(f => f.FileId.ToString() == Id).Select(f => f.Blob.Data).FirstOrDefault();

				return new MemoryStream(data);
			}
		}
	}
}
