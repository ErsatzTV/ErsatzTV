using ErsatzTV.Core.Api.Channels;

namespace ErsatzTV.Application.Channels;

public record GetAllChannelsForApi : IRequest<List<ChannelResponseModel>>;
