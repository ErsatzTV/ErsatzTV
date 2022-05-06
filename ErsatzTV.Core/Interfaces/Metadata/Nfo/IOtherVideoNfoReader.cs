using ErsatzTV.Core.Metadata.Nfo;

namespace ErsatzTV.Core.Interfaces.Metadata.Nfo;

public interface IOtherVideoNfoReader
{
    Task<Either<BaseError, OtherVideoNfo>> Read(Stream input);
}
