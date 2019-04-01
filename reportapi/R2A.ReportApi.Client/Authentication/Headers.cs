using Microsoft.AspNetCore.Http;

namespace R2A.ReportApi.Client.Authentication
{
    public static class Headers
    {
        public const string CertificateSubjectHeader = "X-Certificate-Subject";
        public const string CertificateThumbprintHeader = "X-Certificate-Thumbprint";
        public const string CertificateIssuerHeader = "X-Certificate-Issuer";
        public const string CertificateVerification = "X-Certificate-Verify";



        public static bool HasCertificateInfo(this HttpRequest request)
        {
            return !string.IsNullOrEmpty(request.Headers[CertificateSubjectHeader]) 
                && !string.IsNullOrEmpty(request.Headers[CertificateThumbprintHeader]) 
                && !string.IsNullOrEmpty(request.Headers[CertificateIssuerHeader]);
        }

        public static bool CertificateVerified(this HttpRequest request)
        {
            return request.Headers[CertificateVerification] == "SUCCESS";
        }
    }
}
