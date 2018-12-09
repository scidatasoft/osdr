using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;

namespace Sds.FileStorage.EntityFramework
{
	public class EFStorage : IFileStorage
	{
		#region Events
		event AddFileEventHandler AddFileEvent;

		event DeleteFilesEventHandler DeleteFilesEvent;

		event CreateFolderEventHandler CreateFolderEvent;

		event DeleteFoldersEventHandler DeleteFoldersEvent;

		object objectLock = new Object();

		event AddFileEventHandler IFileStorage.OnFileAdd
		{
			add
			{
				lock (objectLock)
				{
					AddFileEvent += value;
				}
			}
			remove
			{
				lock (objectLock)
				{
					AddFileEvent -= value;
				}
			}
		}

		event DeleteFilesEventHandler IFileStorage.OnFilesDelete
		{
			add
			{
				lock (objectLock)
				{
					DeleteFilesEvent += value;
				}
			}
			remove
			{
				lock (objectLock)
				{
					DeleteFilesEvent -= value;
				}
			}
		}

		event CreateFolderEventHandler IFileStorage.OnFolderCreate
		{
			add
			{
				lock (objectLock)
				{
					CreateFolderEvent += value;
				}
			}
			remove
			{
				lock (objectLock)
				{
					CreateFolderEvent -= value;
				}
			}
		}

		event DeleteFoldersEventHandler IFileStorage.OnFoldersDelete
		{
			add
			{
				lock (objectLock)
				{
					DeleteFoldersEvent += value;
				}
			}
			remove
			{
				lock (objectLock)
				{
					DeleteFoldersEvent -= value;
				}
			}
		}
		#endregion

		private FileStorageContext GetReadContext()
		{
			var db = new FileStorageContext();

			db.Configuration.AutoDetectChangesEnabled = false;

			return db;
		}

		private FileStorageContext GetUpdateContext()
		{
			var db = new FileStorageContext();

			return db;
		}

		/// <summary>
		/// Saves a byte array inside a folder under supplied file name
		/// </summary>
		/// <param name="folder">Folder path to save the file into</param>
		/// <param name="fileName">File name</param>
		/// <param name="data">File's content</param>
		/// <returns>information about the saved file</returns>
		public IFile AddFile(string folder, string fileName, byte[] data, IDictionary<string, string> meta = null)
		{
			var iFolder = FindFolder(folder);

			if (iFolder == null)
			{
				iFolder = CreateFolder(folder);
			}

			using (var db = GetUpdateContext())
			{
				var extension = Path.GetExtension(fileName);

				var file = new ef_DBFile()
				{
                    FileId = Guid.NewGuid().ToString(),
                    FolderId = Convert.ToInt32(iFolder.Id),
					Name = fileName,
					Extension = extension,
					Created = DateTime.Now,
					Blob = new ef_Blob()
					{
						Data = data
					}
				};

				db.Files.Add(file);

				db.SaveChanges();

				var newFile = new EFFile()
				{
					Id = file.Id.ToString(),
					Name = fileName,
					Created = file.Created,
					Folder = folder,
					Path = Path.Combine(folder, fileName)
				};

				AddFileEventHandler handler = AddFileEvent;
				if (handler != null)
				{
					handler(this, new AddFileEventArgs() { File = newFile, Meta = meta });
				}

				return newFile;
			}
		}

        /// <summary>
        /// Saves a data from stream inside a folder under supplied file name
        /// </summary>
        /// <param name="id">File Id</param>
		/// <param name="path">Folder path to save the file into</param>
		/// <param name="fileName">File name</param>
        /// <param name="data">File's content</param>
        /// <param name="meta">Metadata assigned to the file</param>
        /// <returns>information about the saved file</returns>
        public IFile AddFile(string id, string folder, string fileName, Stream data, IDictionary<string, string> meta = null)
        {
            var iFolder = FindFolder(folder);

            if (iFolder == null)
            {
                iFolder = CreateFolder(folder);
            }

            using (var db = GetUpdateContext())
            {
                var fi = new FileInfo(fileName);

                var rawData = new byte[data.Length];
                data.Position = 0;
                data.Read(rawData, 0, (int)data.Length);

                var file = new ef_DBFile()
                {
                    FileId = id,
                    FolderId = Convert.ToInt32(iFolder.Id),
                    Name = fileName,
                    Extension = fi.Extension + "",
                    Created = DateTime.Now,
                    Blob = new ef_Blob()
                    {
                        Data = rawData
                    }
                };

                db.Files.Add(file);

                db.SaveChanges();

                var newFile = new EFFile()
                {
                    Id = file.Id.ToString(),
                    Created = file.Created,
                };

                AddFileEventHandler handler = AddFileEvent;
                if (handler != null)
                {
                    handler(this, new AddFileEventArgs() { File = newFile, Meta = meta });
                }

                return newFile;
            }
        }

        /// <summary>
        /// Saves a data from stream inside a folder under supplied file name
        /// </summary>
        /// <param name="folder">Folder path to save the file into</param>
        /// <param name="fileName">File name</param>
        /// <param name="data">File's content</param>
        /// <param name="meta">Metadata assigned to the file</param>
        /// <returns>information about the saved file</returns>
        public IFile AddFile(string folder, string fileName, Stream data, IDictionary<string, string> meta = null)
		{
			var iFolder = FindFolder(folder);

			if (iFolder == null)
			{
				iFolder = CreateFolder(folder);
			}

			using (var db = GetUpdateContext())
			{
				var fi = new FileInfo(fileName);

				var rawData = new byte[data.Length];
				data.Position = 0;
				data.Read(rawData, 0, (int)data.Length);

				var file = new ef_DBFile()
				{
                    FileId = Guid.NewGuid().ToString(),
                    FolderId = Convert.ToInt32(iFolder.Id),
					Name = fileName,
					Extension = fi.Extension + "",
					Created = DateTime.Now,
					Blob = new ef_Blob()
					{
						Data = rawData
					}
				};

				db.Files.Add(file);

				db.SaveChanges();

				var newFile = new EFFile()
				{
					Id = file.Id.ToString(),
					Name = fileName,
					Created = file.Created,
					Folder = folder,
					Path = Path.Combine(folder, fileName)
				};

				AddFileEventHandler handler = AddFileEvent;
				if (handler != null)
				{
					handler(this, new AddFileEventArgs() { File = newFile, Meta = meta });
				}

				return newFile;
			}
		}

		/// <summary>
		/// Returns list of file Ids inside a specified folder
		/// </summary>
		/// <param name="folder">Folder</param>
		/// <param name="start">Ordinal start number of compounds</param>
		/// <param name="count">Counts of compounds to return</param>
		/// <returns>List of file Ids inside a specified folder</returns>
		public IEnumerable<string> GetFileIds(string folder, int start = 0, int count = -1)
		{
			//	Empty request...
			if (count == 0)
				return new List<string>();

			var iFolder = FindFolder(folder);

			if (iFolder == null)
				return new List<string>();

			using (var db = GetReadContext())
			{
				IQueryable<ef_DBFile> query = db.Files
					.Where(f => !f.Deleted && f.FolderId.ToString() == iFolder.Id)
					.OrderBy(f => f.Name);

				if (start > 0)
					query = query.Skip(start);

				if (count > 0)
					query = query.Take(count);

				return query.Select(f => f.FileId).ToList();
			}
		}

		/// <summary>
		/// Returns list of files inside a specified folder
		/// </summary>
		/// <param name="folder">Folder</param>
		/// <param name="start">Ordinal start number of compounds</param>
		/// <param name="count">Counts of compounds to return</param>
		/// <returns>list of files inside a specified folder</returns>
		public IEnumerable<IFile> GetFiles(string folder, int start = 0, int count = -1)
		{
			//	Empty request...
			if (count == 0)
				return new List<IFile>();

			var iFolder = FindFolder(folder);

			if (iFolder == null)
				return new List<IFile>();

			using (var db = GetReadContext())
			{
				IQueryable<ef_DBFile> query = db.Files
					.Where(f => !f.Deleted && f.FolderId.ToString() == iFolder.Id)
					.OrderBy(f => f.Name);

				if (start > 0)
					query = query.Skip(start);

				if (count > 0)
					query = query.Take(count);

				var files = query.ToList();

				var filesList = files.Select(f => new EFFile()
				{
					Id = f.FileId,
					Created = f.Created,
					Name = f.Name,
					Path = f.GetFullPath()
				}).ToList();

				return filesList;
			}
		}

