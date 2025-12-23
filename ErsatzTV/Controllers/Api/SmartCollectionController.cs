using System.ComponentModel.DataAnnotations;
using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Core;
using ErsatzTV.Core.Api.SmartCollections;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api;

[ApiController]
public class SmartCollectionController(IMediator mediator) : ControllerBase
{
    [HttpGet("/api/collections/smart", Name="GetSmartCollections")]
    [EndpointGroupName("general")]
    public async Task<List<SmartCollectionResponseModel>> GetAll() =>
        await mediator.Send(new GetAllSmartCollectionsForApi());

    [HttpPost("/api/collections/smart", Name = "CreateSmartCollection")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> AddOne(
        [Required] [FromBody]
        CreateSmartCollection request)
    {
        Either<BaseError, CreateSmartCollectionResult> result =
            await mediator.Send(request).MapT(r => new CreateSmartCollectionResult(r.Id));
        return result.Match<IActionResult>(Ok, error => Problem(error.ToString()));
    }

    [HttpPut("/api/collections/smart", Name="UpdateSmartCollection")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> UpdateOne(
        [Required] [FromBody]
        UpdateSmartCollection request)
    {
        Either<BaseError, UpdateSmartCollectionResult> result = await mediator.Send(request);
        return result.Match<IActionResult>(Ok, error => Problem(error.ToString()));
    }

    [HttpDelete("/api/collections/smart/{id:int}", Name="DeleteSmartCollection")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> DeleteSmartCollection(int id)
    {
        Either<BaseError, Unit> result = await mediator.Send(new DeleteSmartCollection(id));
        return result.Match<IActionResult>(_ => NoContent(), error => Problem(error.ToString()));
    }
}
