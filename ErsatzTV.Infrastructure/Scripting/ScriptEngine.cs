using ErsatzTV.Core.Interfaces.Scripting;
using Jint;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Infrastructure.Scripting;

public class ScriptEngine : IScriptEngine
{
    private Engine _engine;

    public ScriptEngine(ILogger<ScriptEngine> logger) =>
        _engine = new Engine(
                options =>
                {
                    options.AllowClr();
                    options.LimitMemory(4_000_000);
                    options.TimeoutInterval(TimeSpan.FromSeconds(4));
                    options.MaxStatements(1000);
                })
            .SetValue("log", new Action<string>(s => logger.LogDebug("JS Script: {Message}", s)));

    public void Load(string jsScriptPath)
    {
        string contents = File.ReadAllText(jsScriptPath);
        _engine.Execute(contents);
    }

    public async Task LoadAsync(string jsScriptPath)
    {
        string contents = await File.ReadAllTextAsync(jsScriptPath);
        _engine.Execute(contents);
    }

    public object GetValue(string propertyName) => _engine.GetValue(propertyName).ToObject();

    public object Invoke(string functionName, params object[] args) => _engine.Invoke(functionName, args).ToObject();

    public void Dispose()
    {
        _engine?.Dispose();
        _engine = null;
    }
}
