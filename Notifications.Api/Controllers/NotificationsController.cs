using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Notifications.Api.Mapping;
using Notifications.Contracts;
using Notifications.Domain;
using Notifications.Infrastructure.Persistence;

namespace Notifications.Api.Controllers;

[ApiController]
[Route("api/notifications")]
public sealed class NotificationsController : ControllerBase
{
    private readonly NotificationsDbContext _db;

    public NotificationsController(NotificationsDbContext db) => _db = db;

    [HttpPost]
    public async Task<ActionResult<CreateNotificationResponse>> Create(
        [FromBody] CreateNotificationRequest req,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Recipient))
            return BadRequest("Recipient is required.");
        if (string.IsNullOrWhiteSpace(req.RecipientTimeZone))
            return BadRequest("RecipientTimeZone is required.");
        if (string.IsNullOrWhiteSpace(req.Title))
            return BadRequest("Title is required.");
        if (string.IsNullOrWhiteSpace(req.Body))
            return BadRequest("Body is required.");

        TimeZoneInfo tz;
        try
        {
            // w kontenerze (Linux) używaj IANA, np. "Europe/Warsaw"
            tz = TimeZoneInfo.FindSystemTimeZoneById(req.RecipientTimeZone);
        }
        catch (TimeZoneNotFoundException)
        {
            return BadRequest($"Invalid time zone id: {req.RecipientTimeZone}");
        }
        catch (InvalidTimeZoneException)
        {
            return BadRequest($"Invalid time zone data: {req.RecipientTimeZone}");
        }

        // ScheduledAtLocal traktujemy jako "czas lokalny" użytkownika (bez offsetu)
        var localUnspecified = DateTime.SpecifyKind(req.ScheduledAtLocal, DateTimeKind.Unspecified);

        DateTime scheduledUtc;
        try
        {
            scheduledUtc = TimeZoneInfo.ConvertTimeToUtc(localUnspecified, tz);
        }
        catch (ArgumentException ex)
        {
            return BadRequest($"Cannot convert scheduled time to UTC: {ex.Message}");
        }

        Notification notification;
        try
        {
            // jeśli dodałem Priority do requestu - dopiać tu argument.
            notification = Notification.CreateScheduled(
                req.Channel.ToDomain(),
                req.Recipient,
                req.RecipientTimeZone,
                req.Title,
                req.Body,
                DateTime.SpecifyKind(scheduledUtc, DateTimeKind.Utc));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }

        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync(ct);

        return Ok(new CreateNotificationResponse(notification.Id, notification.ScheduledAtUtc));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<NotificationStatusResponse>> GetStatus(Guid id, CancellationToken ct)
    {
        var n = await _db.Notifications.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (n is null)
            return NotFound();

        return Ok(new NotificationStatusResponse(
            n.Id,
            n.Status.ToDto(),
            n.Channel.ToDto(),
            n.Recipient,
            n.Title,
            n.ScheduledAtUtc,
            n.CreatedAtUtc,
            n.UpdatedAtUtc,
            n.Attempts,
            n.LastError,
            n.SentAtUtc,
            n.CancelledAtUtc
        ));
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var n = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (n is null)
            return NotFound();

        try
        {
            n.Cancel();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/send-now")]
    public async Task<IActionResult> ForceSendNow(Guid id, CancellationToken ct)
    {
        var n = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (n is null)
            return NotFound();

        try
        {
            n.ForceSendNow(DateTime.UtcNow);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
