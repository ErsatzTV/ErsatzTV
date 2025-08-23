using ErsatzTV.Application.Channels;
using ErsatzTV.Application.Playouts;
using ErsatzTV.Application.ProgramSchedules;

namespace ErsatzTV.ViewModels;

public class PlayoutEditViewModel
{
    public string Kind { get; set; }
    public ChannelViewModel Channel { get; set; }
    public ProgramScheduleViewModel ProgramSchedule { get; set; }
    public string ScheduleFile { get; set; }

    public CreatePlayout ToCreate() =>
        Kind switch
        {
            PlayoutKind.ExternalJson => new CreateExternalJsonPlayout(Channel.Id, ScheduleFile),
            PlayoutKind.Sequential => new CreateSequentialPlayout(Channel.Id, ScheduleFile),
            PlayoutKind.Scripted => new CreateScriptedPlayout(Channel.Id, ScheduleFile),
            PlayoutKind.Block => new CreateBlockPlayout(Channel.Id),
            _ => new CreateClassicPlayout(Channel.Id, ProgramSchedule.Id)
        };
}
