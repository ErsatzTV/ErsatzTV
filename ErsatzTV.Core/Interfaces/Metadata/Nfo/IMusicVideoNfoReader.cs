using ErsatzTV.Core.Metadata.Nfo;

namespace ErsatzTV.Core.Interfaces.Metadata.Nfo;

public interface IMusicVideoNfoReader
{
    Task<Either<BaseError, MusicVideoNfo>> Read(Stream input);
}
