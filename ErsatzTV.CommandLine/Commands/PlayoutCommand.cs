using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CliFx;
using CliFx.Attributes;
using ErsatzTV.Api.Sdk.Api;
using ErsatzTV.Api.Sdk.Model;
using LanguageExt;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.CommandLine.Commands
{
    [Command("playout build", Description = "Builds a playout with the requested channel and schedule")]
    public class PlayoutCommand : ICommand
    {
        private readonly ILogger<PlayoutCommand> _logger;
        private readonly string _serverUrl;

        public PlayoutCommand(IConfiguration configuration, ILogger<PlayoutCommand> logger)
        {
            _logger = logger;
            _serverUrl = configuration["ServerUrl"];
        }

        [CommandParameter(0, Name = "channel-number", Description = "The channel number")]
        public int ChannelNumber { get; set; }

        [CommandParameter(1, Name = "schedule-name", Description = "The schedule name")]
        public string ScheduleName { get; set; }

        // [Option("--type <type>")]
        // [Required]
        // public ProgramSchedulePlayoutType PlayoutType { get; set; }

        public async ValueTask ExecuteAsync(IConsole console)
        {
            try
            {
                CancellationToken cancellationToken = console.GetCancellationToken();

                var channelsApi = new ChannelsApi(_serverUrl);
                Option<ChannelViewModel> maybeChannel = await channelsApi.ApiChannelsGetAsync(cancellationToken)
                    .Map(list => list.SingleOrDefault(c => c.Number == ChannelNumber));

                await maybeChannel.Match(
                    channel => BuildPlayout(cancellationToken, channel),
                    () =>
                    {
                        _logger.LogError("Unable to locate channel number {ChannelNumber}", ChannelNumber);
                        return ValueTask.CompletedTask;
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError("Unable to build playout: {Error}", ex.Message);
            }
        }

        private async ValueTask BuildPlayout(CancellationToken cancellationToken, ChannelViewModel channel)
        {
            var programScheduleApi = new ProgramScheduleApi(_serverUrl);
            Option<ProgramScheduleViewModel> maybeSchedule = await programScheduleApi
                .ApiSchedulesGetAsync(cancellationToken)
                .Map(list => list.SingleOrDefault(s => s.Name == ScheduleName));

            await maybeSchedule.Match(
                schedule => SynchronizePlayoutAsync(channel.Id, schedule.Id, cancellationToken),
                () =>
                {
                    _logger.LogError("Unable to locate schedule {Schedule}", ScheduleName);
                    return ValueTask.CompletedTask;
                });
        }

        private async ValueTask SynchronizePlayoutAsync(
            int channelId,
            int scheduleId,
            CancellationToken cancellationToken)
        {
            var playoutApi = new PlayoutApi(_serverUrl);
            Option<PlayoutViewModel> maybeExisting = await playoutApi.ApiPlayoutsGetAsync(cancellationToken)
                .Map(list => list.SingleOrDefault(p => p.Channel.Id == channelId));
            await maybeExisting.Match(
                existing =>
                {
                    var data = new UpdatePlayout(existing.Id, channelId, scheduleId, ProgramSchedulePlayoutType.Flood);
                    if (existing.Channel.Id != data.ChannelId ||
                        existing.ProgramSchedule.Id != data.ProgramScheduleId ||
                        existing.ProgramSchedulePlayoutType != data.ProgramSchedulePlayoutType)
                    {
                        return playoutApi.ApiPlayoutsPatchAsync(data, cancellationToken);
                    }

                    return Task.CompletedTask;
                },
                () =>
                {
                    var data = new CreatePlayout(channelId, scheduleId, ProgramSchedulePlayoutType.Flood);
                    return playoutApi.ApiPlayoutsPostAsync(data, cancellationToken);
                });

            _logger.LogInformation("Successfully built playout for schedule {Schedule}", ScheduleName);
        }
    }
}
