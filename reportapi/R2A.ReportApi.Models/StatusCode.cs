using System.Collections.Generic;

namespace R2A.ReportApi.Models
{
    public class StatusCode
    {
        public string Code { get; }
        public string Label { get; }

        public StatusCode(string code, string label)
        {
            Code = code;
            Label = label;
        }

        public bool Equals(StatusCode status)
        {
            return Code.Equals(status.Code);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is StatusCode))
                return false;
            return this.Equals((StatusCode) obj);
        }

        public static bool operator ==(StatusCode status1, StatusCode status2)
        {
            if ((object)status1 == null)
            {
                if ((object)status2 == null)
                    return true;
                return false;
            }
            return status1.Equals(status2);
        }

        public static bool operator !=(StatusCode status1, StatusCode status2)
        {
            return !(status1 == status2);
        }

        public override int GetHashCode()
        {
            return Code.GetHashCode();
        }

        public override string ToString()
        {
            return $"{Code} - {Label}";
        }
    }

    public static class SubmissionStatus
    {
        //TODO localize labels
        public static readonly StatusCode InQueue = AddStatus("S1", "In queue");
        public static readonly StatusCode Processing = AddStatus("S2", "Processing");
        public static readonly StatusCode Accepted = AddStatus("S3", "Passed validation");
        public static readonly StatusCode Rejected = AddStatus("S4", "Rejected");
        public static readonly StatusCode Error = AddStatus("SE", "Error");

        private static Dictionary<string, StatusCode> Statuses = new Dictionary<string, StatusCode>(5);

        private static StatusCode AddStatus(string code, string label)
        {
            StatusCode status = new StatusCode(code, label);
            if(Statuses==null)
                Statuses = new Dictionary<string, StatusCode>(5);
            Statuses[code] = status;
            return status;
        }

        public static StatusCode GetStatusFromCode(string code)
        {
            return Statuses[code];
        }
    }

    public static class DataProcessingStatus
    {
        public static readonly StatusCode InQueue = AddStatus("DP1", "In queue");
        public static readonly StatusCode Processing = AddStatus("DP2", "Processing");
        public static readonly StatusCode ProcessedWithErrors = AddStatus("DP3", "Processed with errors");
        public static readonly StatusCode ProcessedSuccessfully = AddStatus("DP4", "Processed successfully");
        public static readonly StatusCode NotApplicable = AddStatus("DP0", "Not applicable");
        public static readonly StatusCode Error = AddStatus("DPE", "Error");

        private static Dictionary<string, StatusCode> Statuses = new Dictionary<string, StatusCode>(7);

        private static StatusCode AddStatus(string code, string label)
        {
            StatusCode status = new StatusCode(code, label);
            if (Statuses == null)
                Statuses = new Dictionary<string, StatusCode>(7);
            Statuses[code] = status;
            return status;
        }

        public static StatusCode GetStatusFromCode(string code)
        {
            return Statuses[code];
        }
    }
}