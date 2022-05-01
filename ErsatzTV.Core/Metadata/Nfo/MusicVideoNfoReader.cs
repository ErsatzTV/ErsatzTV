using System.Xml.Serialization;
using Bugsnag;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.Interfaces.Metadata.Nfo;

namespace ErsatzTV.Core.Metadata.Nfo;

public class MusicVideoNfoReader : IMusicVideoNfoReader
{
    private static readonly XmlSerializer MusicVideoSerializer = new(typeof(MusicVideoNfo));

    private readonly IClient _client;

    public MusicVideoNfoReader(IClient client) => _client = client;

    public async Task<Either<BaseError, MusicVideoNfo>> Read(Stream input)
    {
        try
        {
            Option<MusicVideoNfo> nfo = Optional(MusicVideoSerializer.Deserialize(input) as MusicVideoNfo);
            return nfo.ToEither((BaseError)new FailedToReadNfo());
        }
        catch (Exception ex)
        {
            _client.Notify(ex);
            return new FailedToReadNfo(ex.ToString());
        }
    }
}
