using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CliFx;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace ErsatzTV.CommandLine
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();

            IHost host = CreateHostBuilder(args).Build();
            try
            {
                return await new CliApplicationBuilder()
                    .AddCommandsFromThisAssembly()
                    .UseTypeActivator(host.Services.GetService)
                    .Build()
                    .RunAsync(args);
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

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices(
                    (_, services) =>
                    {
                        services.AddSingleton<IConsole, SystemConsole>();
                        IEnumerable<Type> typesThatImplementICommand = typeof(Program).Assembly.GetTypes()
                            .Where(x => typeof(ICommand).IsAssignableFrom(x))
                            .Where(x => !x.IsAbstract);
                        foreach (Type t in typesThatImplementICommand)
                        {
                            services.AddTransient(t);
                        }
                    })
                .ConfigureAppConfiguration(
                    (_, configuration) =>
                    {
                        configuration.Sources.Clear();

                        string configFolder = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                            "ersatztv");

                        configuration.SetBasePath(configFolder);
                        configuration.AddJsonFile("cli.json", true, true);
                    })
                .UseSerilog()
                .UseConsoleLifetime();
    }
}
