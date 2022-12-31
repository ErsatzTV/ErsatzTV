using ErsatzTV.Core;
using ErsatzTV.Scanner.Core.Metadata.Nfo;

namespace ErsatzTV.Scanner.Core.Interfaces.Metadata.Nfo;

public interface IMovieNfoReader
{
    Task<Either<BaseError, MovieNfo>> ReadFromFile(string fileName);
}
