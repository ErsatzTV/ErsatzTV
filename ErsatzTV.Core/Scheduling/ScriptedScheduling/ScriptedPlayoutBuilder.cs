using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Scheduling;
using ErsatzTV.Core.Scheduling.Engine;
using ErsatzTV.Core.Scheduling.ScriptedScheduling.Modules;
using IronPython.Hosting;
using IronPython.Runtime;
using Microsoft.Extensions.Logging;

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

            schedulingEngine.WithMode(mode);
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

            engine.ExecuteFile(playout.ScheduleFile, scope);

            // define_content is required
            if (!scope.TryGetVariable("define_content", out PythonFunction defineContentFunc))
            {
                logger.LogError("Script must contain a 'define_content' function");
                return result;
            }

            // on_reset is NOT required
            scope.TryGetVariable("on_reset", out PythonFunction onResetFunc);

            // build_playout is required
            if (!scope.TryGetVariable("build_playout", out PythonFunction buildPlayoutFunc))
            {
                logger.LogError("Script must contain a 'build_playout' function");
                return result;
            }

            // define content first
            engine.Operations.Invoke(defineContentFunc);

            // reset if applicable
            if (mode is PlayoutBuildMode.Reset && onResetFunc != null)
            {
                engine.Operations.Invoke(onResetFunc);
            }

            // build playout
            engine.Operations.Invoke(buildPlayoutFunc, schedulingEngine.GetState());
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
}
