using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace Sds.Imaging.Rasterizers
{
	/// <summary>
	/// Provides file rasterization capabilities
	/// </summary>
	public class FileRasterizer
	{
		private static readonly IDictionary<string, Func<IFileRasterizer>> _initialzers = new Dictionary<string, Func<IFileRasterizer>>();
		private static readonly string[] _recordBased;

		static FileRasterizer()
		{
			_initialzers[".pdf"] = () => new PdfRasterizer();
			_initialzers[".jpg"] = () => new ImageRasterizer();
			_initialzers[".jpeg"] = () => new ImageRasterizer();
			_initialzers[".png"] = () => new ImageRasterizer();
			_initialzers[".bmp"] = () => new ImageRasterizer();
			_initialzers[".gif"] = () => new ImageRasterizer();
			_initialzers[".doc"] = () => new OfficeFileRasterizer();
			_initialzers[".docx"] = () => new OfficeFileRasterizer();
			_initialzers[".xls"] = () => new OfficeFileRasterizer();
			_initialzers[".xlsx"] = () => new OfficeFileRasterizer();
			_initialzers[".ppt"] = () => new OfficeFileRasterizer();
			_initialzers[".pptx"] = () => new OfficeFileRasterizer();
			_initialzers[".mol"] = () => new StructureRasterizer();
			_initialzers[".cdx"] = () => new StructureRasterizer();
			_initialzers[".sdf"] = () => new StructureRasterizer();
			_initialzers[".cif"] = () => new StructureRasterizer();
			_initialzers[".json"] = () => new StructureRasterizer();
			_initialzers[".rxn"] = () => new ReactionRasterizer();

			_recordBased = new string[]
			{
				".mol",
				".cdx",
				".sdf",
				".cif",
				".rxn",
				".json"
			};
		}

		public static bool Supports(string extension)
		{
			return _initialzers.ContainsKey(extension.Trim().ToLower());
		}

		/// <summary>
		/// Returns true if the rasterizer needs content of the record instead of the content of the file
		/// </summary>
		public static bool IsRecordBased(string extension)
		{
			return _recordBased.Any(x => x.Equals(extension, StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>
		/// Returns image based on supplied binary stream from a file and the file type
		/// </summary>
		/// <param name="data">Binary stream</param>
		/// <param name="extension">file extension</param>
		/// <returns>based on supplied binary stream from a file and the file type</returns>
		public Image Rasterize(Stream data, string extension)
		{
			if (string.IsNullOrEmpty(extension))
			{
				throw new ArgumentException("extension is not supplied");
			}

			Func<IFileRasterizer> initializer;
			if (_initialzers.TryGetValue(extension.Trim().ToLower(), out initializer))
			{
				var rasterizer = initializer();
				return rasterizer.Rasterize(data, extension);
			}

			return null;
		}
	}
}
