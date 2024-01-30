using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using Destructurama;
using ErsatzTV.Core;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace ErsatzTV;

public class Program
{
    private static readonly string BasePath;

    static Program()
    {
        string executablePath = Environment.ProcessPath ?? string.Empty;
        string executable = Path.GetFileNameWithoutExtension(executablePath);

        IConfigurationBuilder builder = new ConfigurationBuilder();

        BasePath = Path.GetDirectoryName(
            "dotnet".Equals(executable, StringComparison.OrdinalIgnoreCase)
                ? typeof(Program).Assembly.Location
                : executablePath);

        Configuration = builder
            .SetBasePath(BasePath)
            .AddJsonFile("appsettings.json", false, true)
            .AddJsonFile(
                $"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json",
                true)
            .AddEnvironmentVariables()
            .Build();

        LoggingLevelSwitches = new LoggingLevelSwitches();
    }

    private static IConfiguration Configuration { get; }

    private static LoggingLevelSwitches LoggingLevelSwitches { get; }

    public static async Task<int> Main(string[] args)
    {
        using var _ = new Mutex(
            true,
            "Global\\ErsatzTV.Singleton.74360cd8985c4d1fb6bc9e81887206fe",
            out bool createdNew);
        if (!createdNew)
        {
            Console.WriteLine("Another instance of ErsatztTV is already running.");
            return 1;
        }

        LoggingLevelSwitches.DefaultLevelSwitch.MinimumLevel = LogEventLevel.Information;
        LoggingLevelSwitches.ScanningLevelSwitch.MinimumLevel = LogEventLevel.Information;
        LoggingLevelSwitches.SchedulingLevelSwitch.MinimumLevel = LogEventLevel.Information;
        LoggingLevelSwitches.StreamingLevelSwitch.MinimumLevel = LogEventLevel.Information;

        LoggerConfiguration loggerConfiguration = new LoggerConfiguration()
            .ReadFrom.Configuration(Configuration)
            
            .MinimumLevel.ControlledBy(LoggingLevelSwitches.DefaultLevelSwitch)

            // scanning
            .MinimumLevel.Override("ErsatzTV.Services.ScannerService", LoggingLevelSwitches.ScanningLevelSwitch)
            .MinimumLevel.Override("ErsatzTV.Scanner", LoggingLevelSwitches.ScanningLevelSwitch)

            // scheduling
            .MinimumLevel.Override("ErsatzTV.Core.Scheduling", LoggingLevelSwitches.SchedulingLevelSwitch)
            .MinimumLevel.Override("ErsatzTV.Application.Subtitles.ExtractEmbeddedSubtitlesHandler", LoggingLevelSwitches.SchedulingLevelSwitch)
            
            // streaming
            .MinimumLevel.Override("ErsatzTV.Application.Streaming", LoggingLevelSwitches.StreamingLevelSwitch)
            .MinimumLevel.Override("ErsatzTV.FFmpeg", LoggingLevelSwitches.StreamingLevelSwitch)
            .MinimumLevel.Override("ErsatzTV.Controllers.IptvController", LoggingLevelSwitches.StreamingLevelSwitch)
            
            .Destructure.UsingAttributes()
            .Enrich.FromLogContext()
            .WriteTo.File(
                FileSystemLayout.LogFilePath,
                rollingInterval: RollingInterval.Day,
                formatProvider: CultureInfo.InvariantCulture);

        // for performance reasons, restrict windows console to error logs
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !Debugger.IsAttached)
        {
            loggerConfiguration = loggerConfiguration.WriteTo.Console(
                LogEventLevel.Error,
                theme: AnsiConsoleTheme.Code,
                formatProvider: CultureInfo.InvariantCulture);
        }
        else
        {
            loggerConfiguration = loggerConfiguration.WriteTo.Console(
                theme: AnsiConsoleTheme.Code,
                formatProvider: CultureInfo.InvariantCulture);

            // for troubleshooting log category
            // outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} <{SourceContext:l}> {NewLine}{Exception}"
        }

        Log.Logger = loggerConfiguration.CreateLogger();

        try
        {
            await CreateHostBuilder(args).Build().RunAsync();
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host terminated unexpectedly");
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices(services => services.AddSingleton(LoggingLevelSwitches))
            .ConfigureWebHostDefaults(
                webBuilder => webBuilder.UseStartup<Startup>()
                    .UseConfiguration(Configuration)
                    .UseKestrel(options => options.AddServerHeader = false)
                    .UseContentRoot(BasePath))
            .UseSerilog();
}
