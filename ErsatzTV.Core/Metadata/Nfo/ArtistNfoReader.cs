using System.Xml;
using Bugsnag;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.Interfaces.Metadata.Nfo;

namespace ErsatzTV.Core.Metadata.Nfo;

public class ArtistNfoReader : NfoReader<ArtistNfo>, IArtistNfoReader
{
    private readonly IClient _client;

    public ArtistNfoReader(IClient client) => _client = client;

    public async Task<Either<BaseError, ArtistNfo>> Read(Stream input)
    {
        try
        {
            var settings = new XmlReaderSettings { Async = true, ConformanceLevel = ConformanceLevel.Fragment };
            using var reader = XmlReader.Create(input, settings);
            ArtistNfo nfo = null;
            var done = false;

            while (!done && await reader.ReadAsync())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (reader.Name.ToLowerInvariant())
                        {
                            case "artist":
                                nfo = new ArtistNfo
                                {
                                    Genres = new List<string>(),
                                    Styles = new List<string>(),
                                    Moods = new List<string>()
                                };
                                break;
                            case "name":
                                await ReadStringContent(reader, nfo, (artist, name) => artist.Name = name);
                                break;
                            case "disambiguation":
                                await ReadStringContent(
                                    reader,
                                    nfo,
                                    (artist, disambiguation) => artist.Disambiguation = disambiguation);
                                break;
                            case "genre":
                                await ReadStringContent(reader, nfo, (artist, genre) => artist.Genres.Add(genre));
                                break;
                            case "style":
                                await ReadStringContent(reader, nfo, (artist, style) => artist.Styles.Add(style));
                                break;
                            case "mood":
                                await ReadStringContent(reader, nfo, (artist, mood) => artist.Moods.Add(mood));
                                break;
                            case "biography":
                                await ReadStringContent(
                                    reader,
                                    nfo,
                                    (artist, biography) => artist.Biography = biography);
                                break;
                        }

                        break;
                    case XmlNodeType.EndElement:
                        if (reader.Name == "artist")
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
