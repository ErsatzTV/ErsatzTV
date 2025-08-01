﻿using System.Xml;
using Bugsnag;
using ErsatzTV.Core;
using ErsatzTV.Core.Errors;
using ErsatzTV.Scanner.Core.Interfaces.Metadata.Nfo;
using Microsoft.Extensions.Logging;
using Microsoft.IO;

namespace ErsatzTV.Scanner.Core.Metadata.Nfo;

public class MovieNfoReader : NfoReader<MovieNfo>, IMovieNfoReader
{
    private readonly IClient _client;
    private readonly ILogger<MovieNfoReader> _logger;

    public MovieNfoReader(
        RecyclableMemoryStreamManager recyclableMemoryStreamManager,
        IClient client,
        ILogger<MovieNfoReader> logger)
        : base(recyclableMemoryStreamManager, logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<Either<BaseError, MovieNfo>> ReadFromFile(string fileName)
    {
        // ReSharper disable once ConvertToUsingDeclaration
        await using (Stream s = await SanitizedStreamForFile(fileName))
        {
            return await Read(s, fileName);
        }
    }

    internal async Task<Either<BaseError, MovieNfo>> Read(Stream input, string fileName)
    {
        MovieNfo? nfo = null;

        try
        {
            var settings = new XmlReaderSettings { Async = true, ConformanceLevel = ConformanceLevel.Fragment };
            using var reader = XmlReader.Create(input, settings);
            var done = false;

            while (!done && await reader.ReadAsync())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (reader.Name.ToLowerInvariant())
                        {
                            case "movie":
                                nfo = new MovieNfo();
                                break;
                            case "title":
                                await ReadStringContent(reader, nfo, (movie, title) => movie.Title = title, fileName);
                                break;
                            case "sorttitle":
                                await ReadStringContent(
                                    reader,
                                    nfo,
                                    (movie, sortTitle) => movie.SortTitle = sortTitle,
                                    fileName);
                                break;
                            case "outline":
                                await ReadStringContent(
                                    reader,
                                    nfo,
                                    (movie, outline) => movie.Outline = outline,
                                    fileName);
                                break;
                            case "year":
                                await ReadIntContent(reader, nfo, (movie, year) => movie.Year = year, fileName);
                                break;
                            case "mpaa":
                                await ReadStringContent(
                                    reader,
                                    nfo,
                                    (movie, contentRating) => movie.ContentRating = contentRating,
                                    fileName);
                                break;
                            case "premiered":
                                await ReadDateTimeContent(
                                    reader,
                                    nfo,
                                    (movie, premiered) => movie.Premiered = premiered,
                                    fileName);
                                break;
                            case "plot":
                                await ReadStringContent(reader, nfo, (movie, plot) => movie.Plot = plot, fileName);
                                break;
                            case "genre":
                                await ReadStringContent(
                                    reader,
                                    nfo,
                                    (movie, genre) => movie.Genres.Add(genre),
                                    fileName);
                                break;
                            case "tag":
                                await ReadStringContent(reader, nfo, (movie, tag) => movie.Tags.Add(tag), fileName);
                                break;
                            case "country":
                                await ReadStringContent(
                                    reader,
                                    nfo,
                                    (movie, country) => movie.Countries.Add(country),
                                    fileName);
                                break;
                            case "studio":
                                await ReadStringContent(
                                    reader,
                                    nfo,
                                    (movie, studio) => movie.Studios.Add(studio),
                                    fileName);
                                break;
                            case "actor":
                                ReadActor(reader, nfo, (movie, actor) => movie.Actors.Add(actor), fileName);
                                break;
                            case "credits":
                                await ReadStringContent(
                                    reader,
                                    nfo,
                                    (movie, writer) => movie.Writers.Add(writer),
                                    fileName);
                                break;
                            case "director":
                                await ReadStringContent(
                                    reader,
                                    nfo,
                                    (movie, director) => movie.Directors.Add(director),
                                    fileName);
                                break;
                            case "uniqueid":
                                await ReadUniqueId(
                                    reader,
                                    nfo,
                                    (movie, uniqueid) => movie.UniqueIds.Add(uniqueid),
                                    fileName);
                                break;
                        }

                        break;
                    case XmlNodeType.EndElement:
                        if (reader.Name == "movie")
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
