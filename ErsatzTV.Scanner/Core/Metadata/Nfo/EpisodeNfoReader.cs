using System.Xml;
using Bugsnag;
using ErsatzTV.Core;
using ErsatzTV.Core.Errors;
using ErsatzTV.Scanner.Core.Interfaces.Metadata.Nfo;
using Microsoft.Extensions.Logging;
using Microsoft.IO;

namespace ErsatzTV.Scanner.Core.Metadata.Nfo;

public class EpisodeNfoReader : NfoReader<EpisodeNfo>, IEpisodeNfoReader
{
    private readonly IClient _client;
    private readonly ILogger<EpisodeNfoReader> _logger;

    public EpisodeNfoReader(
        RecyclableMemoryStreamManager recyclableMemoryStreamManager,
        IClient client,
        ILogger<EpisodeNfoReader> logger)
        : base(recyclableMemoryStreamManager, logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<Either<BaseError, List<EpisodeNfo>>> ReadFromFile(string fileName)
    {
        // ReSharper disable once ConvertToUsingDeclaration
        await using (Stream s = await SanitizedStreamForFile(fileName))
        {
            return await Read(s);
        }
    }

    internal async Task<Either<BaseError, List<EpisodeNfo>>> Read(Stream input)
    {
        var result = new List<EpisodeNfo>();

        try
        {
            using var reader = XmlReader.Create(input, Settings);
            EpisodeNfo? nfo = null;

            while (await reader.ReadAsync())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (reader.Name.ToLowerInvariant())
                        {
                            case "episodedetails":
                                nfo = new EpisodeNfo();
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
        catch (XmlException)
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
