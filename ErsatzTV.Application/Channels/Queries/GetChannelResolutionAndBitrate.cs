namespace ErsatzTV.Application.Channels;

public record GetChannelResolutionAndBitrate(string ChannelNumber) : IRequest<Option<ResolutionAndBitrateViewModel>>;
