using Aspose.Pdf;
using Aspose.Pdf.Generator;
using System;
using System.Drawing;
using System.IO;
using System.Net;

using SelectPdf;

namespace Sds.Imaging.Rasterizers
{
	internal class HtmlRasterizer
	{
		public System.Drawing.Image Rasterize(Uri uri)
		{
			string appBinFolder = AppDomain.CurrentDomain.BaseDirectory + @"bin";

			var path = GlobalProperties.HtmlEngineFullPath = Path.Combine(appBinFolder, "Select.Html.dep");

			HtmlToPdf converter = new HtmlToPdf();
			PdfDocument doc = converter.ConvertUrl(uri.AbsoluteUri);

			var stream = new MemoryStream();
			doc.Save(stream);
			stream.Seek(0, SeekOrigin.Begin);

			PdfRasterizer pdfRasterizer = new PdfRasterizer();
			var image = pdfRasterizer.Rasterize(stream, "pdf");
			return image;
		}
	}
}
