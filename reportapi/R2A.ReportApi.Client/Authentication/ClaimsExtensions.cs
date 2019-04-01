using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using R2A.ReportApi.Client.Authentication.Certificate;

namespace R2A.ReportApi.Client.Authentication
{
    public static class ClaimsExtensions
    {
        public const string ClaimTypeIssuer = "CertificateIssuer";


        public static void AddCertificateClaims(this IList<Claim> claims, UserCertificateInfo certificateInfo)
        {
            claims.Add(new Claim(ClaimTypes.X500DistinguishedName, certificateInfo.Subject));
            claims.Add(new Claim(ClaimTypes.Thumbprint, certificateInfo.Thumbprint));
            claims.Add(new Claim(ClaimTypeIssuer, certificateInfo.Issuer));
        }

        public static UserCertificateInfo GetCertificateInfo(this IEnumerable<Claim> claims)
        {
            return new UserCertificateInfo()
            {
                Subject = claims?.FirstOrDefault(c => c.Type == ClaimTypes.X500DistinguishedName)?.Value,
                Issuer = claims?.FirstOrDefault(c => c.Type == ClaimTypeIssuer)?.Value,
                Thumbprint = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Thumbprint)?.Value
            };
        }



        public static string GetValueOfType(this IEnumerable<Claim> claims, string claimType)
        {
            return claims.FirstOrDefault(c => c.Type == claimType)?.Value;
        }
            
    }
}
