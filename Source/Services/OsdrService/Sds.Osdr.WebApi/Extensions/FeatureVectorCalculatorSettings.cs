namespace Sds.Osdr.WebApi.Extensions
{
    public class FeatureVectorCalculatorSettings
    {
        public string[] SupportedFormats { get; set; } = new string[] { ".sdf", ".csv" };
        public long MaxFileSize { get; set; } = 10485760;
    }
}