		/// <summary>
		/// Returns files by file IDs 
		/// </summary>
		/// <param name="ids">IDs of files to get</param>
		/// <returns>files by file IDs</returns>
		public IEnumerable<IFile> GetFiles(IEnumerable<string> ids)
		{
			//var fileIds = ids.Select(int.Parse).ToList();

			using (var db = GetReadContext())
			{
				var query = db.Files
					.AsQueryable()
					.Where(f => !f.Deleted && ids.Contains(f.FileId));

				var files = query.ToList();

				var filesList = files.Select(f => new EFFile()
				{
					Id = f.FileId,
					Created = f.Created,
					Name = f.Name,
					Path = f.GetFullPath()
				}).ToList();

				return filesList;
			}
		}

		/// <summary>
		/// Deletes files by file IDs 
		/// </summary>
		/// <param name="ids">IDs of files to delete</param>
		/// <returns>True is success</returns>
		public void DeleteFiles(IEnumerable<string> ids)
		{
			using (var db = GetUpdateContext())
			{
				db.Configuration.AutoDetectChangesEnabled = true;
				db.Configuration.ValidateOnSaveEnabled = true;

				//var fileIds = ids.Select(int.Parse);
				//db.Files.Where(f => fileIds.Contains(f.Id)).Select(f => f.Blob).Delete();
				foreach (var f in db.Files.Where(f => ids.Contains(f.FileId)))
				{
					f.Deleted = true;
				}

				db.SaveChanges();

				db.Configuration.AutoDetectChangesEnabled = false;
				db.Configuration.ValidateOnSaveEnabled = false;
				DeleteFilesEventHandler handler = DeleteFilesEvent;
				if (handler != null)
				{
					handler(this, new DeleteFilesEventArgs() { FileIds = ids });
				}
			}
		}
		/// <summary>
		/// Renames file name
		/// </summary>
		/// <param name="id">File Id</param>
		/// <param name="newName">New file name</param>
		/// <returns></returns>
		public bool RenameFile(string id, string newName)
		{
			var fileName = newName.Trim();
			using (var db = GetUpdateContext())
			{
				var file = db.Files.FirstOrDefault(f => f.FileId == id);
				if (file == null)
					throw new FileNotFoundException("File not found. File Id:" + id);

				var folderId = file.Folder.Id;
				// have duplicate names in the same folder?
				var validation = db.Files.Where(f => folderId == f.Folder.Id && (f.Name == fileName && f.FileId != file.FileId));

				if (validation.Any())
					throw new IOException("File exists");

				file.Name = newName;

				return db.SaveChanges() > 0;
			}
		}

		/// <summary>
		/// Finds folder with the specified path
		/// </summary>
		/// <param name="path">Path to the folder</param>
		/// <returns>Null if nothing was found and IFolder instance otherwise</returns>
		public IFolder FindFolder(string path)
		{
			return FindFolder(path, null);
		}

		/// <summary>
		/// Creates subfolder inside folder
		/// </summary>
		/// <param name="path">Path to the folder</param>
		/// <param name="name">New folder name</param>
		/// <returns>Folder Id</returns>
		public IFolder CreateFolder(string path, string name, IDictionary<string, string> meta = null)
		{
			return CreateFolder(Path.Combine(path ?? "", name ?? ""), (int?)null, meta);
		}

		/// <summary>
		/// Returns list of folders inside specified folder
		/// </summary>
		/// <param name="folder">Folder's path</param>
		/// <param name="start">Ordinal start number of folders</param>
		/// <param name="count">Counts of folders to return</param>
		/// <returns></returns>
		public IEnumerable<IFolder> GetFolders(string folder, int start = 0, int count = -1)
		{
			var iFolder = FindFolder(folder);

			if (iFolder == null)
				return new List<IFolder>();

			using (var db = GetReadContext())
			{
				IQueryable<ef_DBFolder> query = db.Folders
					.Where(f => !f.Deleted && f.ParentId.ToString() == iFolder.Id)
					.OrderBy(f => f.Name);

				if (start > 0)
					query = query.Skip(start);

				if (count > 0)
					query = query.Take(count);

				return query.ToList().Select(f => f.ToEFFolder()).ToList();
			}
		}

