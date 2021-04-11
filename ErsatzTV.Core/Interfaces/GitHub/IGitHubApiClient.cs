using System.Threading.Tasks;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.GitHub
{
    public interface IGitHubApiClient
    {
        Task<Either<BaseError, string>> GetLatestReleaseNotes();
        Task<Either<BaseError, string>> GetReleaseNotes(string tag);
    }
}
