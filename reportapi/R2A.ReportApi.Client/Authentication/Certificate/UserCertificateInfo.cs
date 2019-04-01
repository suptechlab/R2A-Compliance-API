using System.Collections.Generic;
using R2A.ReportApi.Models;

namespace R2A.ReportApi.Client.Authentication.Certificate
{
    public class UserCertificateInfo 
    {
        public string Subject { get; set; }
        public string Issuer { get; set; }
        public string Thumbprint { get; set; }

        public Dictionary<string, string> ToSparseDictionary() 
        {
            var dict = new Dictionary<string,string>();
            if (!string.IsNullOrEmpty(Subject)) dict[MessageHeaders.Subject] = Subject;
            if (!string.IsNullOrEmpty(Issuer)) dict[MessageHeaders.Issuer] = Issuer;
            if (!string.IsNullOrEmpty(Thumbprint)) dict[MessageHeaders.Thumbprint] = Thumbprint;
            return dict;
        }
    }
}
