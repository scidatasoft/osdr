using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sds.FileStorage.EntityFramework
{
	public partial class FileStorageContext : DbContext
	{
		public FileStorageContext()
			: base(Environment.ExpandEnvironmentVariables(ConfigurationManager.ConnectionStrings["FileStorageConnection"].ConnectionString))
		{
			//Database.SetInitializer<OsdrContext>(new CreateDatabaseIfNotExists<OpenNMRContext>());
			Database.SetInitializer<FileStorageContext>(new MigrateDatabaseToLatestVersion<FileStorageContext, Sds.FileStorage.EntityFramework.Configuration>());

			//if (System.Configuration.ConfigurationManager.ConnectionStrings["CVSPConnection"] != null)
			//	Database.CommandTimeout = System.Configuration.ConfigurationManager.ConnectionStrings["CVSPConnection"].Timeout();
		}

        public virtual DbSet<ef_Blob> Blobs { get; set; }
        public virtual DbSet<ef_DBFile> Files { get; set; }
		public virtual DbSet<ef_DBFolder> Folders { get; set; }

		protected override void OnModelCreating(DbModelBuilder modelBuilder)
		{
		}
	}
}
