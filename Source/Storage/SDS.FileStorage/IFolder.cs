using System;

namespace Sds.FileStorage
{
    public interface IFolder
    {
        /// <summary>
        /// Gets or sets unique folder ID
        /// </summary>
        string Id { get; set; }

        /// <summary>
        /// Gets or sets folder name
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Gets full folder's path including name
        /// </summary>
        string Path { get; set; }

		/// <summary>
		/// Date and time when folder was created
		/// </summary>
		DateTime Created { get; set; }
	}
}
