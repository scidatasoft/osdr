using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Sds.FileStorage.DiskStorage
{
	/// <summary>
	/// Physical disk file storage
	/// </summary>
	public class DiskStorage : IFileStorage
	{
		private string root;

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

		public DiskStorage() : this(Path.GetTempPath())
		{
		}

		public DiskStorage(string root)
		{
			this.root = root;

			if (!Directory.Exists(root))
				Directory.CreateDirectory(root);
		}

		/// <summary>
		/// Saves a Stream inside a folder under supplied file name
		/// </summary>
		/// <param name="folder">Folder to save the file into</param>
		/// <param name="fileName">File name</param>
		/// <param name="file">File contenct</param>
		public IEnumerable<IFile> GetFiles(string folder, int start = 0, int count = -1)
		{
			foreach (var fileInfo in Directory.GetFiles(Path.Combine(root, folder)))
			{
				yield return new DiskFile(fileInfo.Replace(root, ""));
			}
		}

		/// <summary>
		/// Saves a byte array inside a folder under supplied file name
		/// </summary>
		/// <param name="folder">Folder to save the file into</param>
		/// <param name="fileName">File name</param>
		/// <param name="data">File contenct</param>
		/// <returns>information about the saved file</returns>
		public IFile AddFile(string folder, string fileName, byte[] data, IDictionary<string, string> meta = null)
		{
			Directory.CreateDirectory(Path.Combine(root, folder));
			string path = Path.Combine(root, folder, fileName);

			File.WriteAllBytes(path, data);

			return new DiskFile(path);
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
			Directory.CreateDirectory(Path.Combine(root, folder));
			string path = Path.Combine(root, folder, fileName);

			using (var fileStream = File.Create(path))
			{
				data.Position = 0;
				data.CopyStream(fileStream);
			}

			return new DiskFile(path);
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
            Directory.CreateDirectory(Path.Combine(root, folder));
            string path = Path.Combine(root, folder, fileName);

            using (var fileStream = File.Create(path))
            {
                data.Position = 0;
                data.CopyStream(fileStream);
            }

            return new DiskFile(path);
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

			var files = Directory.GetFiles(Path.Combine(root, folder));

			return files.Select(f => f.Replace(root, ""));
		}

		/// <summary>
		/// Returns files by file IDs 
		/// </summary>
		/// <param name="ids">IDs of files to get</param>
		/// <returns>files by file IDs</returns>
		public IEnumerable<IFile> GetFiles(IEnumerable<string> ids)
		{
			return ids.Select(id => new DiskFile(id));
		}

		/// <summary>
		/// Deletes files by file IDs 
		/// </summary>
		/// <param name="ids">IDs of files to delete</param>
		/// <returns>True is success</returns>
		public void DeleteFiles(IEnumerable<string> ids)
		{
			ids.AsParallel().ForAll(id =>
			{
				var path = Path.Combine(root, id);

				if (File.Exists(path))
					File.Delete(path);
			});
		}

		/// <summary>
		/// Renames file name
		/// </summary>
		/// <param name="id">File Id</param>
		/// <param name="newName">New file name</param>
		/// <returns></returns>
		public bool RenameFile(string id, string newName)
		{
			var path = Path.Combine(root, id);

			if (File.Exists(path))
			{
				var fi = new FileInfo(path);

				File.Move(path, path.Replace(fi.Name, newName));

				return true;
			}

			return false;
		}

		/// <summary>
		/// Finds folder with the specified path
		/// </summary>
		/// <param name="path">Path to the folder</param>
		/// <returns>Null if nothing was found and IFolder instance otherwise</returns>
		public IFolder FindFolder(string path)
		{
			var fullPath = Path.Combine(root, path);

			if (Directory.Exists(fullPath))
				return new DiskFolder(fullPath);

			return null;
		}

		/// <summary>
		/// Creates subfolder inside folder
		/// </summary>
		/// <param name="path">Path to the folder</param>
		/// <param name="name">New folder name</param>
		/// <returns>Folder Id</returns>
		public IFolder CreateFolder(string path, string name, IDictionary<string, string> meta = null)
		{
			var fullPath = Path.Combine(root, path);

			if (Directory.Exists(fullPath))
			{
				var di = Directory.CreateDirectory(Path.Combine(fullPath, name));

				return new DiskFolder(di.FullName);
			}

			return null;
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
			var folders = Directory.GetDirectories(Path.Combine(root, folder));

			return folders.Select(f => new DiskFolder(f.Replace(root, "")));
		}

		/// <summary>
		/// Returns folders by file Ids 
		/// </summary>
		/// <param name="ids">Ids of folders to return</param>
		/// <returns>Folders by Ids</returns>
		public IEnumerable<IFolder> GetFolders(IEnumerable<string> ids)
		{
			return ids.Select(id => new DiskFolder(id));
		}

		/// <summary>
		/// Renames folder name
		/// </summary>
		/// <param name="id">Folder Id</param>
		/// <param name="newName">New folder name</param>
		/// <returns></returns>
		public bool RenameFolder(string id, string newName)
		{
			var path = Path.Combine(root, id);

			if (Directory.Exists(path))
			{
				var di = new DirectoryInfo(path);

				Directory.Move(path, path.Replace(di.Name, newName));

				return true;
			}

			return false;
		}

		/// <summary>
		/// Deletes folders
		/// </summary>
		/// <param name="ids">List of folder Ids to delete</param>
		/// <returns>List of deleted folders</returns>
		public void DeleteFolders(string[] ids)
		{
			ids.AsParallel().ForAll(id =>
			{
				var path = Path.Combine(root, id);

				if (Directory.Exists(path))
					Directory.Delete(path);
			});
		}

		public bool MoveFiles(string folderId, IEnumerable<string> ids)
		{
			var files = GetFiles(ids);
			var folder = GetFolders(folderId).First();

			if (Directory.Exists(folder.Path))
			{
				foreach (var file in files)
				{
					new FileInfo(file.Path).MoveTo(folder.Path);
				}

				return true;
			}

			return false;
		}

		public bool MoveFolders(string targetFolderId, IEnumerable<string> ids)
		{
			var folders = GetFolders(ids);
			var targetFolder = GetFolders(targetFolderId).First();

			if (Directory.Exists(targetFolder.Path))
			{
				foreach (var folder in folders)
				{
					new DirectoryInfo(folder.Path).MoveTo(targetFolder.Path);
				}

				return true;
			}

			return false;
		}
	}
}
