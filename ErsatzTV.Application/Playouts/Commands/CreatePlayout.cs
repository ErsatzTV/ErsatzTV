using ErsatzTV.Core;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Playouts;

public record CreatePlayout(int ChannelId, ProgramSchedulePlayoutType ProgramSchedulePlayoutType)
    : IRequest<Either<BaseError, CreatePlayoutResponse>>;

public record CreateFloodPlayout(int ChannelId, int ProgramScheduleId)
    : CreatePlayout(ChannelId, ProgramSchedulePlayoutType.Classic);

public record CreateBlockPlayout(int ChannelId)
    : CreatePlayout(ChannelId, ProgramSchedulePlayoutType.Block);

public record CreateYamlPlayout(int ChannelId, string TemplateFile)
    : CreatePlayout(ChannelId, ProgramSchedulePlayoutType.Yaml);

public record CreateExternalJsonPlayout(int ChannelId, string ExternalJsonFile)
    : CreatePlayout(ChannelId, ProgramSchedulePlayoutType.ExternalJson);
