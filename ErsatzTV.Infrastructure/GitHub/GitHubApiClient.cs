using System;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.GitHub;
using LanguageExt;
using Refit;

namespace ErsatzTV.Infrastructure.GitHub
{
    public class GitHubApiClient : IGitHubApiClient
    {
        public async Task<Either<BaseError, string>> GetLatestReleaseNotes()
        {
            try
            {
                IGitHubApi service = RestService.For<IGitHubApi>("https://api.github.com");
                return await service.GetReleases().Map(releases => releases.Head().Body);
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.ToString());
            }
        }

        public async Task<Either<BaseError, string>> GetReleaseNotes(string tag)
        {
            try
            {
                IGitHubApi service = RestService.For<IGitHubApi>("https://api.github.com");
                return await service.GetTag(tag).Map(t => t.Body);
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.ToString());
            }
        }
    }
}
