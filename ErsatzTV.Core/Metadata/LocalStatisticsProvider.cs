using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.Metadata
{
    public class LocalStatisticsProvider : ILocalStatisticsProvider
    {
        private readonly ILocalFileSystem _localFileSystem;
        private readonly ILogger<LocalStatisticsProvider> _logger;
        private readonly IMetadataRepository _metadataRepository;

        public LocalStatisticsProvider(
            IMetadataRepository metadataRepository,
            ILocalFileSystem localFileSystem,
            ILogger<LocalStatisticsProvider> logger)
        {
            _metadataRepository = metadataRepository;
            _localFileSystem = localFileSystem;
            _logger = logger;
        }

        public async Task<Either<BaseError, bool>> RefreshStatistics(string ffprobePath, MediaItem mediaItem)
        {
            try
            {
                string filePath = mediaItem switch
                {
                    Movie m => m.MediaVersions.Head().MediaFiles.Head().Path,
                    Episode e => e.MediaVersions.Head().MediaFiles.Head().Path,
                    MusicVideo mv => mv.MediaVersions.Head().MediaFiles.Head().Path,
                    _ => throw new ArgumentOutOfRangeException(nameof(mediaItem))
                };

                return await RefreshStatistics(ffprobePath, mediaItem, filePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to refresh statistics for media item {Id}", mediaItem.Id);
                return BaseError.New(ex.Message);
            }
        }

        public async Task<Either<BaseError, bool>> RefreshStatistics(
            string ffprobePath,
            MediaItem mediaItem,
            string mediaItemPath)
        {
            try
            {
                Either<BaseError, FFprobe> maybeProbe = await GetProbeOutput(ffprobePath, mediaItemPath);
                return await maybeProbe.Match(
                    async ffprobe =>
                    {
                        MediaVersion version = ProjectToMediaVersion(mediaItemPath, ffprobe);
                        bool result = await ApplyVersionUpdate(mediaItem, version, mediaItemPath);
                        return Right<BaseError, bool>(result);
                    },
                    error => Task.FromResult(Left<BaseError, bool>(error)));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to refresh statistics for media item {Id}", mediaItem.Id);
                return BaseError.New(ex.Message);
            }
        }

        private async Task<bool> ApplyVersionUpdate(MediaItem mediaItem, MediaVersion version, string filePath)
        {
            MediaVersion mediaItemVersion = mediaItem switch
            {
                Movie m => m.MediaVersions.Head(),
                Episode e => e.MediaVersions.Head(),
                MusicVideo mv => mv.MediaVersions.Head(),
                _ => throw new ArgumentOutOfRangeException(nameof(mediaItem))
            };

            bool durationChange = mediaItemVersion.Duration != version.Duration;

            version.DateUpdated = _localFileSystem.GetLastWriteTime(filePath);

            return await _metadataRepository.UpdateLocalStatistics(mediaItemVersion.Id, version) && durationChange;
        }

        private Task<Either<BaseError, FFprobe>> GetProbeOutput(string ffprobePath, string filePath)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = ffprobePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            startInfo.ArgumentList.Add("-v");
            startInfo.ArgumentList.Add("quiet");
            startInfo.ArgumentList.Add("-print_format");
            startInfo.ArgumentList.Add("json");
            startInfo.ArgumentList.Add("-show_format");
            startInfo.ArgumentList.Add("-show_streams");
            startInfo.ArgumentList.Add("-i");
            startInfo.ArgumentList.Add(filePath);

            var probe = new Process
            {
                StartInfo = startInfo
            };

            probe.Start();
            return probe.StandardOutput.ReadToEndAsync().MapAsync<string, Either<BaseError, FFprobe>>(
                async output =>
                {
                    await probe.WaitForExitAsync();
                    return probe.ExitCode == 0
                        ? JsonConvert.DeserializeObject<FFprobe>(output)
                        : BaseError.New($"FFprobe at {ffprobePath} exited with code {probe.ExitCode}");
                });
        }

        private MediaVersion ProjectToMediaVersion(string path, FFprobe probeOutput) =>
            Optional(probeOutput)
                .Filter(json => json?.format != null && json.streams != null)
                .ToValidation<BaseError>("Unable to parse ffprobe output")
                .ToEither<FFprobe>()
                .Match(
                    json =>
                    {
                        var version = new MediaVersion
                            { Name = "Main", DateAdded = DateTime.UtcNow, Streams = new List<MediaStream>() };

                        if (double.TryParse(json.format.duration, out double duration))
                        {
                            var seconds = TimeSpan.FromSeconds(duration);
                            version.Duration = seconds;
                        }
                        else
                        {
                            _logger.LogWarning(
                                "Media item at {Path} has a missing or invalid duration {Duration} and will cause scheduling issues",
                                path,
                                json.format.duration);
                        }

                        foreach (FFprobeStream audioStream in json.streams.Filter(s => s.codec_type == "audio"))
                        {
                            var stream = new MediaStream
                            {
                                MediaVersionId = version.Id,
                                MediaStreamKind = MediaStreamKind.Audio,
                                Index = audioStream.index,
                                Codec = audioStream.codec_name,
                                Profile = (audioStream.profile ?? string.Empty).ToLowerInvariant(),
                                Channels = audioStream.channels
                            };

                            if (audioStream.disposition is not null)
                            {
                                stream.Default = audioStream.disposition.@default == 1;
                                stream.Forced = audioStream.disposition.forced == 1;
                            }

                            if (audioStream.tags is not null)
                            {
                                stream.Language = audioStream.tags.language;
                                stream.Title = audioStream.tags.title;
                            }

                            version.Streams.Add(stream);
                        }

                        FFprobeStream videoStream = json.streams.FirstOrDefault(s => s.codec_type == "video");
                        if (videoStream != null)
                        {
                            version.SampleAspectRatio = string.IsNullOrWhiteSpace(videoStream.sample_aspect_ratio)
                                ? "1:1"
                                : videoStream.sample_aspect_ratio;
                            version.DisplayAspectRatio = videoStream.display_aspect_ratio;
                            version.Width = videoStream.width;
                            version.Height = videoStream.height;
                            version.VideoScanKind = ScanKindFromFieldOrder(videoStream.field_order);

                            var stream = new MediaStream
                            {
                                MediaVersionId = version.Id,
                                MediaStreamKind = MediaStreamKind.Video,
                                Index = videoStream.index,
                                Codec = videoStream.codec_name,
                                Profile = (videoStream.profile ?? string.Empty).ToLowerInvariant()
                            };

                            if (videoStream.disposition is not null)
                            {
                                stream.Default = videoStream.disposition.@default == 1;
                                stream.Forced = videoStream.disposition.forced == 1;
                            }

                            version.Streams.Add(stream);
                        }

                        foreach (FFprobeStream subtitleStream in json.streams.Filter(s => s.codec_type == "subtitle"))
                        {
                            var stream = new MediaStream
                            {
                                MediaVersionId = version.Id,
                                MediaStreamKind = MediaStreamKind.Subtitle,
                                Index = subtitleStream.index,
                                Codec = subtitleStream.codec_name
                            };

                            if (subtitleStream.disposition is not null)
                            {
                                stream.Default = subtitleStream.disposition.@default == 1;
                                stream.Forced = subtitleStream.disposition.forced == 1;
                            }

                            if (subtitleStream.tags is not null)
                            {
                                stream.Language = subtitleStream.tags.language;
                            }

                            version.Streams.Add(stream);
                        }

                        return version;
                    },
                    _ => new MediaVersion
                        { Name = "Main", DateAdded = DateTime.UtcNow, Streams = new List<MediaStream>() });

        private VideoScanKind ScanKindFromFieldOrder(string fieldOrder) =>
            fieldOrder?.ToLowerInvariant() switch
            {
                var x when x == "tt" || x == "bb" || x == "tb" || x == "bt" => VideoScanKind.Interlaced,
                "progressive" => VideoScanKind.Progressive,
                _ => VideoScanKind.Unknown
            };

        // ReSharper disable InconsistentNaming
        public record FFprobe(FFprobeFormat format, List<FFprobeStream> streams);

        public record FFprobeFormat(string duration);

        public record FFprobeDisposition(int @default, int forced);

        public record FFProbeTags(string language, string title);

        public record FFprobeStream(
            int index,
            string codec_name,
            string profile,
            string codec_type,
            int channels,
            int width,
            int height,
            string sample_aspect_ratio,
            string display_aspect_ratio,
            string field_order,
            string r_frame_rate,
            FFprobeDisposition disposition,
            FFProbeTags tags);
        // ReSharper restore InconsistentNaming
    }
}
