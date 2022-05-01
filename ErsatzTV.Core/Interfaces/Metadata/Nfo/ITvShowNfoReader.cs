using ErsatzTV.Core.Metadata.Nfo;

namespace ErsatzTV.Core.Interfaces.Metadata.Nfo;

public interface ITvShowNfoReader
{
    Task<Either<BaseError, TvShowNfo>> Read(Stream input);
}
