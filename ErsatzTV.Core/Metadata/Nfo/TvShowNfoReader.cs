using System.Xml;
using Bugsnag;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.Interfaces.Metadata.Nfo;

namespace ErsatzTV.Core.Metadata.Nfo;

public class TvShowNfoReader : NfoReader<TvShowNfo>, ITvShowNfoReader
{
    private readonly IClient _client;

    public TvShowNfoReader(IClient client) => _client = client;

    public async Task<Either<BaseError, TvShowNfo>> Read(Stream input)
    {
        try
        {
            var settings = new XmlReaderSettings { Async = true, ConformanceLevel = ConformanceLevel.Fragment };
            using var reader = XmlReader.Create(input, settings);
            TvShowNfo nfo = null;
            var done = false;

            while (!done && await reader.ReadAsync())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (reader.Name.ToLowerInvariant())
                        {
                            case "tvshow":
                                nfo = new TvShowNfo
                                {
                                    Genres = new List<string>(),
                                    Tags = new List<string>(),
                                    Studios = new List<string>(),
                                    Actors = new List<ActorNfo>(),
                                    UniqueIds = new List<UniqueIdNfo>()
                                };
                                break;
                            case "title":
                                await ReadStringContent(reader, nfo, (show, title) => show.Title = title);
                                break;
                            case "year":
                                await ReadIntContent(reader, nfo, (show, year) => show.Year = year);
                                break;
                            case "plot":
                                await ReadStringContent(reader, nfo, (show, plot) => show.Plot = plot);
                                break;
                            case "outline":
                                await ReadStringContent(reader, nfo, (show, outline) => show.Outline = outline);
                                break;
                            case "tagline":
                                await ReadStringContent(reader, nfo, (show, tagline) => show.Tagline = tagline);
                                break;
                            case "mpaa":
                                await ReadStringContent(
                                    reader,
                                    nfo,
                                    (show, contentRating) => show.ContentRating = contentRating);
                                break;
                            case "premiered":
                                await ReadDateTimeContent(reader, nfo, (show, premiered) => show.Premiered = premiered);
                                break;
                            case "genre":
                                await ReadStringContent(reader, nfo, (show, genre) => show.Genres.Add(genre));
                                break;
                            case "tag":
                                await ReadStringContent(reader, nfo, (show, tag) => show.Tags.Add(tag));
                                break;
                            case "studio":
                                await ReadStringContent(reader, nfo, (show, studio) => show.Studios.Add(studio));
                                break;
                            case "actor":
                                ReadActor(reader, nfo, (episode, actor) => episode.Actors.Add(actor));
                                break;
                            case "uniqueid":
                                await ReadUniqueId(reader, nfo, (episode, uniqueid) => episode.UniqueIds.Add(uniqueid));
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

            return Optional(nfo).ToEither((BaseError)new FailedToReadNfo());
        }
        catch (Exception ex)
        {
            _client.Notify(ex);
            return new FailedToReadNfo(ex.ToString());
        }
    }
}
