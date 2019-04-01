using Microsoft.AspNetCore.Authentication;
using SimpleInjector;
using IAuthenticationHandler = R2A.ReportApi.Client.Authentication.DI.IAuthenticationHandler;

namespace R2A.ReportApi.Client.Authentication.DI
{
    public class AuthenticationHandlerAdapterOptions<T> : AuthenticationSchemeOptions where T : IAuthenticationHandler
    {
        public Container Container { get; set; }
    }
}
