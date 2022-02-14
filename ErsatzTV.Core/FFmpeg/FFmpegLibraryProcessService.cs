using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.FFmpeg;
using ErsatzTV.FFmpeg.Format;
using ErsatzTV.FFmpeg.OutputFormat;
using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;
using MediaStream = ErsatzTV.Core.Domain.MediaStream;

namespace ErsatzTV.Core.FFmpeg;

public class FFmpegLibraryProcessService : IFFmpegProcessService
{
    private readonly FFmpegProcessService _ffmpegProcessService;
    private readonly FFmpegPlaybackSettingsCalculator _playbackSettingsCalculator;
    private readonly IFFmpegStreamSelector _ffmpegStreamSelector;
    private readonly ILogger<FFmpegLibraryProcessService> _logger;

    public FFmpegLibraryProcessService(
        FFmpegProcessService ffmpegProcessService,
        FFmpegPlaybackSettingsCalculator playbackSettingsCalculator,
        IFFmpegStreamSelector ffmpegStreamSelector,
        ILogger<FFmpegLibraryProcessService> logger)
    {
        _ffmpegProcessService = ffmpegProcessService;
        _playbackSettingsCalculator = playbackSettingsCalculator;
        _ffmpegStreamSelector = ffmpegStreamSelector;
        _logger = logger;
    }

    public async Task<Process> ForPlayoutItem(
        string ffmpegPath,
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
        Option<int> targetFramerate)
    {
        MediaStream videoStream = await _ffmpegStreamSelector.SelectVideoStream(channel, videoVersion);
        Option<MediaStream> maybeAudioStream = await _ffmpegStreamSelector.SelectAudioStream(channel, audioVersion);

        FFmpegPlaybackSettings playbackSettings = _playbackSettingsCalculator.CalculateSettings(
            channel.StreamingMode,
            channel.FFmpegProfile,
            videoVersion,
            videoStream,
            maybeAudioStream,
            start,
            now,
            inPoint,
            outPoint,
            hlsRealtime,
            targetFramerate);

        var inputFiles = new List<InputFile>
        {
            new(
                videoVersion.MediaFiles.Head().Path,
                new List<ErsatzTV.FFmpeg.MediaStream>
                {
                    new VideoStream(
                        videoStream.Index,
                        videoStream.Codec,
                        Some(AvailablePixelFormats.ForPixelFormat(videoStream.PixelFormat)),
                        new FrameSize(videoVersion.Width, videoVersion.Height),
                        videoVersion.RFrameRate)
                })
        };

        foreach (MediaStream audioStream in maybeAudioStream)
        {
            inputFiles.Head().Streams
                .Add(new AudioStream(audioStream.Index, audioStream.Codec, audioStream.Channels));
        }

        // TODO: need formats for these codecs
        string videoFormat = channel.FFmpegProfile.VideoCodec switch
        {
            "libx265" or "hevc_nvenc" or "hevc_qsv" or "hevc_vaapi" => VideoFormat.Hevc,
            "libx264" or "h264_nvenc" or "h264_qsv" or "h264_vaapi" => VideoFormat.H264,
            "mpeg2video" => VideoFormat.Mpeg2Video,
            _ => throw new ArgumentOutOfRangeException($"unexpected video codec {channel.FFmpegProfile.VideoCodec}")
        };

        HardwareAccelerationMode hwAccel = playbackSettings.HardwareAcceleration switch
        {
            HardwareAccelerationKind.Nvenc => HardwareAccelerationMode.Nvenc,
            HardwareAccelerationKind.Qsv => HardwareAccelerationMode.Qsv,
            HardwareAccelerationKind.Vaapi => HardwareAccelerationMode.Vaapi,
            HardwareAccelerationKind.VideoToolbox => HardwareAccelerationMode.VideoToolbox,
            _ => HardwareAccelerationMode.None
        };

        OutputFormatKind outputFormat = channel.StreamingMode == StreamingMode.HttpLiveStreamingSegmenter
            ? OutputFormatKind.Hls
            : OutputFormatKind.MpegTs;

        var desiredState = new FrameState(
            hwAccel,
            playbackSettings.RealtimeOutput,
            false,
            playbackSettings.StreamSeek,
            finish - now,
            videoFormat,
            Some(AvailablePixelFormats.ForPixelFormat(videoStream.PixelFormat)),
            await playbackSettings.ScaledSize.Map(ss => new FrameSize(ss.Width, ss.Height))
                .IfNoneAsync(new FrameSize(videoVersion.Width, videoVersion.Height)),
            new FrameSize(channel.FFmpegProfile.Resolution.Width, channel.FFmpegProfile.Resolution.Height),
            playbackSettings.FrameRate,
            playbackSettings.VideoBitrate,
            playbackSettings.VideoBufferSize,
            playbackSettings.VideoTrackTimeScale,
            playbackSettings.Deinterlace,
            channel.FFmpegProfile.AudioCodec,
            channel.FFmpegProfile.AudioChannels,
            playbackSettings.AudioBitrate,
            playbackSettings.AudioBufferSize,
            playbackSettings.AudioSampleRate,
            videoPath == audioPath ? playbackSettings.AudioDuration : Option<TimeSpan>.None,
            playbackSettings.NormalizeLoudness,
            channel.StreamingMode != StreamingMode.HttpLiveStreamingDirect,
            "ErsatzTV",
            channel.Name,
            maybeAudioStream.Map(s => Optional(s.Language)).Flatten(),
            outputFormat,
            Path.Combine(FileSystemLayout.TranscodeFolder, channel.Number, "live.m3u8"),
            Path.Combine(FileSystemLayout.TranscodeFolder, channel.Number, "live%06d.ts"),
            ptsOffset);

        return GetProcess(ffmpegPath, inputFiles, desiredState);
    }

