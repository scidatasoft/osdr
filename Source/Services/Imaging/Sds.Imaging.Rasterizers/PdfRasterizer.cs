using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Ghostscript.NET;
using Ghostscript.NET.Rasterizer;

namespace Sds.Imaging.Rasterizers
{
	internal class PdfRasterizer : IFileRasterizer
	{
		public Image Rasterize(Stream data, string type)
		{
			int width = 96;
			int height = 96;

            string appBinFolder = AppDomain.CurrentDomain.BaseDirectory + @"bin";

            GhostscriptVersionInfo version = new GhostscriptVersionInfo(Path.Combine(appBinFolder, Environment.Is64BitProcess ? "gsdll64.dll" : "gsdll32.dll"));

            using (var rasterizer = new GhostscriptRasterizer())
			{
				rasterizer.Open(data, version, true);
				var image = rasterizer.GetPage(width, height, 1);
				return image;
			}
		}
	}
}