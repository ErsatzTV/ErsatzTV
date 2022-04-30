using System.Xml;
using ErsatzTV.Core.Interfaces.Metadata.Nfo;

namespace ErsatzTV.Core.Metadata.Nfo;

public class EpisodeNfoReader : NfoReader<TvShowEpisodeNfo>, IEpisodeNfoReader
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
                            await ReadStringContent(reader, nfo, (episode, title) => episode.Title = title);
                            break;
                        case "showtitle":
                            await ReadStringContent(reader, nfo, (episode, showTitle) => episode.ShowTitle = showTitle);
                            break;
                        case "episode":
                            await ReadIntContent(
                                reader,
                                nfo,
                                (episode, episodeNumber) => episode.Episode = episodeNumber);
                            break;
                        case "season":
                            await ReadIntContent(reader, nfo, (episode, seasonNumber) => episode.Season = seasonNumber);
                            break;
                        case "uniqueid":
                            await ReadUniqueId(reader, nfo, (episode, uniqueId) => episode.UniqueIds.Add(uniqueId));
                            break;
                        case "mpaa":
                            await ReadStringContent(
                                reader,
                                nfo,
                                (episode, contentRating) => episode.ContentRating = contentRating);
                            break;
                        case "aired":
                            // TODO: parse the date here
                            await ReadAired(reader, nfo);
                            break;
                        case "plot":
                            await ReadStringContent(reader, nfo, (episode, plot) => episode.Plot = plot);
                            break;
                        case "actor":
                            ReadActor(reader, nfo, (episode, actor) => episode.Actors.Add(actor));
                            break;
                        case "credits":
                            await ReadStringContent(reader, nfo, (episode, writer) => episode.Writers.Add(writer));
                            break;
                        case "director":
                            await ReadStringContent(
                                reader,
                                nfo,
                                (episode, director) => episode.Directors.Add(director));
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

    private static async Task ReadAired(XmlReader reader, TvShowEpisodeNfo nfo)
    {
        if (nfo != null)
        {
            nfo.Aired = await reader.ReadElementContentAsStringAsync();
        }
    }
}
