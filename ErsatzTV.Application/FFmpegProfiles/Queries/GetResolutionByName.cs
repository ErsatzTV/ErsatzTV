using ErsatzTV.Core;

namespace ErsatzTV.Application.FFmpegProfiles;

public record GetResolutionByName(string Name) : IRequest<Option<int>>;
