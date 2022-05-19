using ErsatzTV.Application.Channels;
using ErsatzTV.Core.Api.Channels;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api;

[ApiController]
public class ChannelController
{
    private readonly IMediator _mediator;

    public ChannelController(IMediator mediator) => _mediator = mediator;

    [HttpGet("/api/channels")]
    public async Task<List<ChannelResponseModel>> GetAll() => await _mediator.Send(new GetAllChannelsForApi());
}
