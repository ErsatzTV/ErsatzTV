namespace ErsatzTV.Application.Channels;

public record GetSlugSecondsByChannelNumber(string ChannelNumber) : IRequest<Option<double>>;
