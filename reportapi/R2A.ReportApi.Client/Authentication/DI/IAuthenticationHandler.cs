using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace R2A.ReportApi.Client.Authentication.DI
{
    public interface IAuthenticationHandler
    {
        Task<AuthenticateResult> HandleAuthenticateAsync(HttpContext context);
    }
}
