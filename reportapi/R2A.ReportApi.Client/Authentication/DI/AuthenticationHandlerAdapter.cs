using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using IAuthenticationHandler = R2A.ReportApi.Client.Authentication.DI.IAuthenticationHandler;

namespace R2A.ReportApi.Client.Authentication.DI
{
    public class AuthenticationHandlerAdapter<T> : AuthenticationHandler<AuthenticationHandlerAdapterOptions<T>> where T : IAuthenticationHandler
    {
        public AuthenticationHandlerAdapter(IOptionsMonitor<AuthenticationHandlerAdapterOptions<T>> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) 
            : base(options, logger, encoder, clock)
        {
        }


        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            IAuthenticationHandler handler = (IAuthenticationHandler) Options.Container.GetInstance(typeof(T));
            return await handler.HandleAuthenticateAsync(Context);
        }

    }
}
