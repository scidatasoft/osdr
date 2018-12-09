namespace Sds.Osdr.WebApi.Requests
{
    public class CsvPreviewRequest
    {
        public int Start { get; set; } = 0;
        public int Count { get; set; } = 10;
        public int Columns { get; set; } = -1;
    }
}
