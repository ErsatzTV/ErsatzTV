using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using CliWrap;
using CliWrap.Buffered;
using ErsatzTV.FFmpeg.Capabilities.Vaapi;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.FFmpeg.Capabilities;

public class HardwareCapabilitiesFactory : IHardwareCapabilitiesFactory
{
    private const string ArchitectureCacheKey = "ffmpeg.hardware.nvidia.architecture";
    private const string ModelCacheKey = "ffmpeg.hardware.nvidia.model";
    private const string VaapiCacheKeyFormat = "ffmpeg.hardware.vaapi.{0}.{1}";
    private const string FFmpegCapabilitiesCacheKeyFormat = "ffmpeg.{0}";
    private readonly ILogger<HardwareCapabilitiesFactory> _logger;

    private readonly IMemoryCache _memoryCache;

    public HardwareCapabilitiesFactory(IMemoryCache memoryCache, ILogger<HardwareCapabilitiesFactory> logger)
    {
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public async Task<IFFmpegCapabilities> GetFFmpegCapabilities(string ffmpegPath)
    {
        IReadOnlySet<string> ffmpegDecoders = await GetFFmpegCapabilities(ffmpegPath, "decoders");
        IReadOnlySet<string> ffmpegFilters = await GetFFmpegCapabilities(ffmpegPath, "filters");
        IReadOnlySet<string> ffmpegEncoders = await GetFFmpegCapabilities(ffmpegPath, "encoders");

        return new FFmpegCapabilities(ffmpegDecoders, ffmpegFilters, ffmpegEncoders);
    }

    public async Task<IHardwareCapabilities> GetHardwareCapabilities(
        IFFmpegCapabilities ffmpegCapabilities,
        string ffmpegPath,
        HardwareAccelerationMode hardwareAccelerationMode,
        Option<string> vaapiDriver,
        Option<string> vaapiDevice)
    {
        return hardwareAccelerationMode switch
        {
            HardwareAccelerationMode.Nvenc => await GetNvidiaCapabilities(ffmpegPath, ffmpegCapabilities),
            HardwareAccelerationMode.Vaapi => await GetVaapiCapabilities(vaapiDriver, vaapiDevice),
            HardwareAccelerationMode.Amf => new AmfHardwareCapabilities(),
            _ => new DefaultHardwareCapabilities()
        };
    }

    private async Task<IReadOnlySet<string>> GetFFmpegCapabilities(string ffmpegPath, string capabilities)
    {
        var cacheKey = string.Format(FFmpegCapabilitiesCacheKeyFormat, capabilities);
        if (_memoryCache.TryGetValue(cacheKey, out IReadOnlySet<string>? cachedDecoders) &&
            cachedDecoders is not null)
        {
            return cachedDecoders;
        }
        
        string[] arguments = { "-hide_banner", $"-{capabilities}" };

        BufferedCommandResult result = await Cli.Wrap(ffmpegPath)
            .WithArguments(arguments)
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync(Encoding.UTF8);

        string output = string.IsNullOrWhiteSpace(result.StandardOutput)
            ? result.StandardError
            : result.StandardOutput;

        return output.Split("\n").Map(s => s.Trim())
            .Bind(l => ParseFFmpegLine(l))
            .ToImmutableHashSet();
    }

    private static Option<string> ParseFFmpegLine(string input)
    {
        const string PATTERN = @"^\s*?[A-Z\.]+\s+(\w+).*";
        Match match = Regex.Match(input, PATTERN);
        return match.Success ? match.Groups[1].Value : Option<string>.None;
    }

    private async Task<IHardwareCapabilities> GetVaapiCapabilities(
        Option<string> vaapiDriver,
        Option<string> vaapiDevice)
    {
        try
        {
            if (vaapiDevice.IsNone)
            {
                // this shouldn't really happen

                _logger.LogError(
                    "Cannot detect VAAPI capabilities without device {Device}",
                    vaapiDevice);

                return new NoHardwareCapabilities();
            }

            string driver = vaapiDriver.IfNone(string.Empty);
            string device = vaapiDevice.IfNone(string.Empty);
            var cacheKey = string.Format(VaapiCacheKeyFormat, driver, device);

            if (_memoryCache.TryGetValue(cacheKey, out List<VaapiProfileEntrypoint>? profileEntrypoints) &&
                profileEntrypoints is not null)
            {
                return new VaapiHardwareCapabilities(profileEntrypoints, _logger);
            }

            BufferedCommandResult whichResult = await Cli.Wrap("which")
                .WithArguments("vainfo")
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync(Encoding.UTF8);

            if (whichResult.ExitCode != 0)
            {
                _logger.LogWarning("Unable to determine VAAPI capabilities; please install vainfo");
                return new DefaultHardwareCapabilities();
            }
            
            var envVars = new Dictionary<string, string?>();
            foreach (string libvaDriverName in vaapiDriver)
            {
                envVars.Add("LIBVA_DRIVER_NAME", libvaDriverName);
            }

            BufferedCommandResult result = await Cli.Wrap("vainfo")
                .WithArguments($"--display drm --device {device}")
                .WithEnvironmentVariables(envVars)
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync(Encoding.UTF8);

            profileEntrypoints = new List<VaapiProfileEntrypoint>();

            foreach (string line in result.StandardOutput.Split("\n"))
            {
                const string PROFILE_ENTRYPOINT_PATTERN = @"(VAProfile\w*).*(VAEntrypoint\w*)";
                Match match = Regex.Match(line, PROFILE_ENTRYPOINT_PATTERN);
                if (match.Success)
                {
                    profileEntrypoints.Add(
                        new VaapiProfileEntrypoint(
                            match.Groups[1].Value.Trim(),
                            match.Groups[2].Value.Trim()));
                }
            }

            if (profileEntrypoints.Any())
            {
                _logger.LogInformation(
                    "Detected {Count} VAAPI profile entrypoints for using {Driver} {Device}",
                    profileEntrypoints.Count,
                    driver,
                    device);
                _memoryCache.Set(cacheKey, profileEntrypoints);
                return new VaapiHardwareCapabilities(profileEntrypoints, _logger);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Error detecting VAAPI capabilities; some hardware accelerated features will be unavailable");
            return new NoHardwareCapabilities();
        }

        _logger.LogWarning(
            "Error detecting VAAPI capabilities; some hardware accelerated features will be unavailable");

        return new NoHardwareCapabilities();
    }

    private async Task<IHardwareCapabilities> GetNvidiaCapabilities(
        string ffmpegPath,
        IFFmpegCapabilities ffmpegCapabilities)
    {
        if (_memoryCache.TryGetValue(ArchitectureCacheKey, out int cachedArchitecture)
            && _memoryCache.TryGetValue(ModelCacheKey, out string? cachedModel)
            && cachedModel is not null)
        {
            return new NvidiaHardwareCapabilities(
                cachedArchitecture,
                cachedModel,
                ffmpegCapabilities,
                _logger);
        }

        string[] arguments =
        {
            "-f", "lavfi",
            "-i", "nullsrc",
            "-c:v", "h264_nvenc",
            "-gpu", "list",
            "-f", "null", "-"
        };

        BufferedCommandResult result = await Cli.Wrap(ffmpegPath)
            .WithArguments(arguments)
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync(Encoding.UTF8);

        string output = string.IsNullOrWhiteSpace(result.StandardOutput)
            ? result.StandardError
            : result.StandardOutput;

        Option<string> maybeLine = Optional(output.Split("\n").FirstOrDefault(x => x.Contains("GPU")));
        foreach (string line in maybeLine)
        {
            const string ARCHITECTURE_PATTERN = @"SM\s+(\d\.\d)";
            Match match = Regex.Match(line, ARCHITECTURE_PATTERN);
            if (match.Success && int.TryParse(match.Groups[1].Value.Replace(".", string.Empty), out int architecture))
            {
                const string MODEL_PATTERN = @"(GTX\s+[0-9a-zA-Z]+[\sTtIi]+)";
                Match modelMatch = Regex.Match(line, MODEL_PATTERN);
                string model = modelMatch.Success ? modelMatch.Groups[1].Value.Trim() : "unknown";
                _logger.LogInformation(
                    "Detected NVIDIA GPU model {Model} architecture SM {Architecture}",
                    model,
                    architecture);
                _memoryCache.Set(ArchitectureCacheKey, architecture);
                _memoryCache.Set(ModelCacheKey, model);
                return new NvidiaHardwareCapabilities(architecture, model, ffmpegCapabilities, _logger);
            }
        }

        _logger.LogWarning(
            "Error detecting NVIDIA GPU capabilities; some hardware accelerated features will be unavailable: {ExitCode}",
            result.ExitCode);

        return new NoHardwareCapabilities();
    }
}
