using ErsatzTV.Application.Channels;
using ErsatzTV.Application.Playouts.Commands;
using ErsatzTV.Application.ProgramSchedules;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.ViewModels
{
    public class PlayoutEditViewModel
    {
        public ChannelViewModel Channel { get; set; }
        public ProgramScheduleViewModel ProgramSchedule { get; set; }

        public CreatePlayout ToCreate() =>
            new CreatePlayout(Channel.Id, ProgramSchedule.Id, ProgramSchedulePlayoutType.Flood);
    }
}
