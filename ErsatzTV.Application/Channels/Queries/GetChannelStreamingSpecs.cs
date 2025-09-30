namespace ErsatzTV.Application.Channels;

public record GetChannelStreamingSpecs(string ChannelNumber) : IRequest<Option<ChannelStreamingSpecsViewModel>>;
