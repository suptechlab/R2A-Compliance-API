using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using SimpleInjector;
using IAuthenticationHandler = R2A.ReportApi.Client.Authentication.DI.IAuthenticationHandler;

namespace R2A.ReportApi.Client.Authentication.DI
{
    public static class AuthenticationRegistrationExtensions
    {
        public static AuthenticationBuilder AddAuthenticationHandler<T>(this AuthenticationBuilder builder, string scheme, string displayName, Container container)
            where T : IAuthenticationHandler
        {
            builder.Services.Configure<AuthenticationOptions>(
                o => o.AddScheme(scheme, s =>
                {
                    s.HandlerType = typeof(AuthenticationHandlerAdapter<T>);
                    s.DisplayName = displayName;
                }));
            builder.Services.Configure<AuthenticationHandlerAdapterOptions<T>>(scheme, o => { o.Container = container; });
            builder.Services.AddTransient<AuthenticationHandlerAdapter<T>>();
            return builder;
        }
    }
}
