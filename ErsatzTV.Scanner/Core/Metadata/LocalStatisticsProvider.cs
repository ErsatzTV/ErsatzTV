using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Bugsnag;
using CliWrap;
using CliWrap.Buffered;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Scanner.Core.Interfaces.Metadata;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ErsatzTV.Scanner.Core.Metadata;

public class LocalStatisticsProvider : ILocalStatisticsProvider
{
    private readonly IClient _client;
    private readonly ILocalFileSystem _localFileSystem;
    private readonly ILogger<LocalStatisticsProvider> _logger;
    private readonly IMetadataRepository _metadataRepository;

    public LocalStatisticsProvider(
        IMetadataRepository metadataRepository,
        ILocalFileSystem localFileSystem,
        IClient client,
        ILogger<LocalStatisticsProvider> logger)
    {
        _metadataRepository = metadataRepository;
        _localFileSystem = localFileSystem;
        _client = client;
        _logger = logger;
    }

    public async Task<Either<BaseError, bool>> RefreshStatistics(
        string ffmpegPath,
        string ffprobePath,
        MediaItem mediaItem)
    {
        try
        {
            string filePath = mediaItem.GetHeadVersion().MediaFiles.Head().Path;
            return await RefreshStatistics(ffmpegPath, ffprobePath, mediaItem, filePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh statistics for media item {Id}", mediaItem.Id);
            _client.Notify(ex);
            return BaseError.New(ex.Message);
        }
    }

    public async Task<Either<BaseError, bool>> RefreshStatistics(
        string ffmpegPath,
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
                    if (version.Duration.TotalSeconds < 1)
                    {
                        await AnalyzeDuration(ffmpegPath, mediaItemPath, version);
                    }

                    bool result = await ApplyVersionUpdate(mediaItem, version, mediaItemPath);
                    return Right<BaseError, bool>(result);
                },
                error => Task.FromResult(Left<BaseError, bool>(error)));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh statistics for media item {Id}", mediaItem.Id);
            _client.Notify(ex);
            return BaseError.New(ex.Message);
        }
    }

    public async Task<Either<BaseError, Dictionary<string, string>>> GetSongTags(
        string ffprobePath,
        MediaItem mediaItem)
    {
        try
        {
            string mediaItemPath = mediaItem.GetHeadVersion().MediaFiles.Head().Path;
            Either<BaseError, FFprobe> maybeProbe = await GetProbeOutput(ffprobePath, mediaItemPath);
            foreach (BaseError error in maybeProbe.LeftToSeq())
            {
                return error;
            }

            Option<FFprobeTags> maybeFormatTags = maybeProbe.RightToSeq()
                .Map(p => p?.format?.tags ?? FFprobeTags.Empty)
                .HeadOrNone();

            Option<FFprobeTags> maybeAudioTags = maybeProbe.RightToSeq()
                .Bind(p => p.streams.Filter(s => s.codec_type == "audio").HeadOrNone())
                .Map(s => s.tags ?? FFprobeTags.Empty)
                .HeadOrNone();

            foreach (FFprobeTags formatTags in maybeFormatTags)
            foreach (FFprobeTags audioTags in maybeAudioTags)
            {
                var result = new Dictionary<string, string>();

                // album
                if (!string.IsNullOrWhiteSpace(formatTags.album))
                {
                    result.Add(MetadataSongTag.Album, formatTags.album);
                }
                else if (!string.IsNullOrWhiteSpace(audioTags.album))
                {
                    result.Add(MetadataSongTag.Album, audioTags.album);
                }

                // album artist
                if (!string.IsNullOrWhiteSpace(formatTags.albumArtist))
                {
                    result.Add(MetadataSongTag.AlbumArtist, formatTags.albumArtist);
                }
                else if (!string.IsNullOrWhiteSpace(audioTags.albumArtist))
                {
                    result.Add(MetadataSongTag.AlbumArtist, audioTags.albumArtist);
                }

                // artist
                if (!string.IsNullOrWhiteSpace(formatTags.artist))
                {
                    result.Add(MetadataSongTag.Artist, formatTags.artist);

                    // if no album artist is present, use the track artist
                    result.TryAdd(MetadataSongTag.AlbumArtist, formatTags.artist);
                }
                else if (!string.IsNullOrWhiteSpace(audioTags.artist))
                {
                    result.Add(MetadataSongTag.Artist, audioTags.artist);

                    // if no album artist is present, use the track artist
                    result.TryAdd(MetadataSongTag.AlbumArtist, audioTags.artist);
                }

                // date
                if (!string.IsNullOrWhiteSpace(formatTags.date))
                {
                    result.Add(MetadataSongTag.Date, formatTags.date);
                }
                else if (!string.IsNullOrWhiteSpace(audioTags.date))
                {
                    result.Add(MetadataSongTag.Date, audioTags.date);
                }

                // genre
                if (!string.IsNullOrWhiteSpace(formatTags.genre))
                {
                    result.Add(MetadataSongTag.Genre, formatTags.genre);
                }
                else if (!string.IsNullOrWhiteSpace(audioTags.genre))
                {
                    result.Add(MetadataSongTag.Genre, audioTags.genre);
                }

                // title
                if (!string.IsNullOrWhiteSpace(formatTags.title))
                {
                    result.Add(MetadataSongTag.Title, formatTags.title);
                }
                else if (!string.IsNullOrWhiteSpace(audioTags.title))
                {
                    result.Add(MetadataSongTag.Title, audioTags.title);
                }

                // track
                if (!string.IsNullOrWhiteSpace(formatTags.track))
                {
                    result.Add(MetadataSongTag.Track, formatTags.track);
                }
                else if (!string.IsNullOrWhiteSpace(audioTags.track))
                {
                    result.Add(MetadataSongTag.Track, audioTags.track);
                }

                return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get format tags for media item {Id}", mediaItem.Id);
            _client.Notify(ex);
            return BaseError.New(ex.Message);
        }

        return BaseError.New("BUG - this should never happen");
    }

    private async Task<bool> ApplyVersionUpdate(MediaItem mediaItem, MediaVersion version, string filePath)
    {
        MediaVersion mediaItemVersion = mediaItem.GetHeadVersion();

        bool durationChange = mediaItemVersion.Duration != version.Duration;

        version.DateUpdated = _localFileSystem.GetLastWriteTime(filePath);

        return await _metadataRepository.UpdateStatistics(mediaItem, version) && durationChange;
    }

    private static async Task<Either<BaseError, FFprobe>> GetProbeOutput(string ffprobePath, string filePath)
    {
        string[] arguments =
        {
            "-hide_banner",
            "-print_format", "json",
            "-show_format",
            "-show_streams",
            "-show_chapters",
            "-i", filePath
        };

        BufferedCommandResult probe = await Cli.Wrap(ffprobePath)
            .WithArguments(arguments)
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync(Encoding.UTF8);

        if (probe.ExitCode != 0)
        {
            return BaseError.New($"FFprobe at {ffprobePath} exited with code {probe.ExitCode}");
        }

        FFprobe? ffprobe = JsonConvert.DeserializeObject<FFprobe>(probe.StandardOutput);
        if (ffprobe is not null)
        {
            const string PATTERN = @"\[SAR\s+([0-9]+:[0-9]+)\s+DAR\s+([0-9]+:[0-9]+)\]";
            Match match = Regex.Match(probe.StandardError, PATTERN);
            if (match.Success)
            {
                string sar = match.Groups[1].Value;
                string dar = match.Groups[2].Value;
                foreach (FFprobeStreamData stream in Optional(ffprobe.streams?.Where(s => s.codec_type == "video").ToList())
                             .Flatten())
                {
                    FFprobeStreamData replacement = stream with { sample_aspect_ratio = sar, display_aspect_ratio = dar };
                    ffprobe.streams?.Remove(stream);
                    ffprobe.streams?.Add(replacement);
                }
            }

            // fix chapter ids to be something sensible
            var maybeChapters = Optional(ffprobe.chapters).Flatten().ToList();
            var newChapters = new List<FFprobeChapter>();
            for (var index = 0; index < maybeChapters.Count; index++)
            {
                FFprobeChapter chapter = maybeChapters[index];
                newChapters.Add(chapter with { id = index });
            }

            return ffprobe with { chapters = newChapters };
        }

        return BaseError.New("Unable to deserialize ffprobe output");
    }

    private async Task AnalyzeDuration(string ffmpegPath, string path, MediaVersion version)
    {
        try
        {
            _logger.LogInformation(
                "Media item at {Path} is missing duration metadata and requires additional analysis",
                path);

            var startInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            startInfo.ArgumentList.Add("-i");
            startInfo.ArgumentList.Add(path);
            startInfo.ArgumentList.Add("-f");
            startInfo.ArgumentList.Add("null");
            startInfo.ArgumentList.Add("-");

            using var probe = new Process();
            probe.StartInfo = startInfo;

            probe.Start();
            string output = await probe.StandardError.ReadToEndAsync();
            await probe.WaitForExitAsync();
            if (probe.ExitCode == 0)
            {
                const string PATTERN = @"time=([^ ]+)";
                IEnumerable<string> reversed = output.Split("\n").Reverse();
                foreach (string line in reversed)
                {
                    Match match = Regex.Match(line, PATTERN);
                    if (match.Success)
                    {
                        string time = match.Groups[1].Value;
                        var duration = TimeSpan.Parse(time, NumberFormatInfo.InvariantInfo);
                        _logger.LogInformation("Analyzed duration is {Duration:hh\\:mm\\:ss}", duration);
                        version.Duration = duration;
                        return;
                    }
                }
            }
            else
            {
                _logger.LogError("Duration analysis failed for media item at {Path}", path);
            }
        }
        catch (Exception ex)
        {
            _client.Notify(ex);
            _logger.LogError("Duration analysis failed for media item at {Path}", path);
        }
    }

    internal MediaVersion ProjectToMediaVersion(string path, FFprobe probeOutput) =>
        Optional(probeOutput)
            .Filter(json => json is { format: not null, streams: not null })
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
                            json.format?.duration,
                            NumberStyles.Number,
                            CultureInfo.InvariantCulture,
                            out double duration))
                    {
                        var seconds = TimeSpan.FromSeconds(duration);
                        version.Duration = seconds;
                    }
                    // else
                    // {
                    //     _logger.LogWarning(
                    //         "Media item at {Path} has a missing or invalid duration {Duration} and will cause scheduling issues",
                    //         path,
                    //         json.format.duration);
                    // }

                    foreach (FFprobeStreamData audioStream in json.streams.Filter(s => s.codec_type == "audio"))
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

                    FFprobeStreamData? videoStream = json.streams?.FirstOrDefault(s => s.codec_type == "video");
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
                            ColorRange = (videoStream.color_range ?? string.Empty).ToLowerInvariant(),
                            ColorSpace = (videoStream.color_space ?? string.Empty).ToLowerInvariant(),
                            ColorTransfer = (videoStream.color_transfer ?? string.Empty).ToLowerInvariant(),
                            ColorPrimaries = (videoStream.color_primaries ?? string.Empty).ToLowerInvariant()
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

                    foreach (FFprobeStreamData subtitleStream in json.streams.Filter(s => s.codec_type == "subtitle"))
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
                            stream.Title = subtitleStream.tags.title;
                        }

                        version.Streams.Add(stream);
                    }

                    foreach (FFprobeStreamData attachmentStream in json.streams.Filter(s => s.codec_type == "attachment"))
                    {
                        var stream = new MediaStream
                        {
                            MediaVersionId = version.Id,
                            MediaStreamKind = MediaStreamKind.Attachment,
                            Index = attachmentStream.index,
                            Codec = attachmentStream.codec_name
                        };

                        if (attachmentStream.tags is not null)
                        {
                            stream.FileName = attachmentStream.tags.filename;
                            stream.MimeType = attachmentStream.tags.mimetype;
                        }

                        version.Streams.Add(stream);
                    }

                    foreach (FFprobeChapter probedChapter in Optional(json.chapters).Flatten())
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
                                Title = probedChapter.tags?.title
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

    private static VideoScanKind ScanKindFromFieldOrder(string? fieldOrder) =>
        fieldOrder?.ToLowerInvariant() switch
        {
            "tt" or "bb" or "tb" or "bt" => VideoScanKind.Interlaced,
            "progressive" => VideoScanKind.Progressive,
            _ => VideoScanKind.Unknown
        };

    // ReSharper disable InconsistentNaming
    public record FFprobe(FFprobeFormat? format, List<FFprobeStreamData>? streams, List<FFprobeChapter>? chapters);

    public record FFprobeFormat(string duration, FFprobeTags? tags);

    [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores")]
    public record FFprobeDisposition(int @default, int forced, int attached_pic);

    [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores")]
    public record FFprobeStreamData(
        int index,
        string? codec_name,
        string? profile,
        string? codec_type,
        int channels,
        int width,
        int height,
        string? sample_aspect_ratio,
        string? display_aspect_ratio,
        string? pix_fmt,
        string? color_range,
        string? color_space,
        string? color_transfer,
        string? color_primaries,
        string? field_order,
        string? r_frame_rate,
        string? bits_per_raw_sample,
        FFprobeDisposition? disposition,
        FFprobeTags? tags);

    [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores")]
    public record FFprobeChapter(
        long id,
        string? start_time,
        string? end_time,
        FFprobeTags? tags);

    public record FFprobeTags(
        string? language,
        string? title,
        string? filename,
        string? mimetype,
        string? artist,
        [property: JsonProperty(PropertyName = "album_artist")]
        string? albumArtist,
        string? album,
        string? track,
        string? genre,
        string? date)
    {
        public static readonly FFprobeTags Empty = new(null, null, null, null, null, null, null, null, null, null);
    }
    // ReSharper restore InconsistentNaming
}
