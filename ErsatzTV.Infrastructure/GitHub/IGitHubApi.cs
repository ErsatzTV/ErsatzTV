using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Infrastructure.GitHub.Models;
using Refit;

namespace ErsatzTV.Infrastructure.GitHub;

[Headers("Accept: application/vnd.github.v3+json", "User-Agent: jasongdove/ErsatzTV")]
public interface IGitHubApi
{
    [Get("/repos/jasongdove/ErsatzTV/releases")]
    public Task<List<GitHubTag>> GetReleases(CancellationToken cancellationToken);

    [Get("/repos/jasongdove/ErsatzTV/releases/tags/{tag}")]
    public Task<GitHubTag> GetTag(string tag, CancellationToken cancellationToken);
}