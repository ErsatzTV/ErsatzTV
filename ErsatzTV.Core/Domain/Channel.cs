using ErsatzTV.Core.Domain.Filler;
using System.Net;

namespace ErsatzTV.Core.Domain;

public class Channel
{
    public static readonly string NumberValidator = @"^[0-9]+(\.[0-9]{1,2})?$";

    public Channel(Guid uniqueId) => UniqueId = uniqueId;
    public int Id { get; set; }
    public Guid UniqueId { get; init; }
    public string Number { get; set; }
    public string Name { get; set; }
    public string Group { get; set; }
    public string Categories { get; set; }
    public int FFmpegProfileId { get; set; }
    public FFmpegProfile FFmpegProfile { get; set; }
    public int? WatermarkId { get; set; }
    public ChannelWatermark Watermark { get; set; }
    public int? FallbackFillerId { get; set; }
    public FillerPreset FallbackFiller { get; set; }
    public StreamingMode StreamingMode { get; set; }
    public List<Playout> Playouts { get; set; }
    public List<Artwork> Artwork { get; set; }
    public string PreferredAudioLanguageCode { get; set; }
    public string PreferredAudioTitle { get; set; }
    public string PreferredSubtitleLanguageCode { get; set; }
    public ChannelSubtitleMode SubtitleMode { get; set; }
    public ChannelMusicVideoCreditsMode MusicVideoCreditsMode { get; set; }
    public string MusicVideoCreditsTemplate { get; set; }
    public ChannelSongVideoMode SongVideoMode { get; set; }
    public ChannelProgressMode ProgressMode { get; set; }
    public string WebEncodedName => WebUtility.UrlEncode(Name);
}