		/// <summary>
		/// Returns folders by file Ids 
		/// </summary>
		/// <param name="ids">Ids of folders to return</param>
		/// <returns>Folders by Ids</returns>
		public IEnumerable<IFolder> GetFolders(IEnumerable<string> ids)
		{
			var folderIds = ids.Select(int.Parse).ToList();

			using (var db = GetReadContext())
			{
				return db.Folders
					.AsQueryable()
					.Where(f => !f.Deleted && folderIds.Contains(f.Id))
					.ToList()
					.Select(f => f.ToEFFolder())
					.ToList();
			}
		}

		/// <summary>
		/// Renames folder name
		/// </summary>
		/// <param name="id">Folder Id</param>
		/// <param name="newName">New folder name</param>
		/// <returns></returns>
		public bool RenameFolder(string id, string newName)
		{
			var folderName = newName.Trim();
			using (var db = GetUpdateContext())
			{
				var folder = db.Folders.FirstOrDefault(f => f.Id.ToString() == id);
				if (folder == null)
					throw new DirectoryNotFoundException("Directory not found. Id:" + id);

				var parentFolderId = folder.Parent.Id;
				// have duplicate names in the same folder?
				var validation = db.Folders.Where(f => f.ParentId == parentFolderId && (f.Name == folderName && f.Id != folder.Id));

				if (validation.Any())
					throw new IOException("Folder exists");

				folder.Name = newName;

				return db.SaveChanges() > 0;
			}
		}

		/// <summary>
		/// Deletes folders
		/// </summary>
		/// <param name="ids">List of folder Ids to delete</param>
		/// <returns>List of deleted folders</returns>
		public void DeleteFolders(string[] ids)
		{
			using (var db = GetUpdateContext())
			{
				var folderIds = ids.ToList();
				var fileIds = db.Files.Where(f => folderIds.Contains(f.Folder.Id.ToString())).Select(f => f.Id.ToString()).ToList();

				//var files = GetFiles(fileIds);
				db.Configuration.AutoDetectChangesEnabled = true;
				db.Configuration.ValidateOnSaveEnabled = true;
				if (fileIds.Any())
				{
					//db.Files.Where(f => fileIds.Contains(f.Id.ToString())).Select(f => f.Blob).Delete();
					//db.Files.Where(f => fileIds.Contains(f.Id.ToString())).Delete();
					foreach (var f in db.Files.Where(f => fileIds.Contains(f.Id.ToString())))
					{
						f.Deleted = true;
					}
				}
				foreach (var folderId in ids)
				{
					var id = int.Parse(folderId);
					db.Folders.First(f => f.Id == id).Deleted = true;
				}
				//if (folders.Any())
				//{
				//	db.Folders.Where(f => folderIds.Contains(f.Id.ToString())).Delete();
				//}

				db.SaveChanges();
				db.Configuration.AutoDetectChangesEnabled = false;
				db.Configuration.ValidateOnSaveEnabled = false;
				if (fileIds.Any() && DeleteFilesEvent != null)
				{
					DeleteFilesEvent(this, new DeleteFilesEventArgs() { FileIds = fileIds });
				}

				if (ids.Any() && DeleteFoldersEvent != null)
				{
					DeleteFoldersEvent(this, new DeleteFoldersEventArgs() { FolderIds = ids });
				}
			}
		}

		private IFolder FindFolder(string path, int? folderId = null)
		{
			if (string.IsNullOrEmpty(path))
				return null;

			var parts = path.Split(new char[] { '/', '\\' }).Where(f => !string.IsNullOrEmpty(f)).ToList();

			if (!parts.Any())
				return null;

			using (var db = GetReadContext())
			{
				var subFolder = db.Folders
					.Where(f => !f.Deleted && (f.ParentId == folderId || f.ParentId == null && folderId == null) && f.Name == parts.FirstOrDefault())
					.FirstOrDefault();

				if (subFolder != null)
				{
					if (parts.Count() == 1)
					{
						return subFolder.ToEFFolder();
					}
					else
					{
						parts.RemoveAt(0);

						return FindFolder(string.Join("/", parts.ToArray()), subFolder.Id);
					}
				}

				return null;
			}
		}

