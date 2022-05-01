using System.Xml.Serialization;
using Bugsnag;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.Interfaces.Metadata.Nfo;

namespace ErsatzTV.Core.Metadata.Nfo;

public class ArtistNfoReader : IArtistNfoReader
{
    private static readonly XmlSerializer ArtistSerializer = new(typeof(ArtistNfo));

    private readonly IClient _client;

    public ArtistNfoReader(IClient client) => _client = client;

    public async Task<Either<BaseError, ArtistNfo>> Read(Stream input)
    {
        try
        {
            Option<ArtistNfo> nfo = Optional(ArtistSerializer.Deserialize(input) as ArtistNfo);
            return nfo.ToEither((BaseError)new FailedToReadNfo());
        }
        catch (Exception ex)
        {
            _client.Notify(ex);
            return new FailedToReadNfo(ex.ToString());
        }
    }
}
