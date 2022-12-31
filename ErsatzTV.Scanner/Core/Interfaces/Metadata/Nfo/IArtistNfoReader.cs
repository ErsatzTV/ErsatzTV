using ErsatzTV.Core;
using ErsatzTV.Scanner.Core.Metadata.Nfo;

namespace ErsatzTV.Scanner.Core.Interfaces.Metadata.Nfo;

public interface IArtistNfoReader
{
    Task<Either<BaseError, ArtistNfo>> ReadFromFile(string fileName);
}
