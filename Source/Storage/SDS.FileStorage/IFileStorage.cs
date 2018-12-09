using System;
using System.Collections.Generic;
using System.IO;

namespace Sds.FileStorage
{
    public delegate void AddFileEventHandler(object sender, AddFileEventArgs e);

	public delegate void DeleteFilesEventHandler(object sender, DeleteFilesEventArgs e);

	public delegate void CreateFolderEventHandler(object sender, CreateFolderEventArgs e);

	public delegate void DeleteFoldersEventHandler(object sender, DeleteFoldersEventArgs e);

	public class AddFileEventArgs : EventArgs
	{
		public IFile File { get; set; }
        public IDictionary<string, string> Meta { get; set; }
	}

	public class DeleteFilesEventArgs : EventArgs
	{
		public IEnumerable<string> FileIds { get; set; }
	}

	public class CreateFolderEventArgs : EventArgs
	{
		public IFolder Folder { get; set; }
        public IDictionary<string, string> Meta { get; set; }
    }

    public class DeleteFoldersEventArgs : EventArgs
	{
		public IEnumerable<string> FolderIds { get; set; }
	}

	/// <summary>
	/// Provides an interface to store and retrieve files
	/// </summary>
	public interface IFileStorage
	{
		#region evnts
		event AddFileEventHandler OnFileAdd;

		event DeleteFilesEventHandler OnFilesDelete;

		event CreateFolderEventHandler OnFolderCreate;

		event DeleteFoldersEventHandler OnFoldersDelete;
		#endregion

		#region files
		/// <summary>
		/// Saves a byte array inside a folder under supplied file name
		/// </summary>
		/// <param name="path">Folder path to save the file into</param>
		/// <param name="fileName">File name</param>
		/// <param name="data">File's content</param>
        /// <param name="meta">Metadata assigned to the file</param>
		/// <returns>information about the saved file</returns>
		IFile AddFile(string path, string fileName, byte[] data, IDictionary<string, string> meta = null);

		/// <summary>
		/// Saves a data from stream inside a folder under supplied file name
		/// </summary>
		/// <param name="path">Folder path to save the file into</param>
		/// <param name="fileName">File name</param>
		/// <param name="data">File's content</param>
		/// <param name="meta">Metadata assigned to the file</param>
		/// <returns>information about the saved file</returns>
		IFile AddFile(string path, string fileName, Stream data, IDictionary<string, string> meta = null);

        /// <summary>
        /// Saves a data from stream inside a folder under supplied file name
        /// </summary>
        /// <param name="id">File Id</param>
		/// <param name="path">Folder path to save the file into</param>
		/// <param name="fileName">File name</param>
        /// <param name="data">File's content</param>
        /// <param name="meta">Metadata assigned to the file</param>
        /// <returns>information about the saved file</returns>
        IFile AddFile(string id, string path, string fileName, Stream data, IDictionary<string, string> meta = null);

        /// <summary>
        /// Returns list of file Ids inside a specified folder
        /// </summary>
        /// <param name="folder">Folder</param>
        /// <param name="start">Ordinal start number of compounds</param>
        /// <param name="count">Counts of compounds to return</param>
        /// <returns>List of file Ids inside a specified folder</returns>
        IEnumerable<string> GetFileIds(string folder, int start = 0, int count = -1);

		/// <summary>
		/// Returns list of files inside a specified folder
		/// </summary>
		/// <param name="folder">Folder</param>
		/// <param name="start">Ordinal start number of files</param>
		/// <param name="count">Counts of files to return</param>
		/// <returns>list of files inside a specified folder</returns>
		IEnumerable<IFile> GetFiles(string folder, int start = 0, int count = -1);

		/// <summary>
		/// Returns files by file IDs 
		/// </summary>
		/// <param name="ids">IDs of files to get</param>
		/// <returns>files by file IDs</returns>
		IEnumerable<IFile> GetFiles(IEnumerable<string> ids);

		/// <summary>
		/// Deletes files by file IDs 
		/// </summary>
		/// <param name="ids">IDs of files to delete</param>
		/// <returns>List of deleted files</returns>
		void DeleteFiles(IEnumerable<string> ids);

		/// <summary>
		/// Renames file name
		/// </summary>
		/// <param name="id">File Id</param>
		/// <param name="newName">New file name</param>
		/// <returns></returns>
		bool RenameFile(string id, string newName);

		/// <summary>
		/// Move file from current folder to another
		/// </summary>
		/// <param name="id">File Id</param>
		/// <param name="folderId">Target folder Id</param>
		/// <returns></returns>
		bool MoveFiles(string targetFolderId, IEnumerable<string> ids);
		#endregion

		#region folders
		/// <summary>
		/// Finds folder with the specified path
		/// </summary>
		/// <param name="path">Path to the folder</param>
		/// <returns>Null if nothing was found and IFolder instance otherwise</returns>
		IFolder FindFolder(string path);

		/// <summary>
		/// Creates subfolder inside folder
		/// </summary>
		/// <param name="path">Path to the folder</param>
		/// <param name="name">New folder name</param>
        /// <param name="meta">Metadata assigned to new folder</param>
		/// <returns>Folder Id</returns>
		IFolder CreateFolder(string path, string name, IDictionary<string, string> meta = null);

		/// <summary>
		/// Returns list of folders inside specified folder
		/// </summary>
		/// <param name="folder">Folder's path</param>
		/// <param name="start">Ordinal start number of folders</param>
		/// <param name="count">Counts of folders to return</param>
		/// <returns></returns>
		IEnumerable<IFolder> GetFolders(string folder, int start = 0, int count = -1);

		/// <summary>
		/// Returns folders by file IDs 
		/// </summary>
		/// <param name="ids">IDs of folders to get</param>
		/// <returns>Folders by IDs</returns>
		IEnumerable<IFolder> GetFolders(IEnumerable<string> ids);

		/// <summary>
		/// Renames folder name
		/// </summary>
		/// <param name="id">Folder Id</param>
		/// <param name="newName">New folder name</param>
		/// <returns></returns>
		bool RenameFolder(string id, string newName);

		/// <summary>
		/// Move folder to another folder
		/// </summary>
		/// <param name="id">Move folder Id</param>
		/// <param name="targetFolderId">Target folder Id</param>
		/// <returns></returns>
		bool MoveFolders(string targetFolderId, IEnumerable<string> ids);

		/// <summary>
		/// Deletes folders
		/// </summary>
		/// <param name="ids">List of folder Ids to delete</param>
		/// <returns>List of deleted folders</returns>
		void DeleteFolders(string[] ids);
		#endregion
	}
}
