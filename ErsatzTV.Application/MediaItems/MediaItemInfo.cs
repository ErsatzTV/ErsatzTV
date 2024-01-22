using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.MediaItems;

public record MediaItemInfo(
    int Id,
    string Kind,
    string LibraryKind,
    string ServerName,
    string LibraryName,
    MediaItemState State,
    TimeSpan Duration,
    string SampleAspectRatio,
    string DisplayAspectRatio,
    string RFrameRate,
    VideoScanKind VideoScanKind,
    int Width,
    int Height,
    List<MediaItemInfoStream> Streams,
    List<MediaItemInfoChapter> Chapters);
