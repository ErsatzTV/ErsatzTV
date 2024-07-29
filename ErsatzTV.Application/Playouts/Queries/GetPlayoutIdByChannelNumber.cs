namespace ErsatzTV.Application.Playouts;

public record GetPlayoutIdByChannelNumber(string ChannelNumber) : IRequest<Option<int>>;
