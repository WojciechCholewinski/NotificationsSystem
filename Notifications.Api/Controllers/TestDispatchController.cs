using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Notifications.Contracts;

namespace Notifications.Api.Controllers
{
    [ApiController]
    [Route("api/test-dispatch")]
    public sealed class TestDispatchController : ControllerBase
    {
        private readonly IPublishEndpoint _publish;

        public TestDispatchController(IPublishEndpoint publish) => _publish = publish;

        [HttpPost("email")]
        public async Task<IActionResult> Email()
        {
            var msg = new DispatchNotification(
                Guid.NewGuid(),
                ChannelType.Email,
                "test@example.com",
                "Test",
                "Hello",
                DateTime.UtcNow);

            await _publish.Publish(msg);
            return Ok(new { msg.NotificationId });
        }

        [HttpPost("push")]
        public async Task<IActionResult> Push()
        {
            var msg = new DispatchNotification(
                Guid.NewGuid(),
                ChannelType.Push,
                "device-123",
                "Test",
                "Hello",
                DateTime.UtcNow);

            await _publish.Publish(msg);
            return Ok(new { msg.NotificationId });
        }
    }
}
