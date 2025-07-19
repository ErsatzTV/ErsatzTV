using System.Xml;
using Bugsnag;
using ErsatzTV.Core;
using ErsatzTV.Core.Errors;
using ErsatzTV.Scanner.Core.Interfaces.Metadata.Nfo;
using Microsoft.Extensions.Logging;
using Microsoft.IO;

namespace ErsatzTV.Scanner.Core.Metadata.Nfo;

public class ArtistNfoReader : NfoReader<ArtistNfo>, IArtistNfoReader
{
    private readonly IClient _client;
    private readonly ILogger<ArtistNfoReader> _logger;

    public ArtistNfoReader(
        RecyclableMemoryStreamManager recyclableMemoryStreamManager,
        IClient client,
        ILogger<ArtistNfoReader> logger)
        : base(recyclableMemoryStreamManager, logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<Either<BaseError, ArtistNfo>> ReadFromFile(string fileName)
    {
        // ReSharper disable once ConvertToUsingDeclaration
        await using (Stream s = await SanitizedStreamForFile(fileName))
        {
            return await Read(s, fileName);
        }
    }

    internal async Task<Either<BaseError, ArtistNfo>> Read(Stream input, string fileName)
    {
        ArtistNfo? nfo = null;

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
                            case "artist":
                                nfo = new ArtistNfo();
                                break;
                            case "name":
                                await ReadStringContent(reader, nfo, (artist, name) => artist.Name = name, fileName);
                                break;
                            case "disambiguation":
                                await ReadStringContent(
                                    reader,
                                    nfo,
                                    (artist, disambiguation) => artist.Disambiguation = disambiguation,
                                    fileName);
                                break;
                            case "genre":
                                await ReadStringContent(
                                    reader,
                                    nfo,
                                    (artist, genre) => artist.Genres.Add(genre),
                                    fileName);
                                break;
                            case "style":
                                await ReadStringContent(
                                    reader,
                                    nfo,
                                    (artist, style) => artist.Styles.Add(style),
                                    fileName);
                                break;
                            case "mood":
                                await ReadStringContent(
                                    reader,
                                    nfo,
                                    (artist, mood) => artist.Moods.Add(mood),
                                    fileName);
                                break;
                            case "biography":
                                await ReadStringContent(
                                    reader,
                                    nfo,
                                    (artist, biography) => artist.Biography = biography,
                                    fileName);
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