    public Task<Process> ForError(
        string ffmpegPath,
        Channel channel,
        Option<TimeSpan> duration,
        string errorMessage,
        bool hlsRealtime,
        long ptsOffset) =>
        _ffmpegProcessService.ForError(ffmpegPath, channel, duration, errorMessage, hlsRealtime, ptsOffset);

    public Process ConcatChannel(string ffmpegPath, bool saveReports, Channel channel, string scheme, string host)
    {
        var resolution = new FrameSize(channel.FFmpegProfile.Resolution.Width, channel.FFmpegProfile.Resolution.Height);
        var desiredState = FrameState.Concat(channel.Name, resolution);

        var inputFiles = new List<InputFile>
        {
            new ConcatInputFile($"http://localhost:{Settings.ListenPort}/ffmpeg/concat/{channel.Number}", resolution)
        };

        return GetProcess(ffmpegPath, inputFiles, desiredState);
    }

    public Process WrapSegmenter(string ffmpegPath, bool saveReports, Channel channel, string scheme, string host) =>
        _ffmpegProcessService.WrapSegmenter(ffmpegPath, saveReports, channel, scheme, host);

    public Process ConvertToPng(string ffmpegPath, string inputFile, string outputFile) =>
        _ffmpegProcessService.ConvertToPng(ffmpegPath, inputFile, outputFile);

    public Process ExtractAttachedPicAsPng(string ffmpegPath, string inputFile, int streamIndex, string outputFile) =>
        _ffmpegProcessService.ExtractAttachedPicAsPng(ffmpegPath, inputFile, streamIndex, outputFile);

    public Task<Either<BaseError, string>> GenerateSongImage(
        string ffmpegPath,
        Option<string> subtitleFile,
        Channel channel,
        Option<ChannelWatermark> globalWatermark,
        MediaVersion videoVersion,
        string videoPath,
        bool boxBlur,
        Option<string> watermarkPath,
        ChannelWatermarkLocation watermarkLocation,
        int horizontalMarginPercent,
        int verticalMarginPercent,
        int watermarkWidthPercent) =>
        _ffmpegProcessService.GenerateSongImage(
            ffmpegPath,
            subtitleFile,
            channel,
            globalWatermark,
            videoVersion,
            videoPath,
            boxBlur,
            watermarkPath,
            watermarkLocation,
            horizontalMarginPercent,
            verticalMarginPercent,
            watermarkWidthPercent);

    private Process GetProcess(string ffmpegPath, IList<InputFile> inputFiles, FrameState desiredState)
    {
        var pipelineBuilder = new PipelineBuilder(inputFiles, _logger);

        IList<IPipelineStep> pipelineSteps = pipelineBuilder.Build(desiredState);

        IList<string> arguments = CommandGenerator.GenerateArguments(inputFiles, pipelineSteps);

        var startInfo = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            RedirectStandardOutput = true,
            RedirectStandardError = false,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8
        };

        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return new Process
        {
            StartInfo = startInfo
        };
    }
}
