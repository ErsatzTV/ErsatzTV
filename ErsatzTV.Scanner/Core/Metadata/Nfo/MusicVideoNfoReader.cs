using System.Xml;
using Bugsnag;
using ErsatzTV.Core;
using ErsatzTV.Core.Errors;
using ErsatzTV.Scanner.Core.Interfaces.Metadata.Nfo;
using Microsoft.Extensions.Logging;
using Microsoft.IO;

namespace ErsatzTV.Scanner.Core.Metadata.Nfo;

public class MusicVideoNfoReader : NfoReader<MusicVideoNfo>, IMusicVideoNfoReader
{
    private readonly IClient _client;
    private readonly ILogger<MusicVideoNfoReader> _logger;

    public MusicVideoNfoReader(
        RecyclableMemoryStreamManager recyclableMemoryStreamManager,
        IClient client,
        ILogger<MusicVideoNfoReader> logger)
        : base(recyclableMemoryStreamManager, logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<Either<BaseError, MusicVideoNfo>> ReadFromFile(string fileName)
    {
        // ReSharper disable once ConvertToUsingDeclaration
        await using (Stream s = await SanitizedStreamForFile(fileName))
        {
            return await Read(s);
        }
    }

    internal async Task<Either<BaseError, MusicVideoNfo>> Read(Stream input)
    {
        MusicVideoNfo? nfo = null;

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
                            case "musicvideo":
                                nfo = new MusicVideoNfo();
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
                            case "track":
                                await ReadIntContent(reader, nfo, (musicVideo, track) => musicVideo.Track = track);
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
                            case "director":
                                await ReadStringContent(
                                    reader,
                                    nfo,
                                    (musicVideo, director) => musicVideo.Directors.Add(director));
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
        catch (XmlException)
        {
            _logger.LogWarning("Invalid XML detected; returning incomplete metadata");
            return Optional(nfo).ToEither((BaseError)new FailedToReadNfo());
        }
        catch (Exception ex)
        {
            _client.Notify(ex);
            return new FailedToReadNfo(ex.ToString());
        }
    }
}
