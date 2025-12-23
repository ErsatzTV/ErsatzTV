#nullable enable
using System.ComponentModel.DataAnnotations;
using ErsatzTV.Application.Artworks;
using ErsatzTV.Application.Watermarks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.FFmpeg.State;
using LanguageExt;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api;

[ApiController]

public class WatermarkController(IMediator mediator) : ControllerBase
{
    [HttpGet("/api/watermarks", Name = "GetAllWatermarks")]
    [Tags("Watermarks")]
    [EndpointSummary("Get all watermarks")]
    [EndpointGroupName("general")]
    public async Task<List<WatermarkViewModel>> GetAllWatermarks(CancellationToken cancellationToken) =>
        await mediator.Send(new GetAllWatermarks(), cancellationToken);

    [HttpGet("/api/watermarks/{id:int}", Name = "GetWatermarkById")]
    [Tags("Watermarks")]
    [EndpointSummary("Get watermark by ID")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> GetWatermarkById(int id, CancellationToken cancellationToken)
    {
        Option<WatermarkViewModel> result = await mediator.Send(new GetWatermarkById(id), cancellationToken);
        return result.Match<IActionResult>(Ok, () => NotFound());
    }

    [HttpPost("/api/watermarks", Name = "CreateWatermark")]
    [Tags("Watermarks")]
    [EndpointSummary("Create a watermark")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> CreateWatermark(
        [Required] [FromBody] CreateWatermarkRequest request,
        CancellationToken cancellationToken)
    {
        var image = request.ImagePath != null
            ? new ArtworkContentTypeModel(request.ImagePath, request.ImageContentType ?? "")
            : ArtworkContentTypeModel.None;

        Either<BaseError, CreateWatermarkResult> result = await mediator.Send(
            new CreateWatermark(
                request.Name,
                image,
                request.Mode,
                request.ImageSource,
                request.Location,
                request.Size,
                request.Width,
                request.HorizontalMargin,
                request.VerticalMargin,
                request.FrequencyMinutes,
                request.DurationSeconds,
                request.Opacity,
                request.PlaceWithinSourceContent,
                request.OpacityExpression ?? "",
                request.ZIndex),
            cancellationToken);
        return result.Match<IActionResult>(Ok, error => Problem(error.ToString()));
    }

    [HttpPut("/api/watermarks/{id:int}", Name = "UpdateWatermark")]
    [Tags("Watermarks")]
    [EndpointSummary("Update a watermark")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> UpdateWatermark(
        int id,
        [Required] [FromBody] UpdateWatermarkRequest request,
        CancellationToken cancellationToken)
    {
        var image = request.ImagePath != null
            ? new ArtworkContentTypeModel(request.ImagePath, request.ImageContentType ?? "")
            : ArtworkContentTypeModel.None;

        Either<BaseError, UpdateWatermarkResult> result = await mediator.Send(
            new UpdateWatermark(
                id,
                request.Name,
                image,
                request.Mode,
                request.ImageSource,
                request.Location,
                request.Size,
                request.Width,
                request.HorizontalMargin,
                request.VerticalMargin,
                request.FrequencyMinutes,
                request.DurationSeconds,
                request.Opacity,
                request.PlaceWithinSourceContent,
                request.OpacityExpression ?? "",
                request.ZIndex),
            cancellationToken);
        return result.Match<IActionResult>(Ok, error => Problem(error.ToString()));
    }

    [HttpDelete("/api/watermarks/{id:int}", Name = "DeleteWatermark")]
    [Tags("Watermarks")]
    [EndpointSummary("Delete a watermark")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> DeleteWatermark(int id, CancellationToken cancellationToken)
    {
        Either<BaseError, Unit> result = await mediator.Send(new DeleteWatermark(id), cancellationToken);
        return result.Match<IActionResult>(_ => NoContent(), error => Problem(error.ToString()));
    }

    [HttpPost("/api/watermarks/{id:int}/copy", Name = "CopyWatermark")]
    [Tags("Watermarks")]
    [EndpointSummary("Copy a watermark")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> CopyWatermark(
        int id,
        [Required] [FromBody] CopyWatermarkRequest request,
        CancellationToken cancellationToken)
    {
        Either<BaseError, WatermarkViewModel> result = await mediator.Send(
            new CopyWatermark(id, request.Name), cancellationToken);
        return result.Match<IActionResult>(Ok, error => Problem(error.ToString()));
    }
}

// Request models
public record CreateWatermarkRequest(
    string Name,
    string? ImagePath,
    string? ImageContentType,
    ChannelWatermarkMode Mode,
    ChannelWatermarkImageSource ImageSource,
    WatermarkLocation Location,
    WatermarkSize Size,
    double Width,
    double HorizontalMargin,
    double VerticalMargin,
    int FrequencyMinutes,
    int DurationSeconds,
    int Opacity,
    bool PlaceWithinSourceContent,
    string? OpacityExpression,
    int ZIndex);

public record UpdateWatermarkRequest(
    string Name,
    string? ImagePath,
    string? ImageContentType,
    ChannelWatermarkMode Mode,
    ChannelWatermarkImageSource ImageSource,
    WatermarkLocation Location,
    WatermarkSize Size,
    double Width,
    double HorizontalMargin,
    double VerticalMargin,
    int FrequencyMinutes,
    int DurationSeconds,
    int Opacity,
    bool PlaceWithinSourceContent,
    string? OpacityExpression,
    int ZIndex);

public record CopyWatermarkRequest(string Name);
