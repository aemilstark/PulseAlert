using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PulseAlert.Api.Controllers;
using PulseAlert.Core.Telemetry;

namespace PulseAlert.UnitTests.Api;

public class TelemetryControllerTests
{
    private readonly Mock<ITelemetryQueue> _queue = new();

    private TelemetryController CreateController() => new(_queue.Object)
    {
        ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
    };

    [Fact]
    public void QueuedPing_Returns202()
    {
        _queue.Setup(q => q.TryEnqueue(It.IsAny<IncomingPing>())).Returns(true);

        var result = CreateController().Post(TelemetryPingRequestValidationTests.ValidRequest());

        Assert.IsType<AcceptedResult>(result);
    }

    [Fact]
    public void Post_QueuesTheConvertedPing()
    {
        var request = TelemetryPingRequestValidationTests.ValidRequest();
        _queue.Setup(q => q.TryEnqueue(It.IsAny<IncomingPing>())).Returns(true);

        CreateController().Post(request);

        _queue.Verify(q => q.TryEnqueue(request.ToIncomingPing()), Times.Once);
    }

    [Fact]
    public void FullQueue_Returns503WithRetryAfter()
    {
        _queue.Setup(q => q.TryEnqueue(It.IsAny<IncomingPing>())).Returns(false);
        var controller = CreateController();

        var result = controller.Post(TelemetryPingRequestValidationTests.ValidRequest());

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, objectResult.StatusCode);
        Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(
            TelemetryController.RetryAfterSeconds.ToString(),
            controller.Response.Headers.RetryAfter.ToString());
    }
}
