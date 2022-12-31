using ErsatzTV.Core;
using ErsatzTV.Scanner.Core.Metadata.Nfo;

namespace ErsatzTV.Scanner.Core.Interfaces.Metadata.Nfo;

public interface ITvShowNfoReader
{
    Task<Either<BaseError, TvShowNfo>> ReadFromFile(string fileName);
}
