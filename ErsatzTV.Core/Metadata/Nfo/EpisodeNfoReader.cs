using System.Xml;
using Bugsnag;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.Interfaces.Metadata.Nfo;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Metadata.Nfo;

public class EpisodeNfoReader : NfoReader<TvShowEpisodeNfo>, IEpisodeNfoReader
{
    private readonly IClient _client;
    private readonly ILogger<EpisodeNfoReader> _logger;

    public EpisodeNfoReader(IClient client, ILogger<EpisodeNfoReader> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<Either<BaseError, List<TvShowEpisodeNfo>>> Read(Stream input)
    {
        var result = new List<TvShowEpisodeNfo>();

        try
        {
            using var reader = XmlReader.Create(input, Settings);
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
                                // immediately add so we have something to return if we encounter invalid characters
                                result.Add(nfo);
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
                }
            }

            return result;
        }
        catch (XmlException ex) when (ex.Message.Contains(
                                          "invalid character",
                                          StringComparison.InvariantCultureIgnoreCase))
        {
            _logger.LogWarning("Invalid XML detected; returning incomplete metadata");
            return result;
        }
        catch (Exception ex)
        {
            _client.Notify(ex);
            return new FailedToReadNfo(ex.ToString());
        }
    }
}
