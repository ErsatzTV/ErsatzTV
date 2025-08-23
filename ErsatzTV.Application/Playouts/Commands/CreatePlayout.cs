using ErsatzTV.Core;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Playouts;

public record CreatePlayout(int ChannelId, PlayoutScheduleKind ScheduleKind)
    : IRequest<Either<BaseError, CreatePlayoutResponse>>;

public record CreateClassicPlayout(int ChannelId, int ProgramScheduleId)
    : CreatePlayout(ChannelId, PlayoutScheduleKind.Classic);

public record CreateBlockPlayout(int ChannelId)
    : CreatePlayout(ChannelId, PlayoutScheduleKind.Block);

public record CreateSequentialPlayout(int ChannelId, string TemplateFile)
    : CreatePlayout(ChannelId, PlayoutScheduleKind.Sequential);

public record CreateExternalJsonPlayout(int ChannelId, string ExternalJsonFile)
    : CreatePlayout(ChannelId, PlayoutScheduleKind.ExternalJson);
