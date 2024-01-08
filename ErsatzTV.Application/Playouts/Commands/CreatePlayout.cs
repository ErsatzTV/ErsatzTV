using ErsatzTV.Core;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Playouts;

public record CreatePlayout(int ChannelId, ProgramSchedulePlayoutType ProgramSchedulePlayoutType)
    : IRequest<Either<BaseError, CreatePlayoutResponse>>;

public record CreateFloodPlayout(int ChannelId, int ProgramScheduleId)
    : CreatePlayout(ChannelId, ProgramSchedulePlayoutType.Flood);

public record CreateExternalJsonPlayout(int ChannelId, string ExternalJsonFile)
    : CreatePlayout(ChannelId, ProgramSchedulePlayoutType.ExternalJson);
