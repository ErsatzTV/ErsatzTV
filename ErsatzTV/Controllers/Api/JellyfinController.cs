#nullable enable
using System.ComponentModel.DataAnnotations;
using ErsatzTV.Application.Jellyfin;
using ErsatzTV.Core.Jellyfin;
using ErsatzTV.Core;
using LanguageExt;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api;

[ApiController]

public class JellyfinController(IMediator mediator) : ControllerBase
{
    [HttpGet("/api/jellyfin/sources", Name = "GetJellyfinMediaSources")]
    [Tags("Jellyfin")]
    [EndpointSummary("Get all Jellyfin media sources")]
    [EndpointGroupName("general")]
    public async Task<List<JellyfinMediaSourceViewModel>> GetJellyfinMediaSources(CancellationToken cancellationToken) =>
        await mediator.Send(new GetAllJellyfinMediaSources(), cancellationToken);

    [HttpGet("/api/jellyfin/sources/{id:int}", Name = "GetJellyfinMediaSourceById")]
    [Tags("Jellyfin")]
    [EndpointSummary("Get Jellyfin media source by ID")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> GetJellyfinMediaSourceById(int id, CancellationToken cancellationToken)
    {
        Option<JellyfinMediaSourceViewModel> result = await mediator.Send(new GetJellyfinMediaSourceById(id), cancellationToken);
        return result.Match<IActionResult>(Ok, () => NotFound());
    }

    [HttpGet("/api/jellyfin/sources/{id:int}/libraries", Name = "GetJellyfinLibraries")]
    [Tags("Jellyfin")]
    [EndpointSummary("Get Jellyfin libraries by source")]
    [EndpointGroupName("general")]
    public async Task<List<JellyfinLibraryViewModel>> GetJellyfinLibraries(int id, CancellationToken cancellationToken) =>
        await mediator.Send(new GetJellyfinLibrariesBySourceId(id), cancellationToken);

    [HttpGet("/api/jellyfin/sources/{id:int}/path-replacements", Name = "GetJellyfinPathReplacements")]
    [Tags("Jellyfin")]
    [EndpointSummary("Get Jellyfin path replacements by source")]
    [EndpointGroupName("general")]
    public async Task<List<JellyfinPathReplacementViewModel>> GetJellyfinPathReplacements(int id, CancellationToken cancellationToken) =>
        await mediator.Send(new GetJellyfinPathReplacementsBySourceId(id), cancellationToken);

    [HttpPut("/api/jellyfin/sources/{id:int}/path-replacements", Name = "UpdateJellyfinPathReplacements")]
    [Tags("Jellyfin")]
    [EndpointSummary("Update Jellyfin path replacements")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> UpdateJellyfinPathReplacements(
        int id,
        [Required] [FromBody] UpdateJellyfinPathReplacementsRequest request,
        CancellationToken cancellationToken)
    {
        var replacements = request.PathReplacements?.Select(p =>
            new JellyfinPathReplacementItem(p.Id, p.JellyfinPath, p.LocalPath)).ToList() ?? [];
        Either<BaseError, Unit> result = await mediator.Send(
            new UpdateJellyfinPathReplacements(id, replacements), cancellationToken);
        return result.Match<IActionResult>(_ => Ok(), error => Problem(error.ToString()));
    }

    [HttpPut("/api/jellyfin/library-preferences", Name = "UpdateJellyfinLibraryPreferences")]
    [Tags("Jellyfin")]
    [EndpointSummary("Update Jellyfin library preferences")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> UpdateJellyfinLibraryPreferences(
        [Required] [FromBody] UpdateJellyfinLibraryPreferencesRequest request,
        CancellationToken cancellationToken)
    {
        var preferences = request.Preferences?.Select(p =>
            new JellyfinLibraryPreference(p.Id, p.ShouldSyncItems)).ToList() ?? [];
        Either<BaseError, Unit> result = await mediator.Send(
            new UpdateJellyfinLibraryPreferences(preferences), cancellationToken);
        return result.Match<IActionResult>(_ => Ok(), error => Problem(error.ToString()));
    }

    [HttpGet("/api/jellyfin/secrets", Name = "GetJellyfinSecrets")]
    [Tags("Jellyfin")]
    [EndpointSummary("Get Jellyfin secrets")]
    [EndpointGroupName("general")]
    public async Task<JellyfinSecrets> GetJellyfinSecrets(CancellationToken cancellationToken) =>
        await mediator.Send(new GetJellyfinSecrets(), cancellationToken);
}

// Request models
public record JellyfinPathReplacementRequest(int Id, string JellyfinPath, string LocalPath);
public record UpdateJellyfinPathReplacementsRequest(List<JellyfinPathReplacementRequest>? PathReplacements);
public record JellyfinLibraryPreferenceRequest(int Id, bool ShouldSyncItems);
public record UpdateJellyfinLibraryPreferencesRequest(List<JellyfinLibraryPreferenceRequest>? Preferences);
