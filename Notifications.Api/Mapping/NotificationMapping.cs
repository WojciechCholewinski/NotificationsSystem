using Notifications.Contracts;
using Notifications.Domain;

namespace Notifications.Api.Mapping;

public static class NotificationMapping
{
    public static ChannelType ToDomain(this ChannelTypeDto dto) => dto switch
    {
        ChannelTypeDto.Email => ChannelType.Email,
        ChannelTypeDto.Push => ChannelType.Push,
        _ => throw new ArgumentOutOfRangeException(nameof(dto), dto, null)
    };

    public static ChannelTypeDto ToDto(this ChannelType domain) => domain switch
    {
        ChannelType.Email => ChannelTypeDto.Email,
        ChannelType.Push => ChannelTypeDto.Push,
        _ => throw new ArgumentOutOfRangeException(nameof(domain), domain, null)
    };

    public static NotificationStatusDto ToDto(this NotificationStatus domain) => domain switch
    {
        NotificationStatus.Created => NotificationStatusDto.Created,
        NotificationStatus.Scheduled => NotificationStatusDto.Scheduled,
        NotificationStatus.Sending => NotificationStatusDto.Sending,
        NotificationStatus.Sent => NotificationStatusDto.Sent,
        NotificationStatus.Failed => NotificationStatusDto.Failed,
        NotificationStatus.Canceled => NotificationStatusDto.Cancelled,
        _ => throw new ArgumentOutOfRangeException(nameof(domain), domain, null)
    };
}
