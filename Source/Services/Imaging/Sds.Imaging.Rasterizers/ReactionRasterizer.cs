using System;
using System.Drawing;
using System.IO;

namespace Sds.Imaging.Rasterizers
{
	internal class ReactionRasterizer : IFileRasterizer
	{
		public Image Rasterize(Stream data, string type)
		{
			using (StreamReader dataReader = new StreamReader(data))
			{
				var imageBytes = new IndigoAdapter().Rxn2Image(dataReader.ReadToEnd());
				using (var ms = new MemoryStream(imageBytes, 0, imageBytes.Length))
				{
					Image image = Image.FromStream(ms, true);
					return image;
				}
			}
		}
	}
}
