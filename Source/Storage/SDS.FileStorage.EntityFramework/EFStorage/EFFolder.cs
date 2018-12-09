using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sds.FileStorage.EntityFramework
{
    public class EFFolder : IFolder
    {
        /// <summary>
        /// Gets or sets unique folder ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets folder name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets full folder's path including name
        /// </summary>
        public string Path { get; set; }

		/// <summary>
		/// Date and time when file was created
		/// </summary>
		public DateTime Created { get; set; }
	}
}
