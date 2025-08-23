using ErsatzTV.Application.Channels;
using ErsatzTV.Application.Playouts;
using ErsatzTV.Application.ProgramSchedules;

namespace ErsatzTV.ViewModels;

public class PlayoutEditViewModel
{
    public string Kind { get; set; }
    public ChannelViewModel Channel { get; set; }
    public ProgramScheduleViewModel ProgramSchedule { get; set; }
    public string ExternalJsonFile { get; set; }
    public string SequentialSchedule { get; set; }

    public CreatePlayout ToCreate() =>
        Kind switch
        {
            PlayoutKind.ExternalJson => new CreateExternalJsonPlayout(Channel.Id, ExternalJsonFile),
            PlayoutKind.Sequential => new CreateSequentialPlayout(Channel.Id, SequentialSchedule),
            PlayoutKind.Block => new CreateBlockPlayout(Channel.Id),
            _ => new CreateClassicPlayout(Channel.Id, ProgramSchedule.Id)
        };
}
