﻿using ErsatzTV.Infrastructure.GitHub.Models;
using Refit;

namespace ErsatzTV.Infrastructure.GitHub;

[Headers("Accept: application/vnd.github.v3+json", "User-Agent: ErsatzTV/ErsatzTV")]
public interface IGitHubApi
{
    [Get("/repos/ErsatzTV/ErsatzTV/releases")]
    Task<List<GitHubTag>> GetReleases(CancellationToken cancellationToken);

    [Get("/repos/ErsatzTV/ErsatzTV/releases/tags/{tag}")]
    Task<GitHubTag> GetTag(string tag, CancellationToken cancellationToken);
}
