using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Emby;

public interface IEmbyMovieLibraryScanner
{
    Task<Either<BaseError, Unit>> ScanLibrary(
        string address,
        string apiKey,
        EmbyLibrary library,
        string ffmpegPath,
        string ffprobePath);
}