using System;
using System.IO;
using System.Threading.Tasks;
using ErsatzTV.Core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace ErsatzTV
{
    public class Program
    {
        static Program()
        {
            var executablePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            var executable = Path.GetFileNameWithoutExtension(executablePath);

            IConfigurationBuilder builder = new ConfigurationBuilder();

            if ("dotnet".Equals(executable, StringComparison.InvariantCultureIgnoreCase))
            {
                builder = builder.SetBasePath(Path.GetDirectoryName(typeof(Program).Assembly.Location));
            }
            else
            {
                builder = builder.SetBasePath(Path.GetDirectoryName(executablePath));
            }

            Configuration = builder
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
                        .UseKestrel(options => options.AddServerHeader = false))
                .UseSerilog();
    }
}
