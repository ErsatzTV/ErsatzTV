using System.Collections.Immutable;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using CliWrap;
using CliWrap.Buffered;
using ErsatzTV.FFmpeg.Capabilities.Qsv;
using ErsatzTV.FFmpeg.Capabilities.Vaapi;
using ErsatzTV.FFmpeg.GlobalOption.HardwareAcceleration;
using ErsatzTV.FFmpeg.Runtime;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.FFmpeg.Capabilities;

public class HardwareCapabilitiesFactory : IHardwareCapabilitiesFactory
{
    private const string ArchitectureCacheKey = "ffmpeg.hardware.nvidia.architecture";
    private const string ModelCacheKey = "ffmpeg.hardware.nvidia.model";

    private static readonly CompositeFormat
        VaapiCacheKeyFormat = CompositeFormat.Parse("ffmpeg.hardware.vaapi.{0}.{1}");

    private static readonly CompositeFormat QsvCacheKeyFormat = CompositeFormat.Parse("ffmpeg.hardware.qsv.{0}");
    private static readonly CompositeFormat FFmpegCapabilitiesCacheKeyFormat = CompositeFormat.Parse("ffmpeg.{0}");

    private static readonly string[] QsvArguments =
    {
        "-f", "lavfi",
        "-i", "nullsrc",
        "-t", "00:00:01",
        "-c:v", "h264_qsv",
        "-f", "null", "-"
    };

    private readonly ILogger<HardwareCapabilitiesFactory> _logger;

    private readonly IMemoryCache _memoryCache;
    private readonly IRuntimeInfo _runtimeInfo;

    public HardwareCapabilitiesFactory(
        IMemoryCache memoryCache,
        IRuntimeInfo runtimeInfo,
        ILogger<HardwareCapabilitiesFactory> logger)
    {
        _memoryCache = memoryCache;
        _runtimeInfo = runtimeInfo;
        _logger = logger;
    }

    public async Task<IFFmpegCapabilities> GetFFmpegCapabilities(string ffmpegPath)
    {
        // TODO: validate videotoolbox somehow
        // TODO: validate amf somehow

        IReadOnlySet<string> ffmpegHardwareAccelerations =
            await GetFFmpegCapabilities(ffmpegPath, "hwaccels", ParseFFmpegAccelLine)
                .Map(set => set.Intersect(FFmpegKnownHardwareAcceleration.AllAccels).ToImmutableHashSet());

        IReadOnlySet<string> ffmpegDecoders = await GetFFmpegCapabilities(ffmpegPath, "decoders", ParseFFmpegLine)
            .Map(set => set.Intersect(FFmpegKnownDecoder.AllDecoders).ToImmutableHashSet());

        IReadOnlySet<string> ffmpegFilters = await GetFFmpegCapabilities(ffmpegPath, "filters", ParseFFmpegLine)
            .Map(set => set.Intersect(FFmpegKnownFilter.AllFilters).ToImmutableHashSet());

        IReadOnlySet<string> ffmpegEncoders = await GetFFmpegCapabilities(ffmpegPath, "encoders", ParseFFmpegLine)
            .Map(set => set.Intersect(FFmpegKnownEncoder.AllEncoders).ToImmutableHashSet());

        IReadOnlySet<string> ffmpegOptions = await GetFFmpegOptions(ffmpegPath)
            .Map(set => set.Intersect(FFmpegKnownOption.AllOptions).ToImmutableHashSet());

        return new FFmpegCapabilities(
            ffmpegHardwareAccelerations,
            ffmpegDecoders,
            ffmpegFilters,
            ffmpegEncoders,
            ffmpegOptions);
    }

