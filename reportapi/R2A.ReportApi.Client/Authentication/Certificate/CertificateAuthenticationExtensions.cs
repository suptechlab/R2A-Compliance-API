using Microsoft.AspNetCore.Authentication;
using R2A.ReportApi.Client.Authentication.DI;
using SimpleInjector;

namespace R2A.ReportApi.Client.Authentication.Certificate
{
    public static class CertificateAuthenticationExtensions
    {
        public static AuthenticationBuilder AddCertificateAuthentication(this AuthenticationBuilder builder, Container container)
        {
            return builder.AddAuthenticationHandler<CertificateAuthenticationHandler>(AuthenticationScheme.CertificateAuthentication,
                "Certificate Authentication", container);
        }
    }
}
