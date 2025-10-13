using MediatR;

namespace ErsatzTV.Core.Notifications;

public record PlaybackTroubleshootingCompletedNotification(
    int ExitCode,
    Option<Exception> MaybeException,
    Option<double> MaybeSpeed) : INotification;
