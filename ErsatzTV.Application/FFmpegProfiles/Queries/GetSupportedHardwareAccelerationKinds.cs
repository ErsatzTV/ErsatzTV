using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.FFmpegProfiles;

public record GetSupportedHardwareAccelerationKinds : IRequest<List<HardwareAccelerationKind>>;
