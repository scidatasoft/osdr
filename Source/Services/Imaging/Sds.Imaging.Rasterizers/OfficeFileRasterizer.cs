using Aspose.Cells;
using Aspose.Slides;
using Aspose.Words;
using System.Drawing;
using System.IO;

namespace Sds.Imaging.Rasterizers
{
	internal class OfficeFileRasterizer : IFileRasterizer
	{
		public Image Rasterize(Stream data, string type)
		{
			Stream stream = new MemoryStream();

			switch(type)
			{
				case ".doc":
				case ".docx":
					Document doc = new Document(data);
					doc.Save(stream, Aspose.Words.SaveFormat.Pdf);
					break;
				case ".xls":
				case ".xlsx":
					Workbook workbook = new Workbook(data);
					workbook.Save(stream, Aspose.Cells.SaveFormat.Pdf);
					break;
				case ".ppt":
				case ".pptx":
					Presentation ppt = new Presentation(data);
					ppt.Save(stream, Aspose.Slides.Export.SaveFormat.Pdf);
					break;
			}
			
			PdfRasterizer pdfRasterizer = new PdfRasterizer();
			var img = pdfRasterizer.Rasterize(stream, "pdf");
			return img;
		}
	}
}
