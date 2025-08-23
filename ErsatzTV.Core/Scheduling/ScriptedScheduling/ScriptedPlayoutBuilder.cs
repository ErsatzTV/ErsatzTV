using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Scheduling;
using ErsatzTV.Core.Scheduling.ScriptedScheduling.Modules;
using IronPython.Hosting;
using IronPython.Runtime;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Scheduling.ScriptedScheduling;

public class ScriptedPlayoutBuilder(
    //IConfigElementRepository configElementRepository,
    //IMediaCollectionRepository mediaCollectionRepository,
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
        await Task.Delay(10, cancellationToken);

        var result = PlayoutBuildResult.Empty;

        if (!localFileSystem.FileExists(playout.ScheduleFile))
        {
            logger.LogError("Cannot build scripted playout; schedule file {File} does not exist", playout.ScheduleFile);
            return result;
        }

        logger.LogInformation("Building scripted playout...");

        //int daysToBuild = await GetDaysToBuild();
        //DateTimeOffset finish = start.AddDays(daysToBuild);

        //var enumeratorCache = new EnumeratorCache(mediaCollectionRepository, logger);

        // apply all history???

        try
        {
            var engine = Python.CreateEngine();
            var scope = engine.CreateScope();

            var sys = engine.GetSysModule();
            var modules = (PythonDictionary)sys.GetVariable("modules");

            var types = engine.ImportModule("types");
            dynamic moduleType = types.GetVariable("ModuleType");

            dynamic ersatztv = engine.Operations.Invoke(moduleType, "ersatztv");
            modules["ersatztv"] = ersatztv;

            var contentModule = new ContentModule();
            engine.Operations.SetMember(ersatztv, "content", contentModule);

            engine.ExecuteFile(playout.ScheduleFile, scope);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error building scripted playout");
            throw;
        }

        return result;
    }

    // private async Task<int> GetDaysToBuild() =>
    //     await configElementRepository
    //         .GetValue<int>(ConfigElementKey.PlayoutDaysToBuild)
    //         .IfNoneAsync(2);
}
