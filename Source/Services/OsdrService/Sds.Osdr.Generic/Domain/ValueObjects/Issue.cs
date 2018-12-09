using Sds.Domain;
using System;
using System.Collections.Generic;

namespace Sds.Osdr.Generic.Domain.ValueObjects
{
    public enum Severity
    {
        Fatal = 0,
        Error = 1,
        Warning = 2,
        Information = 3
    }

    public class Issue : ValueObject<Issue>
    {
        public string Code { set; get; }
        public Severity Severity { get; set; }
        public string Title { set; get; }
        public string Message { set; get; }
        public string AuxInfo { set; get; }

        protected override IEnumerable<object> GetAttributesToIncludeInEqualityCheck()
        {
            return new List<Object>() { Code };
        }
    }
}
