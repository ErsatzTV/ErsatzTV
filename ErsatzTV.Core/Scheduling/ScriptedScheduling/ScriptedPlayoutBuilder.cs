using System.CommandLine.Parsing;
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

        Guid buildId = scriptedPlayoutBuilderService.StartSession(schedulingEngine);

        var timeoutSeconds = 30;

        try
        {
            var args = CommandLineParser.SplitCommandLine(playout.ScheduleFile).ToList();

            string scriptFile = args[0];
            string[] scriptArgs = args.Skip(1).ToArray();

            if (!localFileSystem.FileExists(scriptFile))
            {
                logger.LogError(
                    "Cannot build scripted playout; schedule file {File} does not exist",
                    scriptFile);
                return result;
            }

            var arguments = new List<string>
            {
                $"http://localhost:{Settings.UiPort}",
                buildId.ToString(),
                mode.ToString().ToLowerInvariant()
            };

            if (scriptArgs.Length > 0)
            {
                arguments.AddRange(scriptArgs);
            }

            logger.LogInformation(
                "Building scripted playout {Script} with arguments {Arguments}",
                scriptFile,
                arguments);

            int daysToBuild = await GetDaysToBuild(cancellationToken);
            DateTimeOffset finish = start.AddDays(daysToBuild);

            schedulingEngine.WithPlayoutId(playout.Id);
            schedulingEngine.WithMode(mode);
            schedulingEngine.WithSeed(playout.Seed);
            schedulingEngine.BuildBetween(start, finish);
            schedulingEngine.WithReferenceData(referenceData);

            schedulingEngine.RestoreOrReset(Optional(playout.Anchor));

            Option<int> maybeTimeoutSeconds = await configElementRepository.GetValue<int>(
                ConfigElementKey.PlayoutScriptedScheduleTimeoutSeconds,
                cancellationToken);

            foreach (int seconds in maybeTimeoutSeconds)
            {
                timeoutSeconds = seconds;
            }

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);

            Command command = Cli.Wrap(scriptFile)
                .WithArguments(arguments)
                .WithValidation(CommandResultValidation.None);

            var commandResult = await command.ExecuteBufferedAsync(linkedCts.Token);
            if (!string.IsNullOrWhiteSpace(commandResult.StandardOutput))
            {
                logger.LogDebug("Scripted playout output: {Output}", commandResult.StandardOutput);
            }

            if (commandResult.ExitCode != 0)
            {
                logger.LogWarning(
                    "Scripted playout process exited with code {Code}: {Error}",
                    commandResult.ExitCode,
                    commandResult.StandardError);
            }

            playout.Anchor = schedulingEngine.GetAnchor();

            result = MergeResult(result, schedulingEngine.GetState());
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Scripted playout build timed out after {TimeoutSeconds} seconds", timeoutSeconds);
            throw new TimeoutException($"Scripted playout build timed out after {timeoutSeconds} seconds");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unexpected exception building scripted playout");
            throw;
        }
        finally
        {
            scriptedPlayoutBuilderService.EndSession(buildId);
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
