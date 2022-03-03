using System.Collections.Generic;
using MediatR;

namespace ErsatzTV.Application.Channels;

public record GetAllChannels : IRequest<List<ChannelViewModel>>;