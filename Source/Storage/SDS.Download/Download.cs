using Sds.FileStorage;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Sds.Download
{
	public class Download : IDownload
	{
		private readonly IFileStorage storage;

		public Download(IFileStorage storage)
		{
			if (storage == null)
			{
				throw new ArgumentNullException("IFileStorage");
			}

			this.storage = storage;
		}

		private void AddFilesToArchive(ZipArchive archive, IEnumerable<IFile> files, string path = null)
		{
			if (files.Count() <= 0)
			{
				return;
			}

			foreach (var file in files)
			{
				var fileInZipPath = (path ?? "") + file.Name;
				var fileInZip = archive.CreateEntry(fileInZipPath);
				using (var entryStream = fileInZip.Open())
				{
					file.GetContent().CopyTo(entryStream);
				}
			}
		}

		private void AddFoldersToArchive(ZipArchive archive, IEnumerable<IFolder> folders, string path = null)
		{
			if (folders.Count() <= 0)
			{
				return;
			}

			foreach (var folder in folders)
			{
				var folderPath = (path ?? "") + folder.Name + "/";

				var files = this.storage.GetFiles(folder);
				this.AddFilesToArchive(archive, files, folderPath);

				var nestedFolders = this.storage.GetFolders(folder);
				this.AddFoldersToArchive(archive, nestedFolders, folderPath);
			}
		}

		public Stream DownloadArchive(IEnumerable<string> folderIds = null, IEnumerable<string> fileIds = null)
		{
			if (folderIds == null)
			{
				folderIds = new List<string>();
			}

			if (fileIds == null)
			{
				fileIds = new List<string>();
			}

			var memoryStream = new MemoryStream();
			using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
			{
				if (fileIds.Count() > 0)
				{
					var files = this.storage.GetFiles(fileIds);
					this.AddFilesToArchive(archive, files);
				}

				if (folderIds.Count() > 0)
				{
					var folders = this.storage.GetFolders(folderIds);
					this.AddFoldersToArchive(archive, folders);
				}
			}
			memoryStream.Position = 0;
			return memoryStream;
		}

		public Stream DownloadFile(string id)
		{
			return this.storage.GetFile(id).GetContent();
		}
	}
}
