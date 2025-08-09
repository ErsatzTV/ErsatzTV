namespace ErsatzTV.Core.Domain.TemplateData;

public record MusicVideoTemplateData(
    Resolution Resolution,
    string Title,
    int? Track,
    string Album,
    string Plot,
    DateTime? ReleaseDate,
    List<string> AllArtists,
    string Artist,
    List<string> Studios,
    List<string> Directors,
    TimeSpan Duration,
    TimeSpan StreamSeek);

    // ffmpegProfile.Resolution,
    // metadata.Title,
    // metadata.Track,
    // metadata.Album,
    // metadata.Plot,
    // metadata.ReleaseDate,
    // AllArtists = (metadata.Artists ?? new List<MusicVideoArtist>()).Map(a => a.Name),
    // Artist = artist,
    // Studios = (metadata.Studios ?? new List<Studio>()).Map(s => s.Name),
    // Directors = (metadata.Directors ?? new List<Director>()).Map(s => s.Name),
    // musicVideo.GetHeadVersion().Duration,
    // StreamSeek = await settings.StreamSeek.IfNoneAsync(TimeSpan.Zero)