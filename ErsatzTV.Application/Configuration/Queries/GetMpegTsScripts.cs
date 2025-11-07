using ErsatzTV.Core.FFmpeg;

namespace ErsatzTV.Application.Configuration;

public record GetMpegTsScripts : IRequest<List<MpegTsScript>>;
