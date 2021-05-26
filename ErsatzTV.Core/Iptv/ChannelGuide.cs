using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Emby;
using ErsatzTV.Core.Jellyfin;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.Iptv
{
    public class ChannelGuide
    {
        private readonly List<Channel> _channels;
        private readonly string _host;
        private readonly string _scheme;

        public ChannelGuide(string scheme, string host, List<Channel> channels)
        {
            _scheme = scheme;
            _host = host;
            _channels = channels;
        }

        public string ToXml()
        {
            using var ms = new MemoryStream();
            using var xml = XmlWriter.Create(ms);
            xml.WriteStartDocument();

            xml.WriteStartElement("tv");
            xml.WriteAttributeString("generator-info-name", "ersatztv");

            foreach (Channel channel in _channels.OrderBy(c => decimal.Parse(c.Number)))
            {
                xml.WriteStartElement("channel");
                xml.WriteAttributeString("id", channel.Number);

                xml.WriteStartElement("display-name");
                xml.WriteAttributeString("lang", "en");
                xml.WriteString(channel.Name);
                xml.WriteEndElement(); // display-name

                xml.WriteStartElement("icon");
                string logo = Optional(channel.Artwork).Flatten()
                    .Filter(a => a.ArtworkKind == ArtworkKind.Logo)
                    .HeadOrNone()
                    .Match(
                        artwork => $"{_scheme}://{_host}/iptv/logos/{artwork.Path}",
                        () => $"{_scheme}://{_host}/iptv/images/ersatztv-500.png");
                xml.WriteAttributeString("src", logo);
                xml.WriteEndElement(); // icon

                xml.WriteEndElement(); // channel
            }

            foreach (Channel channel in _channels.OrderBy(c => c.Number))
            {
                var sorted = channel.Playouts.Collect(p => p.Items).OrderBy(x => x.Start).ToList();
                var i = 0;
                while (i < sorted.Count)
                {
                    PlayoutItem startItem = sorted[i];
                    bool hasCustomTitle = !string.IsNullOrWhiteSpace(startItem.CustomTitle);

                    int finishIndex = i;
                    while (hasCustomTitle && finishIndex + 1 < sorted.Count && sorted[finishIndex + 1].CustomGroup)
                    {
                        finishIndex++;
                    }

                    PlayoutItem finishItem = sorted[finishIndex];
                    i = finishIndex;

                    string start = startItem.StartOffset.ToString("yyyyMMddHHmmss zzz").Replace(":", string.Empty);
                    string stop = finishItem.FinishOffset.ToString("yyyyMMddHHmmss zzz").Replace(":", string.Empty);

                    string title = GetTitle(startItem);
                    string subtitle = GetSubtitle(startItem);
                    string description = GetDescription(startItem);
                    string contentRating = string.Empty;

                    xml.WriteStartElement("programme");
                    xml.WriteAttributeString("start", start);
                    xml.WriteAttributeString("stop", stop);
                    xml.WriteAttributeString("channel", channel.Number);

                    if (!hasCustomTitle && startItem.MediaItem is Movie movie)
                    {
                        xml.WriteStartElement("category");
                        xml.WriteAttributeString("lang", "en");
                        xml.WriteString("Movie");
                        xml.WriteEndElement(); // category

                        Option<MovieMetadata> maybeMetadata = movie.MovieMetadata.HeadOrNone();
                        if (maybeMetadata.IsSome)
                        {
                            MovieMetadata metadata = maybeMetadata.ValueUnsafe();

                            if (metadata.Year.HasValue)
                            {
                                xml.WriteStartElement("date");
                                xml.WriteString(metadata.Year.Value.ToString());
                                xml.WriteEndElement(); // date
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

                    xml.WriteStartElement("previously-shown");
                    xml.WriteEndElement(); // previously-shown

                    if (!hasCustomTitle && startItem.MediaItem is Episode episode)
                    {
                        Option<ShowMetadata> maybeMetadata =
                            Optional(episode.Season?.Show?.ShowMetadata.HeadOrNone()).Flatten();
                        if (maybeMetadata.IsSome)
                        {
                            ShowMetadata metadata = maybeMetadata.ValueUnsafe();
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

                        int s = Optional(episode.Season?.SeasonNumber).IfNone(0);
                        int e = episode.EpisodeNumber;
                        if (s > 0 && e > 0)
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

                    // sb.AppendLine("<icon src=\"\"/>");

                    if (!string.IsNullOrWhiteSpace(description))
                    {
                        xml.WriteStartElement("desc");
                        xml.WriteAttributeString("lang", "en");
                        xml.WriteString(description);
                        xml.WriteEndElement(); // desc
                    }

                    if (!string.IsNullOrWhiteSpace(contentRating))
                    {
                        xml.WriteStartElement("rating");
                        xml.WriteAttributeString("system", "MPAA");
                        xml.WriteStartElement("value");
                        xml.WriteString(contentRating);
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

                artworkPath = $"{_scheme}://{_host}/iptv/artwork/{artworkFolder}/{artwork.Path}";
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
    }
}
