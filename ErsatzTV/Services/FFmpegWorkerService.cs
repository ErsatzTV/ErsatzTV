using System.Threading.Channels;
using ErsatzTV.Application;
using ErsatzTV.Application.Streaming;
using ErsatzTV.Core.Interfaces.FFmpeg;

namespace ErsatzTV.Services;

public class FFmpegWorkerService : BackgroundService
{
    private readonly ChannelReader<IFFmpegWorkerRequest> _channel;
    private readonly ILogger<FFmpegWorkerService> _logger;
    private readonly IFFmpegSegmenterService _ffmpegSegmenterService;
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
        _logger.LogInformation("FFmpeg worker service started");

        await foreach (IFFmpegWorkerRequest request in _channel.ReadAllAsync(cancellationToken))
        {
            try
            {
                using IServiceScope scope = _serviceScopeFactory.CreateScope();
                // IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

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
            }
        }
    }
}