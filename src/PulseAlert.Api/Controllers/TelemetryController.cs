using Microsoft.AspNetCore.Mvc;
using PulseAlert.Api.Contracts;
using PulseAlert.Api.Filters;
using PulseAlert.Core.Telemetry;

namespace PulseAlert.Api.Controllers;

[ApiController]
[Route("api/telemetry")]
public class TelemetryController(ITelemetryQueue queue) : ControllerBase
{
    /// <summary>How long a sender should wait before retrying when the queue is full.</summary>
    public const int RetryAfterSeconds = 1;

    /// <summary>Webhook for device pings. Validates the ping and queues it for the Telemetry Worker.</summary>
    [HttpPost]
    [ServiceFilter<ApiKeyFilter>]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public IActionResult Post(TelemetryPingRequest request)
    {
        if (!queue.TryEnqueue(request.ToIncomingPing()))
        {
            Response.Headers.RetryAfter = RetryAfterSeconds.ToString();
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "The telemetry queue is full. Retry later.",
            });
        }

        return Accepted();
    }
}
