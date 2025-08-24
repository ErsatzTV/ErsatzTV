using System.Diagnostics.CodeAnalysis;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Scheduling;
using ErsatzTV.Core.Scheduling.Engine;
using ErsatzTV.Core.Scheduling.ScriptedScheduling.Modules;
using IronPython.Hosting;
using IronPython.Runtime;
using Microsoft.Extensions.Logging;
using Microsoft.Scripting.Hosting;

namespace ErsatzTV.Core.Scheduling.ScriptedScheduling;

public class ScriptedPlayoutBuilder(
    IConfigElementRepository configElementRepository,
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

            logger.LogInformation("Building scripted playout...");

            int daysToBuild = await GetDaysToBuild();
            DateTimeOffset finish = start.AddDays(daysToBuild);

            schedulingEngine.WithPlayoutId(playout.Id);
            schedulingEngine.WithMode(mode);
            schedulingEngine.WithSeed(playout.Seed);
            schedulingEngine.BuildBetween(start, finish);
            schedulingEngine.WithReferenceData(referenceData);

            var engine = Python.CreateEngine();
            var scope = engine.CreateScope();

            var sys = engine.GetSysModule();
            var modules = (PythonDictionary)sys.GetVariable("modules");

            var types = engine.ImportModule("types");
            dynamic moduleType = types.GetVariable("ModuleType");

            dynamic ersatztv = engine.Operations.Invoke(moduleType, "ersatztv");
            modules["ersatztv"] = ersatztv;

            var contentModule = new ContentModule(schedulingEngine);
            engine.Operations.SetMember(ersatztv, "content", contentModule);

            var playoutModule = new PlayoutModule(schedulingEngine);
            engine.Operations.SetMember(ersatztv, "playout", playoutModule);

            engine.ExecuteFile(playout.ScheduleFile, scope);

            // define_content is required
            if (!scope.TryGetVariable("define_content", out PythonFunction defineContentFunc))
            {
                logger.LogError("Script must contain a 'define_content' function");
                return result;
            }

            // reset_playout is NOT required
            scope.TryGetVariable("reset_playout", out PythonFunction resetPlayoutFunc);

            // build_playout is required
            if (!scope.TryGetVariable("build_playout", out PythonFunction buildPlayoutFunc))
            {
                logger.LogError("Script must contain a 'build_playout' function");
                return result;
            }

            schedulingEngine.RestoreOrReset(Optional(playout.Anchor));

            // define content first
            engine.Operations.Invoke(defineContentFunc);

            var context = new PythonPlayoutContext(schedulingEngine.GetState(), playoutModule, engine);

            // reset if applicable
            if (mode is PlayoutBuildMode.Reset && resetPlayoutFunc != null)
            {
                engine.Operations.Invoke(resetPlayoutFunc, context);
            }

            // build playout
            engine.Operations.Invoke(buildPlayoutFunc, context);

            playout.Anchor = schedulingEngine.GetAnchor();

            result = MergeResult(result, schedulingEngine.GetState());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error building scripted playout");
            throw;
        }

        return result;
    }

    private async Task<int> GetDaysToBuild() =>
        await configElementRepository
            .GetValue<int>(ConfigElementKey.PlayoutDaysToBuild)
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

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores")]
    public class PythonPlayoutContext
    {
        private readonly ISchedulingEngineState _state;
        private readonly PlayoutModule _playoutModule;
        private readonly ScriptEngine _scriptEngine;
        private readonly dynamic _datetimeModule;
        private const int MaxFailures = 10;

        public PythonPlayoutContext(
            ISchedulingEngineState state,
            PlayoutModule playoutModule,
            ScriptEngine scriptEngine)
        {
            _state = state;
            _playoutModule = playoutModule;
            _scriptEngine = scriptEngine;
            _datetimeModule = _scriptEngine.ImportModule("datetime");
        }

        public object current_time => ToPythonDateTime(_state.CurrentTime);
        //public object finish => ToPythonDateTime(_state.Finish);

        public bool is_done()
        {
            if (_playoutModule.FailureCount >= MaxFailures)
            {
                throw new InvalidOperationException(
                    $"Script execution halted after {MaxFailures} consecutive failures to add content."
                );
            }

            return _state.CurrentTime >= _state.Finish;
        }

        private object ToPythonDateTime(DateTimeOffset dto)
        {
            dynamic dt_constructor = _datetimeModule.datetime;
            dynamic timedelta_constructor = _datetimeModule.timedelta;
            dynamic timezone_constructor = _datetimeModule.timezone;

            var offset = dto.Offset;
            dynamic py_offset = _scriptEngine.Operations.Invoke(
                timedelta_constructor,
                0,
                (int)offset.TotalSeconds
            );

            dynamic py_tzinfo = _scriptEngine.Operations.Invoke(
                timezone_constructor,
                py_offset
            );

            return _scriptEngine.Operations.Invoke(
                dt_constructor,
                dto.Year,
                dto.Month,
                dto.Day,
                dto.Hour,
                dto.Minute,
                dto.Second,
                dto.Millisecond * 1000,
                py_tzinfo
            );
        }
    }
}
