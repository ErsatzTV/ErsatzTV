using ErsatzTV.Core.Metadata.Nfo;

namespace ErsatzTV.Core.Interfaces.Metadata.Nfo;

public interface IArtistNfoReader
{
    Task<Either<BaseError, ArtistNfo>> Read(Stream input);
}
