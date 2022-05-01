using System.Xml;
using Bugsnag;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.Interfaces.Metadata.Nfo;

namespace ErsatzTV.Core.Metadata.Nfo;

public class EpisodeNfoReader : NfoReader<TvShowEpisodeNfo>, IEpisodeNfoReader
{
    private readonly IClient _client;

    public EpisodeNfoReader(IClient client) => _client = client;

    public async Task<Either<BaseError, List<TvShowEpisodeNfo>>> Read(Stream input)
    {
        try
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
                                await ReadStringContent(
                                    reader,
                                    nfo,
                                    (episode, showTitle) => episode.ShowTitle = showTitle);
                                break;
                            case "episode":
                                await ReadIntContent(
                                    reader,
                                    nfo,
                                    (episode, episodeNumber) => episode.Episode = episodeNumber);
                                break;
                            case "season":
                                await ReadIntContent(
                                    reader,
                                    nfo,
                                    (episode, seasonNumber) => episode.Season = seasonNumber);
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
                                await ReadDateTimeContent(reader, nfo, (episode, aired) => episode.Aired = aired);
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
        catch (Exception ex)
        {
            _client.Notify(ex);
            return new FailedToReadNfo(ex.ToString());
        }
    }
}
