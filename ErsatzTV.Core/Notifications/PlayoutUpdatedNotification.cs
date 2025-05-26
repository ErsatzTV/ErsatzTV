using MediatR;

namespace ErsatzTV.Core.Notifications;

public record PlayoutUpdatedNotification(int PlayoutId, bool IsLocked) : INotification;
