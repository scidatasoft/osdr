using System.IO;

namespace Sds.OfficeProcessor.Processing.Converters
{
    public interface IConvert
    {
        Stream Convert(Stream stream);
    }
}
