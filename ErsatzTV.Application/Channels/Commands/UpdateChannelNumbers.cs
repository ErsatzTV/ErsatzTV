using ErsatzTV.Core;

namespace ErsatzTV.Application.Channels;

public record UpdateChannelNumbers(List<ChannelSortViewModel> Channels) : IRequest<Option<BaseError>>;
