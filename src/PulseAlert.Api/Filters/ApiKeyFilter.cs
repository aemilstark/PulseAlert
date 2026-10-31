using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using PulseAlert.Core.Telemetry;

namespace PulseAlert.Api.Filters;

/// <summary>Rejects requests whose X-Api-Key header does not match the configured key.</summary>
/// <remarks>ASP.NET Core runs authorization filters run before model binding, so a bad key gets 401 even when the body is invalid.</remarks>
public sealed class ApiKeyFilter(IOptions<TelemetryOptions> options) : IAuthorizationFilter
{
    public const string HeaderName = "X-Api-Key";

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var providedKey = context.HttpContext.Request.Headers[HeaderName].ToString();

        if (!KeysMatch(providedKey, options.Value.ApiKey))
        {
            context.Result = new UnauthorizedResult();
        }
    }

    // Constant-time comparison, so response timing does not reveal how much of the key was right.
    private static bool KeysMatch(string provided, string expected) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(provided), Encoding.UTF8.GetBytes(expected));
}
