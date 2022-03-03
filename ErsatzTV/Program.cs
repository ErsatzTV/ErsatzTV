using ErsatzTV.Core;
using Serilog;

namespace ErsatzTV;

public class Program
{
    private static readonly string BasePath;

    static Program()
    {
        string executablePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
        string executable = Path.GetFileNameWithoutExtension(executablePath);

        IConfigurationBuilder builder = new ConfigurationBuilder();

        BasePath = Path.GetDirectoryName(
            "dotnet".Equals(executable, StringComparison.InvariantCultureIgnoreCase)
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
    }

    private static IConfiguration Configuration { get; }

    public static async Task<int> Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(Configuration)
            .Enrich.FromLogContext()
            .WriteTo.SQLite(FileSystemLayout.LogDatabasePath, retentionPeriod: TimeSpan.FromDays(1))
            .WriteTo.File(FileSystemLayout.LogFilePath, rollingInterval: RollingInterval.Day)
            .CreateLogger();

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
            Log.CloseAndFlush();
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(
                webBuilder => webBuilder.UseStartup<Startup>()
                    .UseConfiguration(Configuration)
                    .UseKestrel(options => options.AddServerHeader = false)
                    .UseContentRoot(BasePath))
            .UseSerilog();
}