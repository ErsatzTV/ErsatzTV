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
        private readonly IMediaItemRepository _mediaItemRepository;

        public LocalStatisticsProvider(
            IMediaItemRepository mediaItemRepository,
            ILocalFileSystem localFileSystem,
            ILogger<LocalStatisticsProvider> logger)
        {
            _mediaItemRepository = mediaItemRepository;
            _localFileSystem = localFileSystem;
            _logger = logger;
        }

        public async Task<bool> RefreshStatistics(string ffprobePath, MediaItem mediaItem)
        {
            try
            {
                string filePath = mediaItem switch
                {
                    Movie m => m.MediaVersions.Head().MediaFiles.Head().Path,
                    Episode e => e.MediaVersions.Head().MediaFiles.Head().Path,
                    _ => throw new ArgumentOutOfRangeException(nameof(mediaItem))
                };

                FFprobe ffprobe = await GetProbeOutput(ffprobePath, filePath);
                MediaVersion version = ProjectToMediaVersion(ffprobe);
                return await ApplyVersionUpdate(mediaItem, version, filePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to refresh statistics for media item {Id}", mediaItem.Id);
                return false;
            }
        }

        private async Task<bool> ApplyVersionUpdate(MediaItem mediaItem, MediaVersion version, string filePath)
        {
            MediaVersion mediaItemVersion = mediaItem switch
            {
                Movie m => m.MediaVersions.Head(),
                Episode e => e.MediaVersions.Head(),
                _ => throw new ArgumentOutOfRangeException(nameof(mediaItem))
            };

            bool durationChange = mediaItemVersion.Duration != version.Duration;

            mediaItemVersion.DateUpdated = _localFileSystem.GetLastWriteTime(filePath);
            mediaItemVersion.Duration = version.Duration;
            mediaItemVersion.AudioCodec = version.AudioCodec;
            mediaItemVersion.SampleAspectRatio = version.SampleAspectRatio;
            mediaItemVersion.DisplayAspectRatio = version.DisplayAspectRatio;
            mediaItemVersion.Width = version.Width;
            mediaItemVersion.Height = version.Height;
            mediaItemVersion.VideoCodec = version.VideoCodec;
            mediaItemVersion.VideoProfile = version.VideoProfile;
            mediaItemVersion.VideoScanKind = version.VideoScanKind;

            return await _mediaItemRepository.Update(mediaItem) && durationChange;
        }

        private Task<FFprobe> GetProbeOutput(string ffprobePath, string filePath)
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
            return probe.StandardOutput.ReadToEndAsync().MapAsync(
                async output =>
                {
                    await probe.WaitForExitAsync();
                    return JsonConvert.DeserializeObject<FFprobe>(output);
                });
        }

        private MediaVersion ProjectToMediaVersion(FFprobe probeOutput) =>
            Optional(probeOutput)
                .Filter(json => json?.format != null && json.streams != null)
                .ToValidation<BaseError>("Unable to parse ffprobe output")
                .ToEither<FFprobe>()
                .Match(
                    json =>
                    {
                        var duration = TimeSpan.FromSeconds(double.Parse(json.format.duration));

                        var version = new MediaVersion { Name = "Main", Duration = duration };

                        FFprobeStream audioStream = json.streams.FirstOrDefault(s => s.codec_type == "audio");
                        if (audioStream != null)
                        {
                            version.AudioCodec = audioStream.codec_name;
                        }

                        FFprobeStream videoStream = json.streams.FirstOrDefault(s => s.codec_type == "video");
                        if (videoStream != null)
                        {
                            version.SampleAspectRatio = videoStream.sample_aspect_ratio;
                            version.DisplayAspectRatio = videoStream.display_aspect_ratio;
                            version.Width = videoStream.width;
                            version.Height = videoStream.height;
                            version.VideoCodec = videoStream.codec_name;
                            version.VideoProfile = (videoStream.profile ?? string.Empty).ToLowerInvariant();
                            version.VideoScanKind = ScanKindFromFieldOrder(videoStream.field_order);
                        }

                        return version;
                    },
                    _ => new MediaVersion { Name = "Main" });

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

        public record FFprobeStream(
            int index,
            string codec_name,
            string profile,
            string codec_type,
            int width,
            int height,
            string sample_aspect_ratio,
            string display_aspect_ratio,
            string field_order,
            string r_frame_rate);
        // ReSharper restore InconsistentNaming
    }
}
