using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.FFmpeg;
using ErsatzTV.FFmpeg.Environment;
using ErsatzTV.FFmpeg.Format;
using ErsatzTV.FFmpeg.OutputFormat;
using ErsatzTV.FFmpeg.State;
using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;
using FFmpegState = ErsatzTV.FFmpeg.FFmpegState;
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
        
        var audioState = new AudioState(
            finish - now,
            playbackSettings.AudioCodec,
            playbackSettings.AudioChannels,
            playbackSettings.AudioBitrate,
            playbackSettings.AudioBufferSize,
            playbackSettings.AudioSampleRate,
            videoPath == audioPath ? playbackSettings.AudioDuration : Option<TimeSpan>.None,
            playbackSettings.NormalizeLoudness);

        var ffmpegVideoStream = new VideoStream(
            videoStream.Index,
            videoStream.Codec,
            AvailablePixelFormats.ForPixelFormat(videoStream.PixelFormat, _logger),
            new FrameSize(videoVersion.Width, videoVersion.Height),
            videoVersion.RFrameRate,
            videoPath != audioPath); // still image when paths are different

        var videoInputFile = new VideoInputFile(videoPath, new List<VideoStream> { ffmpegVideoStream });

        Option<AudioInputFile> audioInputFile = maybeAudioStream.Map(
            audioStream =>
            {
                var ffmpegAudioStream = new AudioStream(audioStream.Index, audioStream.Codec, audioStream.Channels);
                return new AudioInputFile(audioPath, new List<AudioStream> { ffmpegAudioStream }, audioState);
            });

        // TODO: need formats for these codecs
        string videoFormat = playbackSettings.VideoCodec switch
        {
            "libx265" or "hevc_nvenc" or "hevc_qsv" or "hevc_vaapi" or "hevc_videotoolbox" => VideoFormat.Hevc,
            "libx264" or "h264_nvenc" or "h264_qsv" or "h264_vaapi" or "h264_videotoolbox" => VideoFormat.H264,
            "mpeg2video" => VideoFormat.Mpeg2Video,
            "copy" => VideoFormat.Copy,
            _ => throw new ArgumentOutOfRangeException($"unexpected video codec {playbackSettings.VideoCodec}")
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

        Option<string> hlsPlaylistPath = outputFormat == OutputFormatKind.Hls
            ? Path.Combine(FileSystemLayout.TranscodeFolder, channel.Number, "live.m3u8")
            : Option<string>.None;

        Option<string> hlsSegmentTemplate = outputFormat == OutputFormatKind.Hls
            ? Path.Combine(FileSystemLayout.TranscodeFolder, channel.Number, "live%06d.ts")
            : Option<string>.None;

        // normalize songs to yuv420p
        Option<IPixelFormat> desiredPixelFormat =
            videoPath == audioPath ? ffmpegVideoStream.PixelFormat : new PixelFormatYuv420P();

        var desiredState = new FrameState(
            playbackSettings.RealtimeOutput,
            false,
            videoFormat,
            desiredPixelFormat,
            await playbackSettings.ScaledSize.Map(ss => new FrameSize(ss.Width, ss.Height))
                .IfNoneAsync(new FrameSize(videoVersion.Width, videoVersion.Height)),
            new FrameSize(channel.FFmpegProfile.Resolution.Width, channel.FFmpegProfile.Resolution.Height),
            playbackSettings.FrameRate,
            playbackSettings.VideoBitrate,
            playbackSettings.VideoBufferSize,
            playbackSettings.VideoTrackTimeScale,
            playbackSettings.Deinterlace);

        var ffmpegState = new FFmpegState(
            saveReports,
            hwAccel,
            VaapiDriverName(hwAccel, vaapiDriver),
            VaapiDeviceName(hwAccel, vaapiDevice),
            playbackSettings.StreamSeek,
            finish - now,
            channel.StreamingMode != StreamingMode.HttpLiveStreamingDirect,
            "ErsatzTV",
            channel.Name,
            maybeAudioStream.Map(s => Optional(s.Language)).Flatten(),
            outputFormat,
            hlsPlaylistPath,
            hlsSegmentTemplate,
            ptsOffset);

        _logger.LogDebug("FFmpeg desired state {FrameState}", desiredState);
        
        var pipelineBuilder = new PipelineBuilder(
            videoInputFile,
            audioInputFile,
            FileSystemLayout.FFmpegReportsFolder,
            _logger);

        FFmpegPipeline pipeline = pipelineBuilder.Build(ffmpegState, desiredState);

        return GetProcess(ffmpegPath, videoInputFile, audioInputFile, None, pipeline);
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

        var concatInputFile = new ConcatInputFile(
            $"http://localhost:{Settings.ListenPort}/ffmpeg/concat/{channel.Number}",
            resolution);

        var pipelineBuilder = new PipelineBuilder(None, None, FileSystemLayout.FFmpegReportsFolder, _logger);

        FFmpegPipeline pipeline = pipelineBuilder.Concat(
            concatInputFile,
            FFmpegState.Concat(saveReports, channel.Name));

        return GetProcess(ffmpegPath, None, None, concatInputFile, pipeline);
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

    private Process GetProcess(
        string ffmpegPath,
        Option<VideoInputFile> videoInputFile,
        Option<AudioInputFile> audioInputFile,
        Option<ConcatInputFile> concatInputFile,
        FFmpegPipeline pipeline)
    {
        IEnumerable<string> loggedSteps = pipeline.PipelineSteps.Map(ps => ps.GetType().Name);
        IEnumerable<string> loggedVideoFilters = pipeline.VideoFilterSteps.Map(vf => vf.GetType().Name);
        IEnumerable<string> loggedAudioFilters = pipeline.AudioFilterSteps.Map(af => af.GetType().Name);

        _logger.LogDebug(
            "FFmpeg pipeline {PipelineSteps}, {AudioFilters}, {VideoFilters}",
            loggedSteps,
            loggedAudioFilters,
            loggedVideoFilters
        );

        IList<EnvironmentVariable> environmentVariables =
            CommandGenerator.GenerateEnvironmentVariables(pipeline.PipelineSteps);
        IList<string> arguments = CommandGenerator.GenerateArguments(
            videoInputFile,
            audioInputFile,
            concatInputFile,
            pipeline.PipelineSteps);

        var startInfo = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            RedirectStandardOutput = true,
            RedirectStandardError = false,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8
        };

        if (environmentVariables.Any())
        {
            _logger.LogDebug("FFmpeg environment variables {EnvVars}", environmentVariables);
        }

        foreach ((string key, string value) in environmentVariables)
        {
            startInfo.EnvironmentVariables[key] = value;
        }

        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return new Process
        {
            StartInfo = startInfo
        };
    }

    private static Option<string> VaapiDriverName(HardwareAccelerationMode accelerationMode, VaapiDriver driver)
    {
        if (accelerationMode == HardwareAccelerationMode.Vaapi)
        {
            switch (driver)
            {
                case VaapiDriver.i965:
                    return "i965";
                case VaapiDriver.iHD:
                    return "iHD";
                case VaapiDriver.RadeonSI:
                    return "radeonsi";
            }
        }
        
        return Option<string>.None;
    }

    private static Option<string> VaapiDeviceName(HardwareAccelerationMode accelerationMode, string vaapiDevice)
    {
        return accelerationMode == HardwareAccelerationMode.Vaapi ? vaapiDevice : Option<string>.None;
    }
}
