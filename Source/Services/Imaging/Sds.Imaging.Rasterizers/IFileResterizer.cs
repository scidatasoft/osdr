using System;
using System.Drawing;
using System.IO;

namespace Sds.Imaging.Rasterizers
{
	/// <summary>
	/// Provides file rasterization capabilities
	/// </summary>
    public interface IFileRasterizer
    {
		/// <summary>
		/// Returns image based on supplied binary stream from a file and the file type
		/// </summary>
		/// <param name="data">Binary stream</param>
		/// <param name="type">type of file</param>
		/// <returns>based on supplied binary stream from a file and the file type</returns>
		Image Rasterize(Stream data, string type);
    }
}
