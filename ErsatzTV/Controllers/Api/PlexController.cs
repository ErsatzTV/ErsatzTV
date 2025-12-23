#nullable enable
using System.ComponentModel.DataAnnotations;
using ErsatzTV.Application.Plex;
using ErsatzTV.Core;
using LanguageExt;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api;

[ApiController]

public class PlexController(IMediator mediator) : ControllerBase
{
    [HttpGet("/api/plex/sources", Name = "GetPlexMediaSources")]
    [Tags("Plex")]
    [EndpointSummary("Get all Plex media sources")]
    [EndpointGroupName("general")]
    public async Task<List<PlexMediaSourceViewModel>> GetPlexMediaSources(CancellationToken cancellationToken) =>
        await mediator.Send(new GetAllPlexMediaSources(), cancellationToken);

    [HttpGet("/api/plex/sources/{id:int}", Name = "GetPlexMediaSourceById")]
    [Tags("Plex")]
    [EndpointSummary("Get Plex media source by ID")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> GetPlexMediaSourceById(int id, CancellationToken cancellationToken)
    {
        Option<PlexMediaSourceViewModel> result = await mediator.Send(new GetPlexMediaSourceById(id), cancellationToken);
        return result.Match<IActionResult>(Ok, () => NotFound());
    }

    [HttpGet("/api/plex/sources/{id:int}/libraries", Name = "GetPlexLibraries")]
    [Tags("Plex")]
    [EndpointSummary("Get Plex libraries by source")]
    [EndpointGroupName("general")]
    public async Task<List<PlexLibraryViewModel>> GetPlexLibraries(int id, CancellationToken cancellationToken) =>
        await mediator.Send(new GetPlexLibrariesBySourceId(id), cancellationToken);

    [HttpGet("/api/plex/sources/{id:int}/path-replacements", Name = "GetPlexPathReplacements")]
    [Tags("Plex")]
    [EndpointSummary("Get Plex path replacements by source")]
    [EndpointGroupName("general")]
    public async Task<List<PlexPathReplacementViewModel>> GetPlexPathReplacements(int id, CancellationToken cancellationToken) =>
        await mediator.Send(new GetPlexPathReplacementsBySourceId(id), cancellationToken);

    [HttpPut("/api/plex/sources/{id:int}/path-replacements", Name = "UpdatePlexPathReplacements")]
    [Tags("Plex")]
    [EndpointSummary("Update Plex path replacements")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> UpdatePlexPathReplacements(
        int id,
        [Required] [FromBody] UpdatePlexPathReplacementsRequest request,
        CancellationToken cancellationToken)
    {
        var replacements = request.PathReplacements?.Select(p =>
            new PlexPathReplacementItem(p.Id, p.PlexPath, p.LocalPath)).ToList() ?? [];
        Either<BaseError, Unit> result = await mediator.Send(
            new UpdatePlexPathReplacements(id, replacements), cancellationToken);
        return result.Match<IActionResult>(_ => Ok(), error => Problem(error.ToString()));
    }

    [HttpPut("/api/plex/library-preferences", Name = "UpdatePlexLibraryPreferences")]
    [Tags("Plex")]
    [EndpointSummary("Update Plex library preferences")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> UpdatePlexLibraryPreferences(
        [Required] [FromBody] UpdatePlexLibraryPreferencesRequest request,
        CancellationToken cancellationToken)
    {
        var preferences = request.Preferences?.Select(p =>
            new PlexLibraryPreference(p.Id, p.ShouldSyncItems)).ToList() ?? [];
        Either<BaseError, Unit> result = await mediator.Send(
            new UpdatePlexLibraryPreferences(preferences), cancellationToken);
        return result.Match<IActionResult>(_ => Ok(), error => Problem(error.ToString()));
    }
}

// Request models
public record PlexPathReplacementRequest(int Id, string PlexPath, string LocalPath);
public record UpdatePlexPathReplacementsRequest(List<PlexPathReplacementRequest>? PathReplacements);
public record PlexLibraryPreferenceRequest(int Id, bool ShouldSyncItems);
public record UpdatePlexLibraryPreferencesRequest(List<PlexLibraryPreferenceRequest>? Preferences);
