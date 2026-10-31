using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using PulseAlert.Api.Filters;
using PulseAlert.Core.Telemetry;

namespace PulseAlert.UnitTests.Api;

public class ApiKeyFilterTests
{
    private const string ConfiguredKey = "test-key";

    private readonly ApiKeyFilter _filter = new(Options.Create(new TelemetryOptions { ApiKey = ConfiguredKey }));

    private static AuthorizationFilterContext ContextWithKey(string? key)
    {
        var httpContext = new DefaultHttpContext();
        if (key is not null)
        {
            httpContext.Request.Headers[ApiKeyFilter.HeaderName] = key;
        }

        return new AuthorizationFilterContext(
            new ActionContext(httpContext, new RouteData(), new ActionDescriptor()),
            []);
    }

    [Fact]
    public void CorrectKey_IsAllowedThrough()
    {
        var context = ContextWithKey(ConfiguredKey);

        _filter.OnAuthorization(context);

        Assert.Null(context.Result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("wrong-key")]
    [InlineData("TEST-KEY")]
    [InlineData("test-key ")]
    public void MissingOrWrongKey_Returns401(string? key)
    {
        var context = ContextWithKey(key);

        _filter.OnAuthorization(context);

        Assert.IsType<UnauthorizedResult>(context.Result);
    }
}
