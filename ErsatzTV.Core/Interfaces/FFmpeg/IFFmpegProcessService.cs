using System.Diagnostics;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.FFmpeg.State;

namespace ErsatzTV.Core.Interfaces.FFmpeg;

public interface IFFmpegProcessService
{
    Task<Process> ForPlayoutItem(
        string ffmpegPath,
        string ffprobePath,
        bool saveReports,
        Channel channel,
        MediaVersion videoVersion,
        MediaVersion audioVersion,
        string videoPath,
        string audioPath,
        DateTimeOffset start,
        DateTimeOffset finish,
        DateTimeOffset now,
        Option<ChannelWatermark> globalWatermark,
        VaapiDriver vaapiDriver,
        string vaapiDevice,
        bool hlsRealtime,
        FillerKind fillerKind,
        TimeSpan inPoint,
        TimeSpan outPoint,
        long ptsOffset,
        Option<int> targetFramerate);

    Task<Process> ForError(
        string ffmpegPath,
        Channel channel,
        Option<TimeSpan> duration,
        string errorMessage,
        bool hlsRealtime,
        long ptsOffset);

    Process ConcatChannel(string ffmpegPath, bool saveReports, Channel channel, string scheme, string host);

    Process WrapSegmenter(string ffmpegPath, bool saveReports, Channel channel, string scheme, string host);

    Process ResizeImage(string ffmpegPath, string inputFile, string outputFile, int height);

    Process ConvertToPng(string ffmpegPath, string inputFile, string outputFile);

    Process ExtractAttachedPicAsPng(string ffmpegPath, string inputFile, int streamIndex, string outputFile);

    Task<Either<BaseError, string>> GenerateSongImage(
        string ffmpegPath,
        string ffprobePath,
        Option<string> subtitleFile,
        Channel channel,
        Option<ChannelWatermark> globalWatermark,
        MediaVersion videoVersion,
        string videoPath,
        bool boxBlur,
        Option<string> watermarkPath,
        WatermarkLocation watermarkLocation,
        int horizontalMarginPercent,
        int verticalMarginPercent,
        int watermarkWidthPercent,
        CancellationToken cancellationToken);
}