using System.Collections.Immutable;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using CliWrap;
using CliWrap.Buffered;
using ErsatzTV.FFmpeg.Capabilities.Nvidia;
using ErsatzTV.FFmpeg.Capabilities.Qsv;
using ErsatzTV.FFmpeg.Capabilities.Vaapi;
using ErsatzTV.FFmpeg.Capabilities.VideoToolbox;
using ErsatzTV.FFmpeg.GlobalOption.HardwareAcceleration;
using ErsatzTV.FFmpeg.Runtime;
using Hardware.Info;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.FFmpeg.Capabilities;

public class HardwareCapabilitiesFactory : IHardwareCapabilitiesFactory
{
    private const string CudaDeviceKey = "ffmpeg.hardware.cuda.device";

    private static readonly CompositeFormat
        VaapiCacheKeyFormat = CompositeFormat.Parse("ffmpeg.hardware.vaapi.{0}.{1}.{2}");

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
        Option<string> vaapiDisplay,
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
            HardwareAccelerationMode.Nvenc => GetNvidiaCapabilities(ffmpegCapabilities),
            HardwareAccelerationMode.Qsv => await GetQsvCapabilities(ffmpegPath, vaapiDevice),
            HardwareAccelerationMode.Vaapi => await GetVaapiCapabilities(vaapiDisplay, vaapiDriver, vaapiDevice),
            HardwareAccelerationMode.VideoToolbox => new VideoToolboxHardwareCapabilities(ffmpegCapabilities, _logger),
            HardwareAccelerationMode.Amf => new AmfHardwareCapabilities(),
            HardwareAccelerationMode.V4l2m2m => new V4l2m2mHardwareCapabilities(ffmpegCapabilities),
            HardwareAccelerationMode.Rkmpp => new RkmppHardwareCapabilities(),
            _ => new DefaultHardwareCapabilities()
        };
    }

    public async Task<string> GetNvidiaOutput(string ffmpegPath)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return string.Empty;
        }

        try
        {
            Option<List<CudaDevice>> maybeDevices = CudaHelper.GetDevices();
            foreach (List<CudaDevice> devices in maybeDevices.Where(list => list.Count > 0))
            {
                var sb = new StringBuilder();
                foreach (CudaDevice device in devices)
                {
                    sb.AppendLine(
                        CultureInfo.InvariantCulture,
                        $"GPU #{device.Handle} < {device.Model} > has Compute SM {device.Version.Major}.{device.Version.Minor}");

                    sb.AppendLine(CudaHelper.GetDeviceDetails(device));
                }

                return sb.ToString();
            }
        }
        catch (FileNotFoundException)
        {
            // do nothing
        }

        // if we don't have a list of cuda devices, fall back to ffmpeg check

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
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return new QsvOutput(0, string.Empty);
        }

        var option = new QsvHardwareAccelerationOption(qsvDevice, FFmpegCapability.Software);
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

    public async Task<Option<string>> GetVaapiOutput(string display, Option<string> vaapiDriver, string vaapiDevice)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return Option<string>.None;
        }

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

        var lines = new List<string>();

        string arguments = display == "drm"
            ? $"--display drm --device {vaapiDevice} -a"
            : $"--display {display} -a";

        await Cli.Wrap("vainfo")
            .WithArguments(arguments)
            .WithEnvironmentVariables(envVars)
            .WithValidation(CommandResultValidation.None)
            .WithStandardOutputPipe(PipeTarget.ToDelegate(lines.Add))
            .WithStandardErrorPipe(PipeTarget.ToDelegate(lines.Add))
            .ExecuteAsync();

        return string.Join(System.Environment.NewLine, lines);
    }

    public async Task<List<string>> GetVaapiDisplays()
    {
        BufferedCommandResult whichResult = await Cli.Wrap("which")
            .WithArguments("vainfo")
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync(Encoding.UTF8);

        if (whichResult.ExitCode != 0)
        {
            return ["drm"];
        }

        BufferedCommandResult result = await Cli.Wrap("vainfo")
            .WithArguments("--display help")
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync(Encoding.UTF8);

        return string.IsNullOrWhiteSpace(result.StandardOutput)
            ? ["drm"]
            : result.StandardOutput.Trim().Split("\n").Skip(1).Map(s => s.Trim()).ToList();
    }

    public List<CpuModel> GetCpuList()
    {
        try
        {
            var hardwareInfo = new HardwareInfo();
            hardwareInfo.RefreshCPUList();
            return hardwareInfo.CpuList.Map(c => new CpuModel(c.Manufacturer, c.Name)).ToList();
        }
        catch (Exception)
        {
            // do nothing
        }

        return [];
    }

    public List<VideoControllerModel> GetVideoControllerList()
    {
        try
        {
            var hardwareInfo = new HardwareInfo();
            hardwareInfo.RefreshVideoControllerList();
            return hardwareInfo.VideoControllerList
                .Map(v => new VideoControllerModel(v.Manufacturer, v.Name))
                .ToList();
        }
        catch (Exception)
        {
            // do nothing
        }

        return [];
    }

    public List<string> GetVideoToolboxDecoders()
    {
        var result = new List<string>();

        foreach (string fourCC in FourCC.AllVideoToolbox)
        {
            if (VideoToolboxUtil.IsHardwareDecoderSupported(fourCC, _logger))
            {
                result.Add(fourCC);
            }
        }

        return result;
    }

    public List<string> GetVideoToolboxEncoders() => VideoToolboxUtil.GetAvailableEncoders(_logger);

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
        Option<string> vaapiDisplay,
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

            string display = vaapiDisplay.IfNone("drm");
            string driver = vaapiDriver.IfNone(string.Empty);
            string device = vaapiDevice.IfNone(string.Empty);
            var cacheKey = string.Format(CultureInfo.InvariantCulture, VaapiCacheKeyFormat, display, driver, device);

            if (_memoryCache.TryGetValue(cacheKey, out List<VaapiProfileEntrypoint>? profileEntrypoints) &&
                profileEntrypoints is not null)
            {
                return new VaapiHardwareCapabilities(profileEntrypoints, _logger);
            }

            Option<string> output = await GetVaapiOutput(display, vaapiDriver, device);
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
                if (display == "drm")
                {
                    _logger.LogDebug(
                        "Detected {Count} VAAPI profile entrypoints using {Driver} {Device}",
                        profileEntrypoints.Count,
                        driver,
                        device);
                }
                else
                {
                    _logger.LogDebug(
                        "Detected {Count} VAAPI profile entrypoints using {Display} {Driver}",
                        profileEntrypoints.Count,
                        display,
                        driver);
                }

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
                if (!_memoryCache.TryGetValue("ffmpeg.vaapi_displays", out List<string>? vaapiDisplays))
                {
                    vaapiDisplays = ["drm"];
                }

                vaapiDisplays ??= [];
                vaapiDisplays = vaapiDisplays.OrderBy(s => s).ToList();

                foreach (string vaapiDisplay in vaapiDisplays)
                {
                    Option<string> vaapiOutput = await GetVaapiOutput(vaapiDisplay, Option<string>.None, device);
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
                        _logger.LogDebug(
                            "Detected {Count} VAAPI profile entrypoints using QSV device {Device}",
                            profileEntrypoints.Count,
                            device);

                        _memoryCache.Set(cacheKey, profileEntrypoints);
                        return new VaapiHardwareCapabilities(profileEntrypoints, _logger);
                    }
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

    private IHardwareCapabilities GetNvidiaCapabilities(IFFmpegCapabilities ffmpegCapabilities)
    {
        if (_memoryCache.TryGetValue(CudaDeviceKey, out CudaDevice? cudaDevice) && cudaDevice is not null)
        {
            return new NvidiaHardwareCapabilities(cudaDevice, ffmpegCapabilities, _logger);
        }

        try
        {
            Option<List<CudaDevice>> maybeDevices = CudaHelper.GetDevices();
            foreach (CudaDevice firstDevice in maybeDevices.Map(list => list.HeadOrNone()))
            {
                _logger.LogDebug(
                    "Detected NVIDIA GPU model {Model} architecture SM {Major}.{Minor}",
                    firstDevice.Model,
                    firstDevice.Version.Major,
                    firstDevice.Version.Minor);

                _memoryCache.Set(CudaDeviceKey, firstDevice);
                return new NvidiaHardwareCapabilities(firstDevice, ffmpegCapabilities, _logger);
            }
        }
        catch (FileNotFoundException)
        {
            // do nothing
        }

        _logger.LogWarning(
            "Error detecting NVIDIA GPU capabilities; some hardware accelerated features will be unavailable");

        return new NoHardwareCapabilities();
    }
}
