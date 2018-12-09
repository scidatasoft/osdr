using Aspose.Words;
using System.IO;

namespace Sds.OfficeProcessor.Processing.Converters
{
    public class DocToPdf : IConvert
    {
        public Stream Convert(Stream stream)
        {
            var pdfStream = new MemoryStream();
            var doc = new Document(stream, new LoadOptions());
            var author = doc.BuiltInDocumentProperties.Author;
            var company = doc.BuiltInDocumentProperties.Company;
            var date = doc.BuiltInDocumentProperties.CreatedTime;
            doc.Save(pdfStream, SaveFormat.Pdf);
            return pdfStream;
        }
    }
}
