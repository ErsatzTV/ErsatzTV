﻿using System.Xml;
using Bugsnag;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.Interfaces.Metadata.Nfo;

namespace ErsatzTV.Core.Metadata.Nfo;

public class MovieNfoReader : NfoReader<MovieNfo>, IMovieNfoReader
{
    private readonly IClient _client;

    public MovieNfoReader(IClient client) => _client = client;

    public async Task<Either<BaseError, MovieNfo>> Read(Stream input)
    {
        try
        {
            var settings = new XmlReaderSettings { Async = true, ConformanceLevel = ConformanceLevel.Fragment };
            using var reader = XmlReader.Create(input, settings);
            MovieNfo nfo = null;
            var done = false;

            while (!done && await reader.ReadAsync())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (reader.Name.ToLowerInvariant())
                        {
                            case "movie":
                                nfo = new MovieNfo
                                {
                                    Genres = new List<string>(),
                                    Tags = new List<string>(),
                                    Studios = new List<string>(),
                                    Actors = new List<ActorNfo>(),
                                    Writers = new List<string>(),
                                    Directors = new List<string>(),
                                    UniqueIds = new List<UniqueIdNfo>()
                                };
                                break;
                            case "title":
                                await ReadStringContent(reader, nfo, (movie, title) => movie.Title = title);
                                break;
                            case "sorttitle":
                                await ReadStringContent(reader, nfo, (movie, sortTitle) => movie.SortTitle = sortTitle);
                                break;
                            case "outline":
                                await ReadStringContent(reader, nfo, (movie, outline) => movie.Outline = outline);
                                break;
                            case "year":
                                await ReadIntContent(reader, nfo, (movie, year) => movie.Year = year);
                                break;
                            case "mpaa":
                                await ReadStringContent(
                                    reader,
                                    nfo,
                                    (movie, contentRating) => movie.ContentRating = contentRating);
                                break;
                            case "premiered":
                                await ReadDateTimeContent(
                                    reader,
                                    nfo,
                                    (movie, premiered) => movie.Premiered = premiered);
                                break;
                            case "plot":
                                await ReadStringContent(reader, nfo, (movie, plot) => movie.Plot = plot);
                                break;
                            case "genre":
                                await ReadStringContent(reader, nfo, (movie, genre) => movie.Genres.Add(genre));
                                break;
                            case "tag":
                                await ReadStringContent(reader, nfo, (movie, tag) => movie.Tags.Add(tag));
                                break;
                            case "studio":
                                await ReadStringContent(reader, nfo, (movie, studio) => movie.Studios.Add(studio));
                                break;
                            case "actor":
                                ReadActor(reader, nfo, (movie, actor) => movie.Actors.Add(actor));
                                break;
                            case "credits":
                                await ReadStringContent(reader, nfo, (movie, writer) => movie.Writers.Add(writer));
                                break;
                            case "director":
                                await ReadStringContent(
                                    reader,
                                    nfo,
                                    (movie, director) => movie.Directors.Add(director));
                                break;
                            case "uniqueid":
                                await ReadUniqueId(reader, nfo, (movie, uniqueid) => movie.UniqueIds.Add(uniqueid));
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

            return Optional(nfo).ToEither((BaseError)new FailedToReadNfo());
        }
        catch (Exception ex)
        {
            _client.Notify(ex);
            return new FailedToReadNfo(ex.ToString());
        }
    }
}
