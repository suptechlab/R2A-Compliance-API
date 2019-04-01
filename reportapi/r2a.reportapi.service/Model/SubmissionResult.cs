using System.Collections.Generic;
using R2A.ReportApi.Service.Model;

namespace Tester.Model
{
    public class SubmissionResult
    {
        public SubmissionInfo SubmissionInfo { get; set; }
        public List<ValidationRule> ProcessingResult { get; set; }
        
        
    }
}