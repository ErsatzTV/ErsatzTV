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
            return await Read(s, fileName);
        }
    }

    internal async Task<Either<BaseError, List<EpisodeNfo>>> Read(Stream input, string fileName)
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
                                await ReadStringContent(
                                    reader,
                                    nfo,
                                    (episode, title) => episode.Title = title,
                                    fileName);
                                break;
                            case "showtitle":
                                await ReadStringContent(
                                    reader,
                                    nfo,
                                    (episode, showTitle) => episode.ShowTitle = showTitle,
                                    fileName);
                                break;
                            case "episode":
                                await ReadIntContent(
                                    reader,
                                    nfo,
                                    (episode, episodeNumber) => episode.Episode = episodeNumber,
                                    fileName);
                                break;
                            case "season":
                                await ReadIntContent(
                                    reader,
                                    nfo,
                                    (episode, seasonNumber) => episode.Season = seasonNumber,
                                    fileName);
                                break;
                            case "uniqueid":
                                await ReadUniqueId(
                                    reader,
                                    nfo,
                                    (episode, uniqueId) => episode.UniqueIds.Add(uniqueId),
                                    fileName);
                                break;
                            case "mpaa":
                                await ReadStringContent(
                                    reader,
                                    nfo,
                                    (episode, contentRating) => episode.ContentRating = contentRating,
                                    fileName);
                                break;
                            case "aired":
                                await ReadDateTimeContent(
                                    reader,
                                    nfo,
                                    (episode, aired) => episode.Aired = aired,
                                    fileName);
                                break;
                            case "plot":
                                await ReadStringContent(reader, nfo, (episode, plot) => episode.Plot = plot, fileName);
                                break;
                            case "genre":
                                await ReadStringContent(
                                    reader,
                                    nfo,
                                    (episode, genre) => episode.Genres.Add(genre),
                                    fileName);
                                break;
                            case "tag":
                                await ReadStringContent(reader, nfo, (episode, tag) => episode.Tags.Add(tag), fileName);
                                break;
                            case "actor":
                                ReadActor(reader, nfo, (episode, actor) => episode.Actors.Add(actor), fileName);
                                break;
                            case "credits":
                                await ReadStringContent(
                                    reader,
                                    nfo,
                                    (episode, writer) => episode.Writers.Add(writer),
                                    fileName);
                                break;
                            case "director":
                                await ReadStringContent(
                                    reader,
                                    nfo,
                                    (episode, director) => episode.Directors.Add(director),
                                    fileName);
                                break;
                        }

                        break;
                }
            }

            return result;
        }
        catch (XmlException)
        {
            _logger.LogWarning("Invalid XML detected in file {FileName}; returning incomplete metadata", fileName);
            return result;
        }
        catch (Exception ex)
        {
            _client.Notify(ex);
            return new FailedToReadNfo(ex.ToString());
        }
    }
}
