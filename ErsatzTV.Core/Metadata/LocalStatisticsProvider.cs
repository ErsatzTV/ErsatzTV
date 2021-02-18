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
        private readonly ILogger<LocalStatisticsProvider> _logger;
        private readonly IMediaItemRepository _mediaItemRepository;

        public LocalStatisticsProvider(
            IMediaItemRepository mediaItemRepository,
            ILogger<LocalStatisticsProvider> logger)
        {
            _mediaItemRepository = mediaItemRepository;
            _logger = logger;
        }

        public async Task<bool> RefreshStatistics(string ffprobePath, MediaItem mediaItem)
        {
            try
            {
                FFprobe ffprobe = await GetProbeOutput(ffprobePath, mediaItem);
                MediaItemStatistics statistics = ProjectToMediaItemStatistics(ffprobe);
                return await ApplyStatisticsUpdate(mediaItem, statistics);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to refresh statistics for media item at {Path}", mediaItem.Path);
                return false;
            }
        }

        private async Task<bool> ApplyStatisticsUpdate(
            MediaItem mediaItem,
            MediaItemStatistics statistics)
        {
            if (mediaItem.Statistics == null)
            {
                mediaItem.Statistics = new MediaItemStatistics();
            }

            bool durationChange = mediaItem.Statistics.Duration != statistics.Duration;

            mediaItem.Statistics.Duration = statistics.Duration;
            mediaItem.Statistics.AudioCodec = statistics.AudioCodec;
            mediaItem.Statistics.SampleAspectRatio = statistics.SampleAspectRatio;
            mediaItem.Statistics.DisplayAspectRatio = statistics.DisplayAspectRatio;
            mediaItem.Statistics.Width = statistics.Width;
            mediaItem.Statistics.Height = statistics.Height;
            mediaItem.Statistics.VideoCodec = statistics.VideoCodec;
            mediaItem.Statistics.VideoScanType = statistics.VideoScanType;

            return await _mediaItemRepository.Update(mediaItem) && durationChange;
        }

        private Task<FFprobe> GetProbeOutput(string ffprobePath, MediaItem mediaItem)
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
            startInfo.ArgumentList.Add(mediaItem.Path);

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

        private MediaItemStatistics ProjectToMediaItemStatistics(FFprobe probeOutput) =>
            Optional(probeOutput)
                .Filter(json => json?.format != null && json.streams != null)
                .ToValidation<BaseError>("Unable to parse ffprobe output")
                .ToEither<FFprobe>()
                .Match(
                    json =>
                    {
                        var duration = TimeSpan.FromSeconds(double.Parse(json.format.duration));

                        var statistics = new MediaItemStatistics { Duration = duration };

                        FFprobeStream audioStream = json.streams.FirstOrDefault(s => s.codec_type == "audio");
                        if (audioStream != null)
                        {
                            statistics = statistics with
                            {
                                AudioCodec = audioStream.codec_name
                            };
                        }

                        FFprobeStream videoStream = json.streams.FirstOrDefault(s => s.codec_type == "video");
                        if (videoStream != null)
                        {
                            statistics = statistics with
                            {
                                SampleAspectRatio = videoStream.sample_aspect_ratio,
                                DisplayAspectRatio = videoStream.display_aspect_ratio,
                                Width = videoStream.width,
                                Height = videoStream.height,
                                VideoCodec = videoStream.codec_name,
                                VideoScanType = ScanTypeFromFieldOrder(videoStream.field_order)
                            };
                        }

                        return statistics;
                    },
                    _ => new MediaItemStatistics());

        private VideoScanType ScanTypeFromFieldOrder(string fieldOrder) =>
            fieldOrder?.ToLowerInvariant() switch
            {
                var x when x == "tt" || x == "bb" || x == "tb" || x == "bt" => VideoScanType.Interlaced,
                "progressive" => VideoScanType.Progressive,
                _ => VideoScanType.Unknown
            };

        // ReSharper disable InconsistentNaming
        public record FFprobe(FFprobeFormat format, List<FFprobeStream> streams);

        public record FFprobeFormat(string duration);

        public record FFprobeStream(
            int index,
            string codec_name,
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
