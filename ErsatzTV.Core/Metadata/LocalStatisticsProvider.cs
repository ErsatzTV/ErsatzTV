using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Extensions;
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
                string filePath = mediaItem.GetHeadVersion().MediaFiles.Head().Path;
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

        public async Task<Either<BaseError, Dictionary<string, string>>> GetFormatTags(
            string ffprobePath,
            MediaItem mediaItem)
        {
            try
            {
                string mediaItemPath = mediaItem.GetHeadVersion().MediaFiles.Head().Path;
                Either<BaseError, FFprobe> maybeProbe = await GetProbeOutput(ffprobePath, mediaItemPath);
                return maybeProbe.Match(
                    ffprobe =>
                    {
                        var result = new Dictionary<string, string>();
                        
                        if (!string.IsNullOrWhiteSpace(ffprobe?.format?.tags?.album))
                        {
                            result.Add(MetadataFormatTag.Album, ffprobe.format.tags.album);
                        }

                        if (!string.IsNullOrWhiteSpace(ffprobe?.format?.tags?.artist))
                        {
                            result.Add(MetadataFormatTag.Artist, ffprobe.format.tags.artist);
                        }

                        if (!string.IsNullOrWhiteSpace(ffprobe?.format?.tags?.date))
                        {
                            result.Add(MetadataFormatTag.Date, ffprobe.format.tags.date);
                        }

                        if (!string.IsNullOrWhiteSpace(ffprobe?.format?.tags?.genre))
                        {
                            result.Add(MetadataFormatTag.Genre, ffprobe.format.tags.genre);
                        }

                        if (!string.IsNullOrWhiteSpace(ffprobe?.format?.tags?.title))
                        {
                            result.Add(MetadataFormatTag.Title, ffprobe.format.tags.title);
                        }

                        if (!string.IsNullOrWhiteSpace(ffprobe?.format?.tags?.track))
                        {
                            result.Add(MetadataFormatTag.Track, ffprobe.format.tags.track);
                        }

                        return Right<BaseError, Dictionary<string, string>>(result);
                    },
                    Left<BaseError, Dictionary<string, string>>);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get format tags for media item {Id}", mediaItem.Id);
                return BaseError.New(ex.Message);
            }
        }

        private async Task<bool> ApplyVersionUpdate(MediaItem mediaItem, MediaVersion version, string filePath)
        {
            MediaVersion mediaItemVersion = mediaItem.GetHeadVersion();

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
            startInfo.ArgumentList.Add("-show_chapters");
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

        internal MediaVersion ProjectToMediaVersion(string path, FFprobe probeOutput) =>
            Optional(probeOutput)
                .Filter(json => json?.format != null && json.streams != null)
                .ToValidation<BaseError>("Unable to parse ffprobe output")
                .ToEither<FFprobe>()
                .Match(
                    json =>
                    {
                        var version = new MediaVersion
                        {
                            Name = "Main",
                            DateAdded = DateTime.UtcNow,
                            Streams = new List<MediaStream>(),
                            Chapters = new List<MediaChapter>()
                        };

                        if (double.TryParse(
                            json.format.duration,
                            NumberStyles.Number,
                            CultureInfo.InvariantCulture,
                            out double duration))
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
                            version.RFrameRate = videoStream.r_frame_rate;

                            var stream = new MediaStream
                            {
                                MediaVersionId = version.Id,
                                MediaStreamKind = MediaStreamKind.Video,
                                Index = videoStream.index,
                                Codec = videoStream.codec_name,
                                Profile = (videoStream.profile ?? string.Empty).ToLowerInvariant(),
                                PixelFormat = (videoStream.pix_fmt ?? string.Empty).ToLowerInvariant(),
                            };

                            if (int.TryParse(videoStream.bits_per_raw_sample, out int bitsPerRawSample))
                            {
                                stream.BitsPerRawSample = bitsPerRawSample;
                            }

                            if (videoStream.disposition is not null)
                            {
                                stream.Default = videoStream.disposition.@default == 1;
                                stream.Forced = videoStream.disposition.forced == 1;
                                stream.AttachedPic = videoStream.disposition.attached_pic == 1;
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

                        foreach (FFprobeChapter probedChapter in json.chapters)
                        {
                            if (double.TryParse(
                                    probedChapter.start_time,
                                    NumberStyles.Number,
                                    CultureInfo.InvariantCulture,
                                    out double startTime)
                                && double.TryParse(
                                    probedChapter.end_time,
                                    NumberStyles.Number,
                                    CultureInfo.InvariantCulture,
                                    out double endTime))
                            {
                                var chapter = new MediaChapter
                                {
                                    MediaVersionId = version.Id,
                                    ChapterId = probedChapter.id,
                                    StartTime = TimeSpan.FromSeconds(startTime),
                                    EndTime = TimeSpan.FromSeconds(endTime),
                                    Title = probedChapter?.tags?.title
                                };

                                version.Chapters.Add(chapter);
                            }
                            else
                            {
                                _logger.LogWarning(
                                    "Media item at {Path} has a missing or invalid chapter start/end time",
                                    path);
                            }
                        }

                        if (version.Chapters.Any())
                        {
                            MediaChapter last = version.Chapters.Last();
                            if (last.EndTime != version.Duration)
                            {
                                last.EndTime = version.Duration;
                            }
                        }
                        
                        return version;
                    },
                    _ => new MediaVersion
                    {
                        Name = "Main",
                        DateAdded = DateTime.UtcNow,
                        Streams = new List<MediaStream>(),
                        Chapters = new List<MediaChapter>()
                    });

        private VideoScanKind ScanKindFromFieldOrder(string fieldOrder) =>
            fieldOrder?.ToLowerInvariant() switch
            {
                var x when x == "tt" || x == "bb" || x == "tb" || x == "bt" => VideoScanKind.Interlaced,
                "progressive" => VideoScanKind.Progressive,
                _ => VideoScanKind.Unknown
            };

        // ReSharper disable InconsistentNaming
        public record FFprobe(FFprobeFormat format, List<FFprobeStream> streams, List<FFprobeChapter> chapters);

        public record FFprobeFormat(string duration, FFprobeFormatTags tags);

        public record FFprobeDisposition(int @default, int forced, int attached_pic);

        public record FFprobeTags(string language, string title);

        public record FFprobeFormatTags(
            string title,
            string artist,
            string album,
            string track,
            string genre,
            string date);

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
            string pix_fmt,
            string field_order,
            string r_frame_rate,
            string bits_per_raw_sample,
            FFprobeDisposition disposition,
            FFprobeTags tags);

        public record FFprobeChapter(
            long id,
            string start_time,
            string end_time,
            FFprobeTags tags);
        // ReSharper restore InconsistentNaming
    }
}
