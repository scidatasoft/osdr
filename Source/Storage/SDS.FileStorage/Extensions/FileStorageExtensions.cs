using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sds.FileStorage
{
    public static class FileStorageExtensions
    {
        public static IFile GetFile(this IFileStorage storage, string id)
        {
            return storage.GetFiles(new string[] { id }).FirstOrDefault();
        }

		public static void DeleteFile(this IFileStorage storage, string id)
		{
			storage.DeleteFiles(new string[] { id });
		}

		public static IEnumerable<IFile> GetFiles(this IFileStorage storage, IFolder folder)
		{
			return storage.GetFiles(folder.Path);
		}

		public static IEnumerable<IFolder> GetFolders(this IFileStorage storage, IFolder folder)
		{
			return storage.GetFolders(folder.Path);
		}
	}
}
