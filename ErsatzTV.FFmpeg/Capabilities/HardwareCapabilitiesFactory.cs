using System.Text;
using System.Text.RegularExpressions;
using CliWrap;
using CliWrap.Buffered;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.FFmpeg.Capabilities;

public class HardwareCapabilitiesFactory : IHardwareCapabilitiesFactory
{
    private const string ArchitectureCacheKey = "ffmpeg.hardware.nvidia.architecture";
    private const string ModelCacheKey = "ffmpeg.hardware.nvidia.model";
    private readonly ILogger<HardwareCapabilitiesFactory> _logger;

    private readonly IMemoryCache _memoryCache;

    public HardwareCapabilitiesFactory(IMemoryCache memoryCache, ILogger<HardwareCapabilitiesFactory> logger)
    {
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public async Task<IHardwareCapabilities> GetHardwareCapabilities(
        string ffmpegPath,
        HardwareAccelerationMode hardwareAccelerationMode) =>
        hardwareAccelerationMode switch
        {
            HardwareAccelerationMode.Nvenc => await GetNvidiaCapabilities(ffmpegPath),
            HardwareAccelerationMode.Amf => new AmfHardwareCapabilities(),
            _ => new DefaultHardwareCapabilities()
        };

    private async Task<IHardwareCapabilities> GetNvidiaCapabilities(string ffmpegPath)
    {
        if (_memoryCache.TryGetValue(ArchitectureCacheKey, out int cachedArchitecture)
            && _memoryCache.TryGetValue(ModelCacheKey, out string cachedModel))
        {
            return new NvidiaHardwareCapabilities(cachedArchitecture, cachedModel);
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
                return new NvidiaHardwareCapabilities(architecture, model);
            }
        }

        _logger.LogWarning(
            "Error detecting NVIDIA GPU capabilities; some hardware accelerated features will be unavailable: {ExitCode}",
            result.ExitCode);

        return new NoHardwareCapabilities();
    }
}
