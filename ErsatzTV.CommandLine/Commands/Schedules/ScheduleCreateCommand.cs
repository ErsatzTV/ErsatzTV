using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CliFx;
using CliFx.Attributes;
using ErsatzTV.Api.Sdk.Api;
using ErsatzTV.Api.Sdk.Model;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace ErsatzTV.CommandLine.Commands.Schedules
{
    [Command("schedule create", Description = "Creates a new schedule")]
    public class ScheduleCreateCommand : ICommand
    {
        private readonly ILogger<ScheduleCreateCommand> _logger;
        private readonly string _serverUrl;

        public ScheduleCreateCommand(IConfiguration configuration, ILogger<ScheduleCreateCommand> logger)
        {
            _logger = logger;
            _serverUrl = configuration["ServerUrl"];
        }

        [CommandParameter(0, Name = "schedule-name", Description = "The schedule name")]
        public string Name { get; set; }

        [CommandParameter(1, Name = "playback-order", Description = "The collection playback order")]
        public PlaybackOrder Order { get; set; }

        [CommandOption("reset", Description = "Resets the schedule to contain no items")]
        public bool Reset { get; set; }

        public async ValueTask ExecuteAsync(IConsole console)
        {
            try
            {
                CancellationToken cancellationToken = console.GetCancellationToken();

                Either<Error, Unit> result = await EnsureScheduleExistsAsync(cancellationToken);
                result.IfLeft(error => _logger.LogError("Unable to create schedule: {Error}", error.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError("Unable to create schedule: {Error}", ex.Message);
            }
        }

        private async Task<Either<Error, Unit>> EnsureScheduleExistsAsync(CancellationToken cancellationToken)
        {
            var programScheduleApi = new ProgramScheduleApi(_serverUrl);

            Option<ProgramScheduleViewModel> maybeExisting = await programScheduleApi
                .ApiSchedulesGetAsync(cancellationToken)
                .Map(list => list.SingleOrDefault(schedule => schedule.Name == Name));

            await maybeExisting.Match(
                existing =>
                {
                    // TODO: update playback order if changed?
                    _logger.LogInformation("Schedule {Schedule} is already present", Name);

                    if (Reset)
                    {
                        return programScheduleApi
                            .ApiSchedulesProgramScheduleIdItemsDeleteAsync(existing.Id, cancellationToken)
                            .Iter(_ => _logger.LogInformation("Successfully reset schedule {Schedule}", Name));
                    }

                    return Task.CompletedTask;
                },
                () =>
                {
                    var data = new CreateProgramSchedule(Name, Order);
                    return programScheduleApi.ApiSchedulesPostAsync(data, cancellationToken)
                        .Iter(_ => _logger.LogInformation("Successfully created schedule {Schedule}", Name));
                });

            return unit;
        }
    }
}
