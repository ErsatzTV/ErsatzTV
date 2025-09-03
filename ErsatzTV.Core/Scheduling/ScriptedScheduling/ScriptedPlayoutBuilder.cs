using CliWrap;
using CliWrap.Buffered;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Scheduling;
using ErsatzTV.Core.Scheduling.Engine;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Scheduling.ScriptedScheduling;

public class ScriptedPlayoutBuilder(
    IConfigElementRepository configElementRepository,
    IScriptedPlayoutBuilderService scriptedPlayoutBuilderService,
    ISchedulingEngine schedulingEngine,
    ILocalFileSystem localFileSystem,
    ILogger<ScriptedPlayoutBuilder> logger)
    : IScriptedPlayoutBuilder
{
    public async Task<PlayoutBuildResult> Build(
        DateTimeOffset start,
        Playout playout,
        PlayoutReferenceData referenceData,
        PlayoutBuildMode mode,
        CancellationToken cancellationToken)
    {
        var result = PlayoutBuildResult.Empty;

        try
        {
            if (!localFileSystem.FileExists(playout.ScheduleFile))
            {
                logger.LogError("Cannot build scripted playout; schedule file {File} does not exist", playout.ScheduleFile);
                return result;
            }

            Guid buildId = scriptedPlayoutBuilderService.StartSession(schedulingEngine);
            logger.LogInformation("Building scripted playout with id {BuildId} ...", buildId);

            int daysToBuild = await GetDaysToBuild(cancellationToken);
            DateTimeOffset finish = start.AddDays(daysToBuild);

            schedulingEngine.WithPlayoutId(playout.Id);
            schedulingEngine.WithMode(mode);
            schedulingEngine.WithSeed(playout.Seed);
            schedulingEngine.BuildBetween(start, finish);
            schedulingEngine.WithReferenceData(referenceData);

            schedulingEngine.RestoreOrReset(Optional(playout.Anchor));

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);

            Command command = Cli.Wrap(playout.ScheduleFile)
                .WithArguments(
                    [
                        $"http://localhost:{Settings.UiPort}",
                        buildId.ToString(),
                        mode.ToString().ToLowerInvariant()
                    ]);

            var commandResult = await command.ExecuteBufferedAsync(linkedCts.Token);
            if (!string.IsNullOrWhiteSpace(commandResult.StandardOutput))
            {
                Console.WriteLine(commandResult.StandardOutput);
            }

            playout.Anchor = schedulingEngine.GetAnchor();

            result = MergeResult(result, schedulingEngine.GetState());
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Scripted playout build timed out after 30 seconds");
            throw new TimeoutException("Scripted playout build timed out after 30 seconds");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unexpected exception building scripted playout");
            throw;
        }

        return result;
    }

    private async Task<int> GetDaysToBuild(CancellationToken cancellationToken) =>
        await configElementRepository
            .GetValue<int>(ConfigElementKey.PlayoutDaysToBuild, cancellationToken)
            .IfNoneAsync(2);

    private static PlayoutBuildResult MergeResult(PlayoutBuildResult result, ISchedulingEngineState state) =>
        result with
        {
            ClearItems = state.ClearItems,
            RemoveBefore = state.RemoveBefore,
            AddedItems = state.AddedItems,
            //ItemsToRemove = state.ItemsToRemove,
            AddedHistory = state.AddedHistory,
            HistoryToRemove = state.HistoryToRemove
        };
}
