using System.ComponentModel.DataAnnotations;
using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Core;
using ErsatzTV.Core.Api.SmartCollections;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api;

[ApiController]
[EndpointGroupName("general")]
public class SmartCollectionController(IMediator mediator) : ControllerBase
{
    [HttpGet("/api/collections/smart", Name="GetSmartCollections")]
    public async Task<List<SmartCollectionResponseModel>> GetAll() =>
        await mediator.Send(new GetAllSmartCollectionsForApi());

    [HttpPost("/api/collections/smart/new", Name = "CreateSmartCollection")]
    public async Task<IActionResult> AddOne(
        [Required] [FromBody]
        CreateSmartCollection request)
    {
        Either<BaseError, CreateSmartCollectionResult> result =
            await mediator.Send(request).MapT(r => new CreateSmartCollectionResult(r.Id));
        return result.Match<IActionResult>(Ok, error => Problem(error.ToString()));
    }

    [HttpPut("/api/collections/smart/update", Name="UpdateSmartCollection")]
    public async Task<IActionResult> UpdateOne(
        [Required] [FromBody]
        UpdateSmartCollection request)
    {
        Either<BaseError, UpdateSmartCollectionResult> result = await mediator.Send(request);
        return result.Match<IActionResult>(Ok, error => Problem(error.ToString()));
    }

    [HttpDelete("/api/collections/smart/delete/{id:int}", Name="DeleteSmartCollection")]
    public async Task<IActionResult> DeleteSmartCollection(int id)
    {
        Either<BaseError, Unit> result = await mediator.Send(new DeleteSmartCollection(id));
        return result.Match<IActionResult>(_ => Ok(), error => Problem(error.ToString()));
    }
}
