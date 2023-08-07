using System.Threading.Channels;
using Bugsnag;
using ErsatzTV.Application;
using ErsatzTV.Application.Streaming;
using ErsatzTV.Core.Interfaces.FFmpeg;

namespace ErsatzTV.Services;

public class FFmpegWorkerService : BackgroundService
{
    private readonly ChannelReader<IFFmpegWorkerRequest> _channel;
    private readonly IFFmpegSegmenterService _ffmpegSegmenterService;
    private readonly ILogger<FFmpegWorkerService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public FFmpegWorkerService(
        ChannelReader<IFFmpegWorkerRequest> channel,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<FFmpegWorkerService> logger,
        IFFmpegSegmenterService ffmpegSegmenterService)
    {
        _channel = channel;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _ffmpegSegmenterService = ffmpegSegmenterService;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await Task.Yield();

        try
        {
            _logger.LogInformation("FFmpeg worker service started");

            await foreach (IFFmpegWorkerRequest request in _channel.ReadAllAsync(cancellationToken))
            {
                using IServiceScope scope = _serviceScopeFactory.CreateScope();

                try
                {
                    switch (request)
                    {
                        case TouchFFmpegSession touchFFmpegSession:
                            foreach (DirectoryInfo parent in Optional(Directory.GetParent(touchFFmpegSession.Path)))
                            {
                                _ffmpegSegmenterService.TouchChannel(parent.Name);
                            }

                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to handle ffmpeg worker request");

                    try
                    {
                        IClient client = scope.ServiceProvider.GetRequiredService<IClient>();
                        client.Notify(ex);
                    }
                    catch (Exception)
                    {
                        // do nothing
                    }
                }
            }
        }
        catch (Exception ex) when (ex is TaskCanceledException or OperationCanceledException)
        {
            _logger.LogInformation("FFmpeg worker service shutting down");
        }
    }
}
