using System.Xml;
using Bugsnag;
using ErsatzTV.Core;
using ErsatzTV.Core.Errors;
using ErsatzTV.Scanner.Core.Interfaces.Metadata.Nfo;
using Microsoft.Extensions.Logging;
using Microsoft.IO;

namespace ErsatzTV.Scanner.Core.Metadata.Nfo;

public class ShowNfoReader : NfoReader<ShowNfo>, IShowNfoReader
{
    private readonly IClient _client;
    private readonly ILogger<ShowNfoReader> _logger;

    public ShowNfoReader(
        RecyclableMemoryStreamManager recyclableMemoryStreamManager,
        IClient client,
        ILogger<ShowNfoReader> logger)
        : base(recyclableMemoryStreamManager, logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<Either<BaseError, ShowNfo>> ReadFromFile(string fileName)
    {
        // ReSharper disable once ConvertToUsingDeclaration
        await using (Stream s = await SanitizedStreamForFile(fileName))
        {
            return await Read(s, fileName);
        }
    }

    internal async Task<Either<BaseError, ShowNfo>> Read(Stream input, string fileName)
    {
        ShowNfo? nfo = null;

        try
        {
            var settings = new XmlReaderSettings { Async = true, ConformanceLevel = ConformanceLevel.Fragment };
            using var reader = XmlReader.Create(input, settings);
            var done = false;
            var showDepth = 0;

            while (!done && await reader.ReadAsync())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (reader.Name.ToLowerInvariant())
                        {
                            case "tvshow":
                                nfo = new ShowNfo();
                                showDepth = reader.Depth;
                                break;
                            case "title":
                                if (reader.Depth == showDepth + 1)
                                {
                                    await ReadStringContent(reader, nfo, (show, title) => show.Title = title, fileName);
                                }

                                break;
                            case "year":
                                await ReadIntContent(reader, nfo, (show, year) => show.Year = year, fileName);
                                break;
                            case "plot":
                                await ReadStringContent(reader, nfo, (show, plot) => show.Plot = plot, fileName);
                                break;
                            case "outline":
                                await ReadStringContent(
                                    reader,
                                    nfo,
                                    (show, outline) => show.Outline = outline,
                                    fileName);
                                break;
                            case "tagline":
                                await ReadStringContent(
                                    reader,
                                    nfo,
                                    (show, tagline) => show.Tagline = tagline,
                                    fileName);
                                break;
                            case "mpaa":
                                await ReadStringContent(
                                    reader,
                                    nfo,
                                    (show, contentRating) => show.ContentRating = contentRating,
                                    fileName);
                                break;
                            case "premiered":
                                await ReadDateTimeContent(
                                    reader,
                                    nfo,
                                    (show, premiered) => show.Premiered = premiered,
                                    fileName);
                                break;
                            case "genre":
                                await ReadStringContent(reader, nfo, (show, genre) => show.Genres.Add(genre), fileName);
                                break;
                            case "tag":
                                await ReadStringContent(reader, nfo, (show, tag) => show.Tags.Add(tag), fileName);
                                break;
                            case "studio":
                                await ReadStringContent(
                                    reader,
                                    nfo,
                                    (show, studio) => show.Studios.Add(studio),
                                    fileName);
                                break;
                            case "actor":
                                ReadActor(reader, nfo, (episode, actor) => episode.Actors.Add(actor), fileName);
                                break;
                            case "uniqueid":
                                await ReadUniqueId(
                                    reader,
                                    nfo,
                                    (episode, uniqueid) => episode.UniqueIds.Add(uniqueid),
                                    fileName);
                                break;
                        }

                        break;
                    case XmlNodeType.EndElement:
                        if (reader.Name == "tvshow")
                        {
                            done = true;
                        }

                        break;
                }
            }

            return Optional(nfo).ToEither<BaseError>(new FailedToReadNfo());
        }
        catch (XmlException)
        {
            _logger.LogWarning("Invalid XML detected in file {FileName}; returning incomplete metadata", fileName);
            return Optional(nfo).ToEither<BaseError>(new FailedToReadNfo());
        }
        catch (Exception ex)
        {
            _client.Notify(ex);
            return new FailedToReadNfo(ex.ToString());
        }
    }
}
