namespace Sds.ChemicalStandardizationValidation.Domain.Models
{
    public enum Severity
    {
        Fatal = 0,
        Error = 1,
        Warning = 2,
        Information = 3
    }

    public class Issue
    {
        public string Code { set; get; }
        public Severity Severity { get; set; }
        public string Title { set; get; }
        public string Message { set; get; }
        public string AuxInfo { set; get; }
    }
}
