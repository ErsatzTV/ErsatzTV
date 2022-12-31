using ErsatzTV.Core;
using ErsatzTV.Scanner.Core.Metadata.Nfo;

namespace ErsatzTV.Scanner.Core.Interfaces.Metadata.Nfo;

public interface IOtherVideoNfoReader
{
    Task<Either<BaseError, OtherVideoNfo>> ReadFromFile(string fileName);
}
