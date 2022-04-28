using System.Xml;
using System.Xml.Linq;
using ErsatzTV.Core.Interfaces.Metadata.Nfo;

namespace ErsatzTV.Core.Metadata.Nfo;

public class EpisodeNfoReader : IEpisodeNfoReader
{
    public async Task<List<TvShowEpisodeNfo>> Read(Stream input)
    {
        var result = new List<TvShowEpisodeNfo>();

        var settings = new XmlReaderSettings { Async = true, ConformanceLevel = ConformanceLevel.Fragment };
        using var reader = XmlReader.Create(input, settings);
        TvShowEpisodeNfo nfo = null;

        while (await reader.ReadAsync())
        {
            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                    switch (reader.Name.ToLowerInvariant())
                    {
                        case "episodedetails":
                            nfo = new TvShowEpisodeNfo
                            {
                                UniqueIds = new List<UniqueIdNfo>(),
                                Actors = new List<ActorNfo>(),
                                Writers = new List<string>(),
                                Directors = new List<string>()
                            };
                            break;
                        case "title":
                            await ReadTitle(reader, nfo);
                            break;
                        case "showtitle":
                            await ReadShowTitle(reader, nfo);
                            break;
                        case "episode":
                            await ReadEpisode(reader, nfo);
                            break;
                        case "season":
                            await ReadSeason(reader, nfo);
                            break;
                        case "uniqueid":
                            await ReadUniqueId(reader, nfo);
                            break;
                        case "mpaa":
                            await ReadContentRating(reader, nfo);
                            break;
                        case "aired":
                            // TODO: parse the date here
                            await ReadAired(reader, nfo);
                            break;
                        case "plot":
                            await ReadPlot(reader, nfo);
                            break;
                        case "actor":
                            ReadActor(reader, nfo);
                            break;
                        case "credits":
                            await ReadWriter(reader, nfo);
                            break;
                        case "director":
                            await ReadDirector(reader, nfo);
                            break;
                    }

                    break;
                case XmlNodeType.EndElement:
                    switch (reader.Name.ToLowerInvariant())
                    {
                        case "episodedetails":
                            if (nfo != null)
                            {
                                result.Add(nfo);
                            }

                            break;
                    }

                    break;
            }
        }

        return result;
    }

    private static async Task ReadTitle(XmlReader reader, TvShowEpisodeNfo nfo)
    {
        if (nfo != null)
        {
            nfo.Title = await reader.ReadElementContentAsStringAsync();
        }
    }

    private static async Task ReadShowTitle(XmlReader reader, TvShowEpisodeNfo nfo)
    {
        if (nfo != null)
        {
            nfo.ShowTitle = await reader.ReadElementContentAsStringAsync();
        }
    }

    private static async Task ReadEpisode(XmlReader reader, TvShowEpisodeNfo nfo)
    {
        if (nfo != null)
        {
            bool _ = int.TryParse(await reader.ReadElementContentAsStringAsync(), out int episode);
            nfo.Episode = episode;
        }
    }

    private static async Task ReadSeason(XmlReader reader, TvShowEpisodeNfo nfo)
    {
        if (nfo != null)
        {
            bool _ = int.TryParse(await reader.ReadElementContentAsStringAsync(), out int season);
            nfo.Season = season;
        }
    }

    private static async Task ReadUniqueId(XmlReader reader, TvShowEpisodeNfo nfo)
    {
        if (nfo != null)
        {
            var uniqueId = new UniqueIdNfo();
            reader.MoveToAttribute("default");
            uniqueId.Default = bool.TryParse(reader.Value, out bool def) && def;
            reader.MoveToAttribute("type");
            uniqueId.Type = reader.Value;
            reader.MoveToElement();
            uniqueId.Guid = await reader.ReadElementContentAsStringAsync();

            nfo.UniqueIds.Add(uniqueId);
        }
    }

    private static async Task ReadContentRating(XmlReader reader, TvShowEpisodeNfo nfo)
    {
        if (nfo != null)
        {
            nfo.ContentRating = await reader.ReadElementContentAsStringAsync();
        }
    }

    private static async Task ReadAired(XmlReader reader, TvShowEpisodeNfo nfo)
    {
        if (nfo != null)
        {
            nfo.Aired = await reader.ReadElementContentAsStringAsync();
        }
    }

    private static async Task ReadPlot(XmlReader reader, TvShowEpisodeNfo nfo)
    {
        if (nfo != null)
        {
            nfo.Plot = await reader.ReadElementContentAsStringAsync();
        }
    }

    private static void ReadActor(XmlReader reader, TvShowEpisodeNfo nfo)
    {
        if (nfo != null)
        {
            var actor = new ActorNfo();
            var element = (XElement)XNode.ReadFrom(reader);

            XElement name = element.Element("name");
            if (name != null)
            {
                actor.Name = name.Value;
            }

            XElement role = element.Element("role");
            if (role != null)
            {
                actor.Role = role.Value;
            }

            XElement thumb = element.Element("thumb");
            if (thumb != null)
            {
                actor.Thumb = thumb.Value;
            }

            nfo.Actors.Add(actor);
        }
    }

    private static async Task ReadWriter(XmlReader reader, TvShowEpisodeNfo nfo) =>
        nfo?.Writers.Add(await reader.ReadElementContentAsStringAsync());

    private static async Task ReadDirector(XmlReader reader, TvShowEpisodeNfo nfo) =>
        nfo?.Directors.Add(await reader.ReadElementContentAsStringAsync());
}
