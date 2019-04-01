using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Xml;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using R2A.ReportApi.Client.Common;
using IAuthenticationHandler = R2A.ReportApi.Client.Authentication.DI.IAuthenticationHandler;


namespace R2A.ReportApi.Client.Authentication.Certificate
{
    public class CertificateAuthenticationHandler : DI.IAuthenticationHandler
    {
        private const int BankCodeLength = 6;

        private readonly ILogger _logger;
        private readonly Settings _settings;

        public CertificateAuthenticationHandler(ILoggerFactory loggerFactory, Settings settings)
        {
            _logger = loggerFactory.CreateLogger(GetType());
            _settings = settings;
        }

        public async Task<AuthenticateResult> HandleAuthenticateAsync(HttpContext context)
        {
            var request = context.Request;

            if (!request.HasCertificateInfo())
            {
                _logger.LogDebug("No client certificate info found in headers.");
                return AuthenticateResult.NoResult();
            }

            var certificateInfo = new UserCertificateInfo()
            {
                Subject = request.Headers[Headers.CertificateSubjectHeader],
                Thumbprint = request.Headers[Headers.CertificateThumbprintHeader],
                Issuer = request.Headers[Headers.CertificateIssuerHeader]
            };
            
            if (!request.CertificateVerified())
            {
                _logger.LogDebug($"The given certificate was invalid. Reason: {request.Headers[Headers.CertificateVerification]}; Subject: {certificateInfo.Subject}; Issuer: {certificateInfo.Issuer};");
                return AuthenticateResult.NoResult();
            }

            var claims = new List<Claim>();
            claims.AddCertificateClaims(certificateInfo);

            string id = GetUserIdFromCertificate(certificateInfo.Subject,_settings.CertificateNamePrefix);
            if (string.IsNullOrEmpty(id))
            {
                _logger.LogDebug( $"Unable to extract user ID from certificate. Subject:{certificateInfo.Subject}");
                return AuthenticateResult.NoResult();
            }
            claims.Add(new Claim(ClaimTypes.Sid,id));

            var userIdentity = new ClaimsIdentity(claims, AuthenticationScheme.CertificateAuthentication);
            var userPrincipal = new ClaimsPrincipal(userIdentity);
            var ticket = new AuthenticationTicket(userPrincipal, new AuthenticationProperties(), AuthenticationScheme.CertificateAuthentication);
            return AuthenticateResult.Success(ticket);
        }




        public static string GetUserIdFromCertificate(string commonName, string certificateNamePrefix)
        {
            return  commonName.Substring(commonName.IndexOf(certificateNamePrefix) + certificateNamePrefix.Length,BankCodeLength);
        }
    }
}
