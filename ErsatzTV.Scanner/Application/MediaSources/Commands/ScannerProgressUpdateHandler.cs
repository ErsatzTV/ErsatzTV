using ErsatzTV.Core.MediaSources;
using Newtonsoft.Json;

namespace ErsatzTV.Scanner.Application.MediaSources;

public class ScannerProgressUpdateHandler : INotificationHandler<ScannerProgressUpdate>
{
    public Task Handle(ScannerProgressUpdate notification, CancellationToken cancellationToken)
    {
        // dump progress to stdout for main process to read
        string json = JsonConvert.SerializeObject(notification);
        Console.WriteLine(json);
        return Task.CompletedTask;
    }
}
