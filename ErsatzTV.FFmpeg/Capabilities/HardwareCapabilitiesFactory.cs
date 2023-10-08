using System.Collections.Immutable;
using System.Globalization;
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
        // TODO: validate qsv somehow
        // TODO: validate videotoolbox somehow
        // TODO: validate amf somehow

        IReadOnlySet<string> ffmpegHardwareAccelerations =
            await GetFFmpegCapabilities(ffmpegPath, "hwaccels", ParseFFmpegAccelLine);
        IReadOnlySet<string> ffmpegDecoders = await GetFFmpegCapabilities(ffmpegPath, "decoders", ParseFFmpegLine);
        IReadOnlySet<string> ffmpegFilters = await GetFFmpegCapabilities(ffmpegPath, "filters", ParseFFmpegLine);
        IReadOnlySet<string> ffmpegEncoders = await GetFFmpegCapabilities(ffmpegPath, "encoders", ParseFFmpegLine);

        return new FFmpegCapabilities(ffmpegHardwareAccelerations, ffmpegDecoders, ffmpegFilters, ffmpegEncoders);
    }

    public async Task<IHardwareCapabilities> GetHardwareCapabilities(
        IFFmpegCapabilities ffmpegCapabilities,
        string ffmpegPath,
        HardwareAccelerationMode hardwareAccelerationMode,
        Option<string> vaapiDriver,
        Option<string> vaapiDevice)
    {
        if (!ffmpegCapabilities.HasHardwareAcceleration(hardwareAccelerationMode))
        {
            _logger.LogWarning(
                "FFmpeg does not support {HardwareAcceleration} acceleration; will use software mode",
                hardwareAccelerationMode);

            return new NoHardwareCapabilities();
        }
        
        return hardwareAccelerationMode switch
        {
            HardwareAccelerationMode.Nvenc => await GetNvidiaCapabilities(ffmpegPath, ffmpegCapabilities),
            HardwareAccelerationMode.Vaapi => await GetVaapiCapabilities(vaapiDriver, vaapiDevice),
            HardwareAccelerationMode.Amf => new AmfHardwareCapabilities(),
            _ => new DefaultHardwareCapabilities()
        };
    }

    public async Task<string> GetNvidiaOutput(string ffmpegPath)
    {
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

        return output;
    }

    public async Task<Option<string>> GetVaapiOutput(Option<string> vaapiDriver, string vaapiDevice)
    {
        BufferedCommandResult whichResult = await Cli.Wrap("which")
            .WithArguments("vainfo")
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync(Encoding.UTF8);

        if (whichResult.ExitCode != 0)
        {
            return Option<string>.None;
        }

        var envVars = new Dictionary<string, string?>();
        foreach (string libvaDriverName in vaapiDriver)
        {
            envVars.Add("LIBVA_DRIVER_NAME", libvaDriverName);
        }

        BufferedCommandResult result = await Cli.Wrap("vainfo")
            .WithArguments($"--display drm --device {vaapiDevice} -a")
            .WithEnvironmentVariables(envVars)
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync(Encoding.UTF8);

        return result.StandardOutput;
    }

    private async Task<IReadOnlySet<string>> GetFFmpegCapabilities(
        string ffmpegPath,
        string capabilities,
        Func<string, Option<string>> parseLine)
    {
        var cacheKey = string.Format(CultureInfo.InvariantCulture, FFmpegCapabilitiesCacheKeyFormat, capabilities);
        if (_memoryCache.TryGetValue(cacheKey, out IReadOnlySet<string>? cachedCapabilities) &&
            cachedCapabilities is not null)
        {
            return cachedCapabilities;
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
            .Bind(l => parseLine(l))
            .ToImmutableHashSet();
    }

    private static Option<string> ParseFFmpegAccelLine(string input)
    {
        const string PATTERN = @"^([\w]+)$";
        Match match = Regex.Match(input, PATTERN);
        return match.Success ? match.Groups[1].Value : Option<string>.None;
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
            var cacheKey = string.Format(CultureInfo.InvariantCulture, VaapiCacheKeyFormat, driver, device);

            if (_memoryCache.TryGetValue(cacheKey, out List<VaapiProfileEntrypoint>? profileEntrypoints) &&
                profileEntrypoints is not null)
            {
                return new VaapiHardwareCapabilities(profileEntrypoints, _logger);
            }

            Option<string> output = await GetVaapiOutput(vaapiDriver, device);
            if (output.IsNone)
            {
                _logger.LogWarning("Unable to determine VAAPI capabilities; please install vainfo");
                return new DefaultHardwareCapabilities();
            }

            foreach (string o in output)
            {
                profileEntrypoints = VaapiCapabilityParser.ParseFull(o);
            }

            if (profileEntrypoints?.Any() ?? false)
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

        string output = await GetNvidiaOutput(ffmpegPath);

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
            "Error detecting NVIDIA GPU capabilities; some hardware accelerated features will be unavailable");

        return new NoHardwareCapabilities();
    }
}