		private IFolder CreateFolder(string path, int? folderId = null, IDictionary<string, string> meta = null)
		{
			if (string.IsNullOrEmpty(path))
				return null;

			var parts = path.Split(new char[] { '/', '\\' }).Where(f => !string.IsNullOrEmpty(f)).ToList();

			if (!parts.Any())
				return null;

			using (var db = GetUpdateContext())
			{
				var folderName = parts.FirstOrDefault();

				var subFolder = db.Folders
					.Where(f => !f.Deleted && (f.ParentId == folderId || f.ParentId == null && folderId == null) && f.Name == folderName)
					.FirstOrDefault();

				if (subFolder == null)
				{
					subFolder = new ef_DBFolder()
					{
						Name = folderName,
						Created = DateTime.Now,
						ParentId = folderId
					};

					db.Folders.Add(subFolder);

					db.SaveChanges();
				}

				if (parts.Count() == 1)
				{
					var newFolder = subFolder.ToEFFolder();

					CreateFolderEventHandler handler = CreateFolderEvent;
					if (handler != null)
					{
						handler(this, new CreateFolderEventArgs() { Folder = newFolder, Meta = meta });
					}

					return newFolder;
				}
				else
				{
					parts.RemoveAt(0);

					return CreateFolder(string.Join("/", parts.ToArray()), subFolder.Id);
				}
			}
		}

		private IEnumerable<IFolder> GetWithSubFolders(IEnumerable<string> ids)
		{
			List<IFolder> folders = new List<IFolder>();

			var folderIds = ids.Select(int.Parse).ToList();

			IEnumerable<string> subfolderIds = new List<string>();

			using (var db = GetReadContext())
			{
				folders = db.Folders
					.AsQueryable()
					.Where(f => !f.Deleted && folderIds.Contains(f.Id))
					.ToList()
					.Select(f => f.ToEFFolder())
					.Cast<IFolder>()
					.ToList();

				if (folders.Any())
				{
					subfolderIds = db.Folders
						.AsQueryable()
						.Where(f => !f.Deleted && folderIds.Contains((int)f.ParentId))
						.Select(f => f.Id.ToString())
						.ToList();
				}
			}

			if (subfolderIds.Any())
			{
				folders.AddRange(GetWithSubFolders(subfolderIds));
			}

			return folders;
		}

		/// <summary>
		/// Move file to another folder
		/// </summary>
		/// <param name="id">File Id</param>
		/// <param name="folderId">Target folder Id</param>
		/// <returns>Success moved</returns>
		public bool MoveFiles(string folderId, IEnumerable<string> ids)
		{
			using (var db = GetUpdateContext())
			{
				var folder = db.Folders.FirstOrDefault(f => f.Id.ToString() == folderId);

				// exclude duplicates
				var existFiles = db.Files.Where(f => f.FolderId == folder.Id).Select(f => f.Name);
				var files = db.Files.Where(f => ids.Contains(f.Id.ToString()) && !existFiles.Contains(f.Name));

				foreach (var file in files)
				{
					if (file.FolderId != folder.Id)
						file.FolderId = folder.Id;
				}
				return db.SaveChanges() > 0;
			}
		}
		/// <summary>
		/// Move folder to anther folder
		/// </summary>
		/// <param name="id">Current folder Id</param>
		/// <param name="targetFolderId">Targer folder Id</param>
		/// <returns>Success moved</returns>
		public bool MoveFolders(string targetFolderId, IEnumerable<string> folderIDs)
		{
			using (var db = GetUpdateContext())
			{
				var targetFolder = db.Folders.FirstOrDefault(f => f.Id.ToString() == targetFolderId);

				// exclude duplicates
				var existFolders = db.Folders.Where(f => f.ParentId == targetFolder.Id).Select(f => f.Name);
				var currentFolders = db.Folders.Where(f => folderIDs.Contains(f.Id.ToString()) && !existFolders.Contains(f.Name));

				if (targetFolder != null && !targetFolder.Deleted)
				{
					foreach (var folder in currentFolders)
					{
						if (folder.Id != targetFolder.Id && folder.ParentId != targetFolder.Id)
							folder.ParentId = targetFolder.Id;
					}
				}

				return db.SaveChanges() > 0;
			}
		}
	}
}
