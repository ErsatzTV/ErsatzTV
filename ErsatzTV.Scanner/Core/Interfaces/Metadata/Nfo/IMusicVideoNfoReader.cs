using ErsatzTV.Core;
using ErsatzTV.Scanner.Core.Metadata.Nfo;

namespace ErsatzTV.Scanner.Core.Interfaces.Metadata.Nfo;

public interface IMusicVideoNfoReader
{
    Task<Either<BaseError, MusicVideoNfo>> ReadFromFile(string fileName);
}
