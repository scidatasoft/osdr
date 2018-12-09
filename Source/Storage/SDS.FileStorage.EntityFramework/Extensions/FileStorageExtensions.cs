namespace Sds.FileStorage.EntityFramework
{
	public static class FileStorageExtensions
	{
		public static EFFolder ToEFFolder(this ef_DBFolder ef)
		{
			return new EFFolder
			{
				Id = ef.Id.ToString(),
				Created = ef.Created,
				Name = ef.Name,
				Path = ef.GetFullPath()
			};
		}

		public static EFFile ToEFFile(this ef_DBFile ef)
		{
			return new EFFile
			{
				Id = ef.FileId,
				Name = ef.Name,
				Created = ef.Created
			};
		}
	}
}