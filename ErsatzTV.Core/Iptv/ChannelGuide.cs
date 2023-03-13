using System.Text;
using System.Xml;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Emby;
using ErsatzTV.Core.Jellyfin;
using Microsoft.IO;
using Serilog;

namespace ErsatzTV.Core.Iptv;

public class ChannelGuide
{
    private readonly List<Channel> _channels;
    private readonly string _host;
    private readonly string _baseUrl;
    private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;
    private readonly string _scheme;

    public ChannelGuide(
        RecyclableMemoryStreamManager recyclableMemoryStreamManager,
        string scheme,
        string host,
        string baseUrl,
        List<Channel> channels)
    {
        _recyclableMemoryStreamManager = recyclableMemoryStreamManager;
        _scheme = scheme;
        _host = host;
        _baseUrl = baseUrl;
        _channels = channels;
    }

    public string ToXml()
    {
        using MemoryStream ms = _recyclableMemoryStreamManager.GetStream();
        using var xml = XmlWriter.Create(ms);
        xml.WriteStartDocument();

        xml.WriteStartElement("tv");
        xml.WriteAttributeString("generator-info-name", "ersatztv");

        var sortedChannelItems = new Dictionary<Channel, List<PlayoutItem>>();

        foreach (Channel channel in _channels.OrderBy(c => decimal.Parse(c.Number)))
        {
            var sortedItems = channel.Playouts.Collect(p => p.Items).OrderBy(x => x.Start).ToList();
            sortedChannelItems.Add(channel, sortedItems);

            if (sortedItems.Any())
            {
                xml.WriteStartElement("channel");
                xml.WriteAttributeString("id", $"{channel.Number}.etv");

                xml.WriteStartElement("display-name");
                xml.WriteString($"{channel.Number} {channel.Name}");
                xml.WriteEndElement(); // display-name (number and name)

                xml.WriteStartElement("display-name");
                xml.WriteString(channel.Number);
                xml.WriteEndElement(); // display-name (number)

                xml.WriteStartElement("display-name");
                xml.WriteString(channel.Name);
                xml.WriteEndElement(); // display-name (name)

                foreach (string category in GetCategories(channel.Categories))
                {
                    xml.WriteStartElement("category");
                    xml.WriteAttributeString("lang", "en");
                    xml.WriteString(category);
                    xml.WriteEndElement(); // category
                }

                xml.WriteStartElement("icon");
                string logo = Optional(channel.Artwork).Flatten()
                    .Filter(a => a.ArtworkKind == ArtworkKind.Logo)
                    .HeadOrNone()
                    .Match(
                        artwork => $"{_scheme}://{_host}{_baseUrl}/iptv/logos/{artwork.Path}.jpg",
                        () => $"{_scheme}://{_host}{_baseUrl}/iptv/images/ersatztv-500.png");
                xml.WriteAttributeString("src", logo);
                xml.WriteEndElement(); // icon

                xml.WriteEndElement(); // channel
            }
        }

        foreach ((Channel channel, List<PlayoutItem> sorted) in sortedChannelItems.OrderBy(
                     kvp => decimal.Parse(kvp.Key.Number)))
        {
            // skip all filler that isn't pre-roll
            var i = 0;
            while (i < sorted.Count && sorted[i].FillerKind != FillerKind.None &&
                   sorted[i].FillerKind != FillerKind.PreRoll)
            {
                i++;
            }

            while (i < sorted.Count)
            {
                PlayoutItem startItem = sorted[i];
                int j = i;
                while (sorted[j].FillerKind != FillerKind.None && j + 1 < sorted.Count)
                {
                    j++;
                }

                PlayoutItem displayItem = sorted[j];
                bool hasCustomTitle = !string.IsNullOrWhiteSpace(startItem.CustomTitle);

                int finishIndex = j;
                while (finishIndex + 1 < sorted.Count && (sorted[finishIndex + 1].GuideGroup == startItem.GuideGroup
                                                          || sorted[finishIndex + 1].FillerKind is FillerKind.GuideMode
                                                              or FillerKind.Tail or FillerKind.Fallback))
                {
                    finishIndex++;
                }

                int customShowId = -1;
                if (displayItem.MediaItem is Episode ep)
                {
                    customShowId = ep.Season.ShowId;
                }

                bool isSameCustomShow = hasCustomTitle;
                for (int x = j; x <= finishIndex; x++)
                {
                    isSameCustomShow = isSameCustomShow && sorted[x].MediaItem is Episode e &&
                                       customShowId == e.Season.ShowId;
                }

                PlayoutItem finishItem = sorted[finishIndex];
                i = finishIndex;

                string start = startItem.StartOffset.ToString("yyyyMMddHHmmss zzz").Replace(":", string.Empty);
                string stop = displayItem.GuideFinishOffset.HasValue
                    ? displayItem.GuideFinishOffset.Value.ToString("yyyyMMddHHmmss zzz").Replace(":", string.Empty)
                    : finishItem.FinishOffset.ToString("yyyyMMddHHmmss zzz").Replace(":", string.Empty);

                string title = GetTitle(displayItem);
                string subtitle = GetSubtitle(displayItem);
                string description = GetDescription(displayItem);
                Option<ContentRating> contentRating = GetContentRating(displayItem);

                xml.WriteStartElement("programme");
                xml.WriteAttributeString("start", start);
                xml.WriteAttributeString("stop", stop);
                xml.WriteAttributeString("channel", $"{channel.Number}.etv");

                xml.WriteStartElement("title");
                xml.WriteAttributeString("lang", "en");
                xml.WriteString(title);
                xml.WriteEndElement(); // title

                if (!string.IsNullOrWhiteSpace(subtitle))
                {
                    xml.WriteStartElement("sub-title");
                    xml.WriteAttributeString("lang", "en");
                    xml.WriteString(subtitle);
                    xml.WriteEndElement(); // subtitle
                }

                if (!isSameCustomShow)
                {
                    if (!string.IsNullOrWhiteSpace(description))
                    {
                        xml.WriteStartElement("desc");
                        xml.WriteAttributeString("lang", "en");
                        xml.WriteString(description);
                        xml.WriteEndElement(); // desc
                    }
                }

                if (!hasCustomTitle && displayItem.MediaItem is Movie movie)
                {
                    foreach (MovieMetadata metadata in movie.MovieMetadata.HeadOrNone())
                    {
                        if (metadata.Year.HasValue)
                        {
                            xml.WriteStartElement("date");
                            xml.WriteString(metadata.Year.Value.ToString());
                            xml.WriteEndElement(); // date
                        }

                        xml.WriteStartElement("category");
                        xml.WriteAttributeString("lang", "en");
                        xml.WriteString("Movie");
                        xml.WriteEndElement(); // category

                        foreach (Genre genre in Optional(metadata.Genres).Flatten())
                        {
                            xml.WriteStartElement("category");
                            xml.WriteAttributeString("lang", "en");
                            xml.WriteString(genre.Name);
                            xml.WriteEndElement(); // category
                        }

                        string poster = Optional(metadata.Artwork).Flatten()
                            .Filter(a => a.ArtworkKind == ArtworkKind.Poster)
                            .HeadOrNone()
                            .Match(a => GetArtworkUrl(a, ArtworkKind.Poster), () => string.Empty);

                        if (!string.IsNullOrWhiteSpace(poster))
                        {
                            xml.WriteStartElement("icon");
                            xml.WriteAttributeString("src", poster);
                            xml.WriteEndElement(); // icon
                        }
                    }
                }

                if (!hasCustomTitle && displayItem.MediaItem is MusicVideo musicVideo)
                {
                    foreach (MusicVideoMetadata metadata in musicVideo.MusicVideoMetadata.HeadOrNone())
                    {
                        if (metadata.Year.HasValue)
                        {
                            xml.WriteStartElement("date");
                            xml.WriteString(metadata.Year.Value.ToString());
                            xml.WriteEndElement(); // date
                        }

                        xml.WriteStartElement("category");
                        xml.WriteAttributeString("lang", "en");
                        xml.WriteString("Music");
                        xml.WriteEndElement(); // category

                        // music video genres
                        foreach (Genre genre in Optional(metadata.Genres).Flatten())
                        {
                            xml.WriteStartElement("category");
                            xml.WriteAttributeString("lang", "en");
                            xml.WriteString(genre.Name);
                            xml.WriteEndElement(); // category
                        }

                        // artist genres
                        Option<ArtistMetadata> maybeMetadata =
                            Optional(musicVideo.Artist?.ArtistMetadata.HeadOrNone()).Flatten();
                        foreach (ArtistMetadata artistMetadata in maybeMetadata)
                        {
                            foreach (Genre genre in Optional(artistMetadata.Genres).Flatten())
                            {
                                xml.WriteStartElement("category");
                                xml.WriteAttributeString("lang", "en");
                                xml.WriteString(genre.Name);
                                xml.WriteEndElement(); // category
                            }
                        }

                        string thumbnail = Optional(metadata.Artwork).Flatten()
                            .Filter(a => a.ArtworkKind == ArtworkKind.Thumbnail)
                            .HeadOrNone()
                            .Match(a => GetArtworkUrl(a, ArtworkKind.Thumbnail), () => string.Empty);

                        if (!string.IsNullOrWhiteSpace(thumbnail))
                        {
                            xml.WriteStartElement("icon");
                            xml.WriteAttributeString("src", thumbnail);
                            xml.WriteEndElement(); // icon
                        }
                    }
                }

                if (!hasCustomTitle && displayItem.MediaItem is Song song)
                {
                    xml.WriteStartElement("category");
                    xml.WriteAttributeString("lang", "en");
                    xml.WriteString("Music");
                    xml.WriteEndElement(); // category

                    foreach (SongMetadata metadata in song.SongMetadata.HeadOrNone())
                    {
                        string thumbnail = Optional(metadata.Artwork).Flatten()
                            .Filter(a => a.ArtworkKind == ArtworkKind.Thumbnail)
                            .HeadOrNone()
                            .Match(a => GetArtworkUrl(a, ArtworkKind.Thumbnail), () => string.Empty);

                        if (!string.IsNullOrWhiteSpace(thumbnail))
                        {
                            xml.WriteStartElement("icon");
                            xml.WriteAttributeString("src", thumbnail);
                            xml.WriteEndElement(); // icon
                        }
                    }
                }

                if (displayItem.MediaItem is Episode episode && (!hasCustomTitle || isSameCustomShow))
                {
                    Option<ShowMetadata> maybeMetadata =
                        Optional(episode.Season?.Show?.ShowMetadata.HeadOrNone()).Flatten();
                    foreach (ShowMetadata metadata in maybeMetadata)
                    {
                        xml.WriteStartElement("category");
                        xml.WriteAttributeString("lang", "en");
                        xml.WriteString("Series");
                        xml.WriteEndElement(); // category

                        foreach (Genre genre in Optional(metadata.Genres).Flatten())
                        {
                            xml.WriteStartElement("category");
                            xml.WriteAttributeString("lang", "en");
                            xml.WriteString(genre.Name);
                            xml.WriteEndElement(); // category
                        }

                        string artwork = Optional(metadata.Artwork).Flatten()
                            .Filter(a => a.ArtworkKind == ArtworkKind.Thumbnail)
                            .HeadOrNone()
                            .Match(a => GetArtworkUrl(a, ArtworkKind.Thumbnail), () => string.Empty);

                        // fall back to poster
                        if (string.IsNullOrWhiteSpace(artwork))
                        {
                            artwork = Optional(metadata.Artwork).Flatten()
                                .Filter(a => a.ArtworkKind == ArtworkKind.Poster)
                                .HeadOrNone()
                                .Match(a => GetArtworkUrl(a, ArtworkKind.Poster), () => string.Empty);
                        }

                        if (!string.IsNullOrWhiteSpace(artwork))
                        {
                            xml.WriteStartElement("icon");
                            xml.WriteAttributeString("src", artwork);
                            xml.WriteEndElement(); // icon
                        }
                    }

                    if (!isSameCustomShow)
                    {
                        int s = Optional(episode.Season?.SeasonNumber).IfNone(-1);
                        // TODO: multi-episode?
                        int e = episode.EpisodeMetadata.HeadOrNone().Match(em => em.EpisodeNumber, -1);
                        if (s >= 0 && e > 0)
                        {
                            xml.WriteStartElement("episode-num");
                            xml.WriteAttributeString("system", "onscreen");
                            xml.WriteString($"S{s:00}E{e:00}");
                            xml.WriteEndElement(); // episode-num

                            xml.WriteStartElement("episode-num");
                            xml.WriteAttributeString("system", "xmltv_ns");
                            xml.WriteString($"{s - 1}.{e - 1}.0/1");
                            xml.WriteEndElement(); // episode-num
                        }
                    }
                }

                xml.WriteStartElement("previously-shown");
                xml.WriteEndElement(); // previously-shown

                foreach (ContentRating rating in contentRating)
                {
                    xml.WriteStartElement("rating");
                    foreach (string system in rating.System)
                    {
                        xml.WriteAttributeString("system", system);
                    }

                    xml.WriteStartElement("value");
                    xml.WriteString(rating.Value);
                    xml.WriteEndElement(); // value
                    xml.WriteEndElement(); // rating
                }

                xml.WriteEndElement(); // programme

                i++;
            }
        }

        xml.WriteEndElement(); // tv
        xml.WriteEndDocument();

        xml.Flush();
        return Encoding.UTF8.GetString(ms.ToArray());
    }

    private string GetArtworkUrl(Artwork artwork, ArtworkKind artworkKind)
    {
        string artworkPath = artwork.Path;

        int height = artworkKind switch
        {
            ArtworkKind.Thumbnail => 220,
            _ => 440
        };

        if (artworkPath.StartsWith("jellyfin://"))
        {
            artworkPath = JellyfinUrl.ProxyForArtwork(_scheme, _host, artworkPath, artworkKind)
                .SetQueryParam("fillHeight", height);
        }
        else if (artworkPath.StartsWith("emby://"))
        {
            artworkPath = EmbyUrl.ProxyForArtwork(_scheme, _host, artworkPath, artworkKind)
                .SetQueryParam("maxHeight", height);
        }
        else
        {
            string artworkFolder = artworkKind switch
            {
                ArtworkKind.Thumbnail => "thumbnails",
                _ => "posters"
            };

            artworkPath = $"{_scheme}://{_host}{_baseUrl}/iptv/artwork/{artworkFolder}/{artwork.Path}.jpg";
        }

        return artworkPath;
    }

    private static string GetTitle(PlayoutItem playoutItem)
    {
        if (!string.IsNullOrWhiteSpace(playoutItem.CustomTitle))
        {
            return playoutItem.CustomTitle;
        }

        return playoutItem.MediaItem switch
        {
            Movie m => m.MovieMetadata.HeadOrNone().Map(mm => mm.Title ?? string.Empty)
                .IfNone("[unknown movie]"),
            Episode e => e.Season.Show.ShowMetadata.HeadOrNone().Map(em => em.Title ?? string.Empty)
                .IfNone("[unknown show]"),
            MusicVideo mv => mv.Artist.ArtistMetadata.HeadOrNone().Map(am => am.Title ?? string.Empty)
                .IfNone("[unknown artist]"),
            OtherVideo ov => ov.OtherVideoMetadata.HeadOrNone().Map(vm => vm.Title ?? string.Empty)
                .IfNone("[unknown video]"),
            Song s => s.SongMetadata.HeadOrNone().Map(sm => sm.Artist ?? string.Empty)
                .IfNone("[unknown artist]"),
            _ => "[unknown]"
        };
    }

    private static string GetSubtitle(PlayoutItem playoutItem)
    {
        if (!string.IsNullOrWhiteSpace(playoutItem.CustomTitle))
        {
            return string.Empty;
        }

        return playoutItem.MediaItem switch
        {
            Episode e => e.EpisodeMetadata.HeadOrNone().Match(
                em => em.Title ?? string.Empty,
                () => string.Empty),
            MusicVideo mv => mv.MusicVideoMetadata.HeadOrNone().Match(
                mvm => mvm.Title ?? string.Empty,
                () => string.Empty),
            Song s => s.SongMetadata.HeadOrNone().Match(
                mvm => mvm.Title ?? string.Empty,
                () => string.Empty),
            _ => string.Empty
        };
    }

    private static string GetDescription(PlayoutItem playoutItem)
    {
        if (!string.IsNullOrWhiteSpace(playoutItem.CustomTitle))
        {
            return string.Empty;
        }

        return playoutItem.MediaItem switch
        {
            Movie m => m.MovieMetadata.HeadOrNone().Map(mm => mm.Plot ?? string.Empty).IfNone(string.Empty),
            Episode e => e.EpisodeMetadata.HeadOrNone().Map(em => em.Plot ?? string.Empty)
                .IfNone(string.Empty),
            MusicVideo mv => mv.MusicVideoMetadata.HeadOrNone().Map(mvm => mvm.Plot ?? string.Empty)
                .IfNone(string.Empty),
            _ => string.Empty
        };
    }

    private static Option<ContentRating> GetContentRating(PlayoutItem playoutItem)
    {
        try
        {
            return playoutItem.MediaItem switch
            {
                Movie m => m.MovieMetadata
                    .HeadOrNone()
                    .Match(mm => ParseContentRating(mm.ContentRating, "MPAA"), () => None),
                Episode e => e.Season.Show.ShowMetadata
                    .HeadOrNone()
                    .Match(sm => ParseContentRating(sm.ContentRating, "VCHIP"), () => None),
                _ => None
            };
        }
        catch (Exception ex)
        {
            Log.Logger.Warning(ex, "Failed to get content rating for playout item {Item}", GetTitle(playoutItem));
            return None;
        }
    }

    private static Option<ContentRating> ParseContentRating(string contentRating, string system)
    {
        Option<string> maybeFirst = (contentRating ?? string.Empty).Split('/').HeadOrNone();
        return maybeFirst.Map(
            first =>
            {
                string[] split = first.Split(':');
                if (split.Length == 2)
                {
                    return split[0].ToLowerInvariant() == "us"
                        ? new ContentRating(system, split[1].ToUpperInvariant())
                        : new ContentRating(None, split[1].ToUpperInvariant());
                }

                return string.IsNullOrWhiteSpace(first)
                    ? Option<ContentRating>.None
                    : new ContentRating(None, first);
            }).Flatten();
    }

    private static List<string> GetCategories(string categories) =>
        (categories ?? string.Empty).Split(',')
        .Map(s => s.Trim())
        .Filter(s => !string.IsNullOrWhiteSpace(s))
        .Distinct()
        .ToList();

    private record ContentRating(Option<string> System, string Value);
}
