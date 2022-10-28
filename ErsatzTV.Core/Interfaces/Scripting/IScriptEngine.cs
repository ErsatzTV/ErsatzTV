namespace ErsatzTV.Core.Interfaces.Scripting;

public interface IScriptEngine : IDisposable
{
    void Load(string jsScriptPath);
    Task LoadAsync(string jsScriptPath);
    object GetValue(string propertyName);
    object Invoke(string functionName, params object[] args);
}
