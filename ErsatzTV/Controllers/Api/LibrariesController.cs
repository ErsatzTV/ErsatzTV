#nullable enable
using System.ComponentModel.DataAnnotations;
using ErsatzTV.Application.Libraries;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api;

[ApiController]
[EndpointGroupName("general")]
public class LibrariesController(ITelevisionRepository televisionRepository, IMediator mediator) : ControllerBase
{
    [HttpGet("/api/libraries", Name = "GetAllLibraries")]
    [Tags("Libraries")]
    [EndpointSummary("Get all configured libraries")]
    public async Task<List<LibraryViewModel>> GetAllLibraries(CancellationToken cancellationToken) =>
        await mediator.Send(new GetConfiguredLibraries(), cancellationToken);

    [HttpGet("/api/libraries/local", Name = "GetLocalLibraries")]
    [Tags("Libraries")]
    [EndpointSummary("Get all local libraries")]
    public async Task<List<LocalLibraryViewModel>> GetLocalLibraries(CancellationToken cancellationToken) =>
        await mediator.Send(new GetAllLocalLibraries(), cancellationToken);

    [HttpGet("/api/libraries/local/{id:int}", Name = "GetLocalLibraryById")]
    [Tags("Libraries")]
    [EndpointSummary("Get local library by ID")]
    public async Task<IActionResult> GetLocalLibraryById(int id, CancellationToken cancellationToken)
    {
        Option<LocalLibraryViewModel> result = await mediator.Send(new GetLocalLibraryById(id), cancellationToken);
        return result.Match<IActionResult>(Ok, () => NotFound());
    }

    [HttpGet("/api/libraries/local/{id:int}/paths", Name = "GetLocalLibraryPaths")]
    [Tags("Libraries")]
    [EndpointSummary("Get local library paths")]
    public async Task<List<LocalLibraryPathViewModel>> GetLocalLibraryPaths(int id, CancellationToken cancellationToken) =>
        await mediator.Send(new GetLocalLibraryPaths(id), cancellationToken);

    [HttpPost("/api/libraries/local", Name = "CreateLocalLibrary")]
    [Tags("Libraries")]
    [EndpointSummary("Create a local library")]
    public async Task<IActionResult> CreateLocalLibrary(
        [Required] [FromBody] CreateLocalLibraryRequest request,
        CancellationToken cancellationToken)
    {
        Either<BaseError, LocalLibraryViewModel> result = await mediator.Send(
            new CreateLocalLibrary(request.Name, request.MediaKind, request.Paths ?? []), cancellationToken);
        return result.Match<IActionResult>(Ok, error => Problem(error.ToString()));
    }

    [HttpPut("/api/libraries/local/{id:int}", Name = "UpdateLocalLibrary")]
    [Tags("Libraries")]
    [EndpointSummary("Update a local library")]
    public async Task<IActionResult> UpdateLocalLibrary(
        int id,
        [Required] [FromBody] UpdateLocalLibraryRequest request,
        CancellationToken cancellationToken)
    {
        var paths = request.Paths?.Select(p => new UpdateLocalLibraryPath(p.Id, p.Path)).ToList() ?? [];
        Either<BaseError, LocalLibraryViewModel> result = await mediator.Send(
            new UpdateLocalLibrary(id, request.Name, paths), cancellationToken);
        return result.Match<IActionResult>(Ok, error => Problem(error.ToString()));
    }

    [HttpDelete("/api/libraries/local/{id:int}", Name = "DeleteLocalLibrary")]
    [Tags("Libraries")]
    [EndpointSummary("Delete a local library")]
    public async Task<IActionResult> DeleteLocalLibrary(int id, CancellationToken cancellationToken)
    {
        Either<BaseError, Unit> result = await mediator.Send(new DeleteLocalLibrary(id), cancellationToken);
        return result.Match<IActionResult>(_ => NoContent(), error => Problem(error.ToString()));
    }

    [HttpPost("/api/libraries/{id:int}/scan", Name = "ScanLibrary")]
    [Tags("Libraries")]
    [EndpointSummary("Scan library")]
    public async Task<IActionResult> ScanLibrary(int id) =>
        await mediator.Send(new QueueLibraryScanByLibraryId(id))
            ? Ok()
            : NotFound();

    [HttpPost("/api/libraries/{id:int}/scan-show", Name = "ScanShow")]
    [Tags("Libraries")]
    [EndpointSummary("Scan show")]
    public async Task<IActionResult> ScanShow(int id, [FromBody] ScanShowRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ShowTitle))
        {
            return BadRequest(new { error = "ShowTitle is required" });
        }

        string trimmedTitle = request.ShowTitle.Trim();
        Option<int> maybeShowId = await televisionRepository.GetShowIdByTitle(id, trimmedTitle);
        foreach (int showId in maybeShowId)
        {
            bool result = await mediator.Send(new QueueShowScanByLibraryId(id, showId, trimmedTitle, request.DeepScan));

            return result
                ? Ok()
                : BadRequest(new { error = "Unable to queue show scan. Library may not exist, may not support single show scanning, or may already be scanning." });
        }

        return BadRequest(
            new { error = $"Unable to locate show with title {request.ShowTitle} in library {id}" });
    }

    [HttpGet("/api/libraries/external-collections", Name = "GetExternalCollections")]
    [Tags("Libraries")]
    [EndpointSummary("Get external collections")]
    public async Task<List<LibraryViewModel>> GetExternalCollections(CancellationToken cancellationToken) =>
        await mediator.Send(new GetExternalCollections(), cancellationToken);
}

// Request models
public record ScanShowRequest(string ShowTitle, bool DeepScan = false);
public record CreateLocalLibraryRequest(string Name, LibraryMediaKind MediaKind, List<string>? Paths);
public record UpdateLocalLibraryPathRequest(int Id, string Path);
public record UpdateLocalLibraryRequest(string Name, List<UpdateLocalLibraryPathRequest>? Paths);
