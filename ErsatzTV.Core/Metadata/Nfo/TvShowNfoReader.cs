using System.Xml.Serialization;
using Bugsnag;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.Interfaces.Metadata.Nfo;

namespace ErsatzTV.Core.Metadata.Nfo;

public class TvShowNfoReader : ITvShowNfoReader
{
    private static readonly XmlSerializer TvShowSerializer = new(typeof(TvShowNfo));

    private readonly IClient _client;

    public TvShowNfoReader(IClient client) => _client = client;

    public async Task<Either<BaseError, TvShowNfo>> Read(Stream input)
    {
        try
        {
            Option<TvShowNfo> nfo = Optional(TvShowSerializer.Deserialize(input) as TvShowNfo);
            return nfo.ToEither((BaseError)new FailedToReadNfo());
        }
        catch (Exception ex)
        {
            _client.Notify(ex);
            return new FailedToReadNfo(ex.ToString());
        }
    }
}
