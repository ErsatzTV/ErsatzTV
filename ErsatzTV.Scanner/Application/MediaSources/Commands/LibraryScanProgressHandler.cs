using ErsatzTV.Core.Metadata;
using Newtonsoft.Json;

namespace ErsatzTV.Scanner.Application.MediaSources;

public class LibraryScanProgressHandler : INotificationHandler<LibraryScanProgress>
{
    public Task Handle(LibraryScanProgress notification, CancellationToken cancellationToken)
    {
        // dump progress to stdout for main process to read
        string json = JsonConvert.SerializeObject(notification);
        Console.WriteLine(json);
        return Task.CompletedTask;
    }
}
