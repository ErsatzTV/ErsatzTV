#nullable enable
using System.ComponentModel.DataAnnotations;
using ErsatzTV.Application.Emby;
using ErsatzTV.Core.Emby;
using ErsatzTV.Core;
using LanguageExt;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api;

[ApiController]

public class EmbyController(IMediator mediator) : ControllerBase
{
    [HttpGet("/api/emby/sources", Name = "GetEmbyMediaSources")]
    [Tags("Emby")]
    [EndpointSummary("Get all Emby media sources")]
    [EndpointGroupName("general")]
    public async Task<List<EmbyMediaSourceViewModel>> GetEmbyMediaSources(CancellationToken cancellationToken) =>
        await mediator.Send(new GetAllEmbyMediaSources(), cancellationToken);

    [HttpGet("/api/emby/sources/{id:int}", Name = "GetEmbyMediaSourceById")]
    [Tags("Emby")]
    [EndpointSummary("Get Emby media source by ID")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> GetEmbyMediaSourceById(int id, CancellationToken cancellationToken)
    {
        Option<EmbyMediaSourceViewModel> result = await mediator.Send(new GetEmbyMediaSourceById(id), cancellationToken);
        return result.Match<IActionResult>(Ok, () => NotFound());
    }

    [HttpGet("/api/emby/sources/{id:int}/libraries", Name = "GetEmbyLibraries")]
    [Tags("Emby")]
    [EndpointSummary("Get Emby libraries by source")]
    [EndpointGroupName("general")]
    public async Task<List<EmbyLibraryViewModel>> GetEmbyLibraries(int id, CancellationToken cancellationToken) =>
        await mediator.Send(new GetEmbyLibrariesBySourceId(id), cancellationToken);

    [HttpGet("/api/emby/sources/{id:int}/path-replacements", Name = "GetEmbyPathReplacements")]
    [Tags("Emby")]
    [EndpointSummary("Get Emby path replacements by source")]
    [EndpointGroupName("general")]
    public async Task<List<EmbyPathReplacementViewModel>> GetEmbyPathReplacements(int id, CancellationToken cancellationToken) =>
        await mediator.Send(new GetEmbyPathReplacementsBySourceId(id), cancellationToken);

    [HttpPut("/api/emby/sources/{id:int}/path-replacements", Name = "UpdateEmbyPathReplacements")]
    [Tags("Emby")]
    [EndpointSummary("Update Emby path replacements")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> UpdateEmbyPathReplacements(
        int id,
        [Required] [FromBody] UpdateEmbyPathReplacementsRequest request,
        CancellationToken cancellationToken)
    {
        var replacements = request.PathReplacements?.Select(p =>
            new EmbyPathReplacementItem(p.Id, p.EmbyPath, p.LocalPath)).ToList() ?? [];
        Either<BaseError, Unit> result = await mediator.Send(
            new UpdateEmbyPathReplacements(id, replacements), cancellationToken);
        return result.Match<IActionResult>(_ => Ok(), error => Problem(error.ToString()));
    }

    [HttpPut("/api/emby/library-preferences", Name = "UpdateEmbyLibraryPreferences")]
    [Tags("Emby")]
    [EndpointSummary("Update Emby library preferences")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> UpdateEmbyLibraryPreferences(
        [Required] [FromBody] UpdateEmbyLibraryPreferencesRequest request,
        CancellationToken cancellationToken)
    {
        var preferences = request.Preferences?.Select(p =>
            new EmbyLibraryPreference(p.Id, p.ShouldSyncItems)).ToList() ?? [];
        Either<BaseError, Unit> result = await mediator.Send(
            new UpdateEmbyLibraryPreferences(preferences), cancellationToken);
        return result.Match<IActionResult>(_ => Ok(), error => Problem(error.ToString()));
    }

    [HttpGet("/api/emby/secrets", Name = "GetEmbySecrets")]
    [Tags("Emby")]
    [EndpointSummary("Get Emby secrets")]
    [EndpointGroupName("general")]
    public async Task<EmbySecrets> GetEmbySecrets(CancellationToken cancellationToken) =>
        await mediator.Send(new GetEmbySecrets(), cancellationToken);
}

// Request models
public record EmbyPathReplacementRequest(int Id, string EmbyPath, string LocalPath);
public record UpdateEmbyPathReplacementsRequest(List<EmbyPathReplacementRequest>? PathReplacements);
public record EmbyLibraryPreferenceRequest(int Id, bool ShouldSyncItems);
public record UpdateEmbyLibraryPreferencesRequest(List<EmbyLibraryPreferenceRequest>? Preferences);
