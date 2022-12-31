using ErsatzTV.Core;
using ErsatzTV.Scanner.Core.Metadata.Nfo;

namespace ErsatzTV.Scanner.Core.Interfaces.Metadata.Nfo;

public interface IEpisodeNfoReader
{
    Task<Either<BaseError, List<EpisodeNfo>>> ReadFromFile(string fileName);
}
