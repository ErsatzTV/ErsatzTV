using ErsatzTV.Core;
using ErsatzTV.Scanner.Core.Metadata.Nfo;

namespace ErsatzTV.Scanner.Core.Interfaces.Metadata.Nfo;

public interface IShowNfoReader
{
    Task<Either<BaseError, ShowNfo>> ReadFromFile(string fileName);
}
