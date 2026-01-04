using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Notifications.Contracts;

namespace Notifications.Api.Controllers
{
    [ApiController]
    [Route("api/test-dispatch")]
    public sealed class TestDispatchController : ControllerBase
    {
        private readonly ISendEndpointProvider _sendEndpointProvider;

        public TestDispatchController(ISendEndpointProvider sendEndpointProvider)
        => _sendEndpointProvider = sendEndpointProvider;

        [HttpPost("email")]
        public async Task<IActionResult> Email()
        {
            var msg = new DispatchNotification(
                Guid.NewGuid(),
                ChannelTypeDto.Email,
                "test@example.com",
                "Test EMAIL",
                "Hello",
                DateTime.UtcNow);

            var endpoint = await _sendEndpointProvider.GetSendEndpoint(new Uri("queue:email.dispatch"));
            await endpoint.Send(msg);

            return Ok(new { msg.NotificationId });
        }

        [HttpPost("push")]
        public async Task<IActionResult> Push()
        {
            var msg = new DispatchNotification(
                Guid.NewGuid(),
                ChannelTypeDto.Push,
                "device-123",
                "Test PUSH",
                "Hello",
                DateTime.UtcNow);

            var endpoint = await _sendEndpointProvider.GetSendEndpoint(new Uri("queue:push.dispatch"));
            await endpoint.Send(msg);

            return Ok(new { msg.NotificationId });
        }
    }
}
