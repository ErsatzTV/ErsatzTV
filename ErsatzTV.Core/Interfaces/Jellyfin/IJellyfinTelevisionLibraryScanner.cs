using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Jellyfin;

public interface IJellyfinTelevisionLibraryScanner
{
    Task<Either<BaseError, Unit>> ScanLibrary(
        string address,
        string apiKey,
        JellyfinLibrary library,
        string ffprobePath);
}