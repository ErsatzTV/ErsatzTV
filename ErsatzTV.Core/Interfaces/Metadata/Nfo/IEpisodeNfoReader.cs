using ErsatzTV.Core.Metadata.Nfo;

namespace ErsatzTV.Core.Interfaces.Metadata.Nfo;

public interface IEpisodeNfoReader
{
    Task<Either<BaseError, List<TvShowEpisodeNfo>>> Read(Stream input);
}
