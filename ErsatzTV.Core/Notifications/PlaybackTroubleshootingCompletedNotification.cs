using MediatR;

namespace ErsatzTV.Core.Notifications;

public record PlaybackTroubleshootingCompletedNotification(int ExitCode) : INotification;
