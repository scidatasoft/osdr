using Aspose.Cells;
using System.IO;

namespace Sds.OfficeProcessor.Processing.Converters
{
    public class XlsToPdf : IConvert
    {
        public Stream Convert(Stream stream)
        {
            Stream pdfStream = new MemoryStream();
            var workbook = new Workbook(stream);
            workbook.Save(pdfStream, SaveFormat.Pdf);
            return pdfStream;
        }
    }
}
