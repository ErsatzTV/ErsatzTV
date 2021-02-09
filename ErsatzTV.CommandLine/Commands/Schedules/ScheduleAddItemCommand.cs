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

namespace ErsatzTV.CommandLine.Commands.Schedules
{
    [Command("schedule add-item", Description = "Adds an item to the end of a schedule")]
    public class ScheduleAddItemCommand : ICommand
    {
        private readonly ILogger<ScheduleAddItemCommand> _logger;
        private readonly string _serverUrl;

        public ScheduleAddItemCommand(IConfiguration configuration, ILogger<ScheduleAddItemCommand> logger)
        {
            _logger = logger;
            _serverUrl = configuration["ServerUrl"];
        }

        [CommandParameter(0, Name = "schedule-name", Description = "The schedule name")]
        public string ScheduleName { get; set; }

        [CommandParameter(1, Name = "collection-name", Description = "The media collection name")]
        public string CollectionName { get; set; }

        // [CommandParameter(2, Description = "The collection playback order")]
        // public PlaybackOrder Order { get; set; }

        [CommandOption("start-type", 's', Description = "The playout start type")]
        public StartType StartType { get; set; } = StartType.Dynamic;

        [CommandOption("start-time", 't', Description = "The playout start time (of day)")]
        public string StartTime { get; set; } = null;

        [CommandOption("playout-mode", 'm', Description = "The playout mode")]
        public PlayoutMode PlayoutMode { get; set; } = PlayoutMode.Flood;

        [CommandOption(
            "multiple-count",
            'c',
            Description = "How many items to play from the collection (for Multiple playout mode)")]
        public int? MultipleCount { get; set; } = null;

        [CommandOption(
            "playout-duration",
            'd',
            Description = "How long to play items from the collection (for Duration playout mode)")]
        public string PlayoutDuration { get; set; } = null;

        [CommandOption(
            "offline-tail",
            'o',
            Description =
                "Whether to remain offline for the entire duration, or to start the next item immediately (for Duration playout mode)")]
        public bool? OfflineTail { get; set; } = null;

        public async ValueTask ExecuteAsync(IConsole console)
        {
            try
            {
                CancellationToken cancellationToken = console.GetCancellationToken();

                Option<ProgramScheduleViewModel> maybeSchedule = await GetSchedule(cancellationToken);
                await maybeSchedule.Match(
                    programSchedule => AddItemToSchedule(cancellationToken, programSchedule),
                    () =>
                    {
                        _logger.LogError("Unable to locate schedule {Schedule}", ScheduleName);
                        return ValueTask.CompletedTask;
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError("Unable to add item to schedule: {Error}", ex.Message);
            }
        }

        private async ValueTask AddItemToSchedule(
            CancellationToken cancellationToken,
            ProgramScheduleViewModel programSchedule)
        {
            var mediaCollectionsApi = new MediaCollectionsApi(_serverUrl);
            Option<MediaCollectionViewModel> maybeMediaCollection = await mediaCollectionsApi
                .ApiMediaCollectionsGetAsync(cancellationToken)
                .Map(list => list.SingleOrDefault(mc => mc.Name == CollectionName));

            await maybeMediaCollection.Match(
                collection =>
                    AddScheduleItem(programSchedule.Id, collection.Id, cancellationToken),
                () =>
                {
                    _logger.LogError(
                        "Unable to locate media collection {MediaCollection}",
                        CollectionName);
                    return Task.CompletedTask;
                });
        }

        private async Task<Option<ProgramScheduleViewModel>> GetSchedule(CancellationToken cancellationToken)
        {
            var programScheduleApi = new ProgramScheduleApi(_serverUrl);
            return await programScheduleApi.ApiSchedulesGetAsync(cancellationToken)
                .Map(list => list.SingleOrDefault(schedule => schedule.Name == ScheduleName));
        }

        private async Task AddScheduleItem(
            int programScheduleId,
            int mediaCollectionId,
            CancellationToken cancellationToken)
        {
            var programScheduleApi = new ProgramScheduleApi(_serverUrl);

            var request = new AddProgramScheduleItem
            {
                ProgramScheduleId = programScheduleId,
                StartType = StartType,
                StartTime = StartTime,
                PlayoutMode = PlayoutMode,
                MediaCollectionId = mediaCollectionId,
                PlayoutDuration = PlayoutDuration,
                MultipleCount = MultipleCount,
                OfflineTail = OfflineTail
            };

            await programScheduleApi.ApiSchedulesItemsAddPostAsync(request, cancellationToken);

            _logger.LogInformation(
                "Collection {Collection} has been added to schedule {Schedule}",
                CollectionName,
                ScheduleName);
        }
    }
}
