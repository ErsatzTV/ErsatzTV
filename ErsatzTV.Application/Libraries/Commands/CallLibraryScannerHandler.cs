using System.Runtime.InteropServices;
using CliWrap;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.FFmpeg.Runtime;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Compact.Reader;

namespace ErsatzTV.Application.Libraries;

public abstract class CallLibraryScannerHandler<TRequest>(
    IDbContextFactory<TvContext> dbContextFactory,
    IConfigElementRepository configElementRepository,
    IRuntimeInfo runtimeInfo)
{
    protected static string GetBaseUrl(Guid scanId) => $"http://localhost:{Settings.UiPort}/api/scan/{scanId}";

    protected async Task<Either<BaseError, string>> PerformScan(
        ScanParameters parameters,
        List<string> arguments,
        CancellationToken cancellationToken)
    {
        try
        {
            using var forcefulCts = new CancellationTokenSource();

            await using CancellationTokenRegistration link =
                cancellationToken.Register(() => forcefulCts.CancelAfter(TimeSpan.FromSeconds(10)));

            CommandResult process = await Cli.Wrap(parameters.Scanner)
                .WithArguments(arguments)
                .WithValidation(CommandResultValidation.None)
                .WithStandardErrorPipe(PipeTarget.ToDelegate(ProcessLogOutput))
                .WithStandardOutputPipe(PipeTarget.Null)
                .ExecuteAsync(forcefulCts.Token, cancellationToken);

            if (process.ExitCode != 0)
            {
                return BaseError.New($"ErsatzTV.Scanner exited with code {process.ExitCode}");
            }
        }
        catch (Exception ex) when (ex is TaskCanceledException or OperationCanceledException)
        {
            // do nothing
        }

        return parameters.LibraryName;
    }

    private static void ProcessLogOutput(string s)
    {
        if (!string.IsNullOrWhiteSpace(s))
        {
            try
            {
                // make a new log event to force using local time
                // because the compact json writer used by the scanner
                // writes in UTC
                LogEvent logEvent = LogEventReader.ReadFromString(s);

                ILogger log = Log.Logger;
                if (logEvent.Properties.TryGetValue("SourceContext", out LogEventPropertyValue property))
                {
                    log = log.ForContext(
                        Constants.SourceContextPropertyName,
                        property.ToString().Trim('"'));
                }

                log.Write(
                    new LogEvent(
                        logEvent.Timestamp.ToLocalTime(),
                        logEvent.Level,
                        logEvent.Exception,
                        logEvent.MessageTemplate,
                        logEvent.Properties.Map(pair => new LogEventProperty(pair.Key, pair.Value))));
            }
            catch
            {
                Console.WriteLine(s);
            }
        }
    }

    protected abstract Task<Tuple<string, DateTimeOffset>> GetLastScan(
        TvContext dbContext,
        TRequest request,
        CancellationToken cancellationToken);

    protected abstract bool ScanIsRequired(DateTimeOffset lastScan, int libraryRefreshInterval, TRequest request);

    protected async Task<Validation<BaseError, ScanParameters>> Validate(TRequest request, CancellationToken cancellationToken)
    {
        try
        {
            int libraryRefreshInterval = await configElementRepository
                .GetValue<int>(ConfigElementKey.LibraryRefreshInterval, cancellationToken)
                .IfNoneAsync(0);

            libraryRefreshInterval = Math.Clamp(libraryRefreshInterval, 0, 999_999);

            await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            (string libraryName, DateTimeOffset lastScan) = await GetLastScan(dbContext, request, cancellationToken);
            if (!ScanIsRequired(lastScan, libraryRefreshInterval, request))
            {
                return new ScanIsNotRequired();
            }

            string executable = runtimeInfo.IsOSPlatform(OSPlatform.Windows)
                ? "ErsatzTV.Scanner.exe"
                : "ErsatzTV.Scanner";

            string processFileName = Environment.ProcessPath ?? string.Empty;
            string processExecutable = Path.GetFileNameWithoutExtension(processFileName);
            string folderName = Path.GetDirectoryName(processFileName);
            if ("dotnet".Equals(processExecutable, StringComparison.OrdinalIgnoreCase))
            {
                folderName = AppContext.BaseDirectory;
            }

            if (!string.IsNullOrWhiteSpace(folderName))
            {
                string localFileName = Path.Combine(folderName, executable);
                if (File.Exists(localFileName))
                {
                    return new ScanParameters(libraryName, localFileName);
                }
            }

            return BaseError.New("Unable to locate ErsatzTV.Scanner executable");
        }
        catch (Exception ex) when (ex is TaskCanceledException or OperationCanceledException)
        {
            return BaseError.New("Scan was canceled");
        }
    }

    protected sealed record ScanParameters(string LibraryName, string Scanner);
}