    public async Task<IHardwareCapabilities> GetHardwareCapabilities(
        IFFmpegCapabilities ffmpegCapabilities,
        string ffmpegPath,
        HardwareAccelerationMode hardwareAccelerationMode,
        Option<string> vaapiDriver,
        Option<string> vaapiDevice)
    {
        if (hardwareAccelerationMode is HardwareAccelerationMode.None)
        {
            return new NoHardwareCapabilities();
        }

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
            HardwareAccelerationMode.Qsv => await GetQsvCapabilities(ffmpegPath, vaapiDevice),
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

    public async Task<QsvOutput> GetQsvOutput(string ffmpegPath, Option<string> qsvDevice)
    {
        var option = new QsvHardwareAccelerationOption(qsvDevice);
        var arguments = option.GlobalOptions.ToList();

        arguments.AddRange(QsvArguments);

        BufferedCommandResult result = await Cli.Wrap(ffmpegPath)
            .WithArguments(arguments)
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync(Encoding.UTF8);

        string output = string.IsNullOrWhiteSpace(result.StandardOutput)
            ? result.StandardError
            : result.StandardOutput;

        return new QsvOutput(result.ExitCode, output);
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

    private async Task<IReadOnlySet<string>> GetFFmpegOptions(string ffmpegPath)
    {
        var cacheKey = string.Format(CultureInfo.InvariantCulture, FFmpegCapabilitiesCacheKeyFormat, "options");
        if (_memoryCache.TryGetValue(cacheKey, out IReadOnlySet<string>? cachedCapabilities) &&
            cachedCapabilities is not null)
        {
            return cachedCapabilities;
        }

        string[] arguments = { "-hide_banner", "-h", "long" };

        BufferedCommandResult result = await Cli.Wrap(ffmpegPath)
            .WithArguments(arguments)
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync(Encoding.UTF8);

        string output = string.IsNullOrWhiteSpace(result.StandardOutput)
            ? result.StandardError
            : result.StandardOutput;

        return output.Split("\n").Map(s => s.Trim())
            .Bind(l => ParseFFmpegOptionLine(l))
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

    private static Option<string> ParseFFmpegOptionLine(string input)
    {
        const string PATTERN = @"^-([a-z_]+)\s+.*";
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

            if (profileEntrypoints is not null && profileEntrypoints.Count != 0)
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

    private async Task<IHardwareCapabilities> GetQsvCapabilities(string ffmpegPath, Option<string> qsvDevice)
    {
        try
        {
            if (_runtimeInfo.IsOSPlatform(OSPlatform.Linux) && qsvDevice.IsNone)
            {
                // this shouldn't really happen
                _logger.LogError("Cannot detect QSV capabilities without device {Device}", qsvDevice);
                return new NoHardwareCapabilities();
            }

            string device = qsvDevice.IfNone(string.Empty);
            var cacheKey = string.Format(CultureInfo.InvariantCulture, QsvCacheKeyFormat, device);

            if (_memoryCache.TryGetValue(cacheKey, out List<VaapiProfileEntrypoint>? profileEntrypoints) &&
                profileEntrypoints is not null)
            {
                return new VaapiHardwareCapabilities(profileEntrypoints, _logger);
            }

            QsvOutput output = await GetQsvOutput(ffmpegPath, qsvDevice);
            if (output.ExitCode != 0)
            {
                _logger.LogWarning("QSV test failed; some hardware accelerated features will be unavailable");
                return new NoHardwareCapabilities();
            }

            if (_runtimeInfo.IsOSPlatform(OSPlatform.Linux))
            {
                Option<string> vaapiOutput = await GetVaapiOutput(Option<string>.None, device);
                if (vaapiOutput.IsNone)
                {
                    _logger.LogWarning("Unable to determine QSV capabilities; please install vainfo");
                    return new DefaultHardwareCapabilities();
                }

                foreach (string o in vaapiOutput)
                {
                    profileEntrypoints = VaapiCapabilityParser.ParseFull(o);
                }

                if (profileEntrypoints is not null && profileEntrypoints.Count != 0)
                {
                    _logger.LogInformation(
                        "Detected {Count} VAAPI profile entrypoints for using QSV device {Device}",
                        profileEntrypoints.Count,
                        device);

                    _memoryCache.Set(cacheKey, profileEntrypoints);
                    return new VaapiHardwareCapabilities(profileEntrypoints, _logger);
                }
            }

            // not sure how to check capabilities on windows
            return new DefaultHardwareCapabilities();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Error detecting QSV capabilities; some hardware accelerated features will be unavailable");
            return new NoHardwareCapabilities();
        }
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
