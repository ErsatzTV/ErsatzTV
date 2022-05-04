using System.Xml;
using Bugsnag;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.Interfaces.Metadata.Nfo;

namespace ErsatzTV.Core.Metadata.Nfo;

public class MusicVideoNfoReader : NfoReader<MusicVideoNfo>, IMusicVideoNfoReader
{
    private readonly IClient _client;

    public MusicVideoNfoReader(IClient client) => _client = client;

    public async Task<Either<BaseError, MusicVideoNfo>> Read(Stream input)
    {
        try
        {
            var settings = new XmlReaderSettings { Async = true, ConformanceLevel = ConformanceLevel.Fragment };
            using var reader = XmlReader.Create(input, settings);
            MusicVideoNfo nfo = null;
            var done = false;

            while (!done && await reader.ReadAsync())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (reader.Name.ToLowerInvariant())
                        {
                            case "musicvideo":
                                nfo = new MusicVideoNfo
                                {
                                    Artists = new List<string>(),
                                    Genres = new List<string>(),
                                    Tags = new List<string>(),
                                    Studios = new List<string>()
                                };
                                break;
                            case "artist":
                                await ReadStringContent(
                                    reader,
                                    nfo,
                                    (musicVideo, artist) => musicVideo.Artists.Add(artist));
                                break;
                            case "title":
                                await ReadStringContent(reader, nfo, (musicVideo, title) => musicVideo.Title = title);
                                break;
                            case "album":
                                await ReadStringContent(reader, nfo, (musicVideo, album) => musicVideo.Album = album);
                                break;
                            case "plot":
                                await ReadStringContent(reader, nfo, (musicVideo, plot) => musicVideo.Plot = plot);
                                break;
                            case "year":
                                await ReadIntContent(reader, nfo, (musicVideo, year) => musicVideo.Year = year);
                                break;
                            case "aired":
                                await ReadDateTimeContent(reader, nfo, (show, aired) => show.Aired = aired);
                                break;
                            case "genre":
                                await ReadStringContent(
                                    reader,
                                    nfo,
                                    (musicVideo, genre) => musicVideo.Genres.Add(genre));
                                break;
                            case "tag":
                                await ReadStringContent(reader, nfo, (musicVideo, tag) => musicVideo.Tags.Add(tag));
                                break;
                            case "studio":
                                await ReadStringContent(
                                    reader,
                                    nfo,
                                    (musicVideo, studio) => musicVideo.Studios.Add(studio));
                                break;
                        }

                        break;
                    case XmlNodeType.EndElement:
                        if (reader.Name == "musicvideo")
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
