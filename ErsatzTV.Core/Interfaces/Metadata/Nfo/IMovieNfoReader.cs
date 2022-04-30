using ErsatzTV.Core.Metadata.Nfo;

namespace ErsatzTV.Core.Interfaces.Metadata.Nfo;

public interface IMovieNfoReader
{
    Task<Either<BaseError, MovieNfo>> Read(Stream input);
}
