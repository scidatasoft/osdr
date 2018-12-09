using Aspose.Slides;
using Spire.Pdf;
using System.IO;

namespace Sds.OfficeProcessor.Processing.Converters
{
    public class PptToPdf : IConvert
    {
        public Stream Convert(Stream stream)
        {
            Stream pdfStream = new MemoryStream();
            var ppt = new Presentation(stream);
            ppt.Save(pdfStream, Aspose.Slides.Export.SaveFormat.Pdf);
            return pdfStream;
        }
    }
}
