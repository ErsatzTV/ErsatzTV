using System.Text;
using System.Text.RegularExpressions;
using CliWrap;
using CliWrap.Buffered;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.FFmpeg.Capabilities;

public class HardwareCapabilitiesFactory : IHardwareCapabilitiesFactory
{
    private const string CacheKey = "ffmpeg.hardware.nvidia.architecture";
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
            _ => new DefaultHardwareCapabilities()
        };

    private async Task<IHardwareCapabilities> GetNvidiaCapabilities(string ffmpegPath)
    {
        if (_memoryCache.TryGetValue(CacheKey, out int cachedArchitecture))
        {
            return new NvidiaHardwareCapabilities(cachedArchitecture);
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
            const string PATTERN = @"SM\s+(\d\.\d)";
            Match match = Regex.Match(line, PATTERN);
            if (match.Success && int.TryParse(match.Groups[1].Value.Replace(".", string.Empty), out int architecture))
            {
                _logger.LogInformation("Detected NVIDIA GPU architecture SM {Architecture}", architecture);
                _memoryCache.Set(CacheKey, architecture);
                return new NvidiaHardwareCapabilities(architecture);
            }
        }

        _logger.LogWarning(
            "Error detecting NVIDIA GPU capabilities; some hardware accelerated features will be unavailable: {ExitCode}",
            result.ExitCode);

        return new NoHardwareCapabilities();
    }
}
