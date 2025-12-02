using ErsatzTV.FFmpeg;

namespace ErsatzTV.Application.Channels;

public record GetChannelFramerate(string ChannelNumber) : IRequest<Option<FrameRate>>;
