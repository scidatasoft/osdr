using Sds.ChemicalStandardizationValidation.Domain.Models;
using Sds.Cvsp;
using System.Collections.Generic;
using System.Linq;

namespace Sds.ChemicalStandardizationValidation.Processing
{
    public static class IssuesResolver
    {
        public static List<Issue> ResolveIssues(IEnumerable<Sds.Domain.Issue> issues, IIssuesConfig issuesConfig)
        {
            var logIssues = issuesConfig.EntryTypes.ToList();

            var res = new List<Issue>();
            foreach (var issue in issues)
            {
                var issueByCode = logIssues.Where(i => i.Code == issue.Code).Single();

                res.Add(new Issue
                {
                    Code = issue.Code,
                    Title = issueByCode.Title,
                    Message = issue.Message,
                    AuxInfo = issue.AuxInfo,
                    Severity = (Domain.Models.Severity)issueByCode.Severity
                });
            }
            return res;
        }
    }
}
