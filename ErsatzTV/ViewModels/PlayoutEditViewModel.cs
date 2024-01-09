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

    public CreatePlayout ToCreate() => Kind == "externaljson"
        ? new CreateExternalJsonPlayout(Channel.Id, ExternalJsonFile)
        : new CreateFloodPlayout(Channel.Id, ProgramSchedule.Id);
}
