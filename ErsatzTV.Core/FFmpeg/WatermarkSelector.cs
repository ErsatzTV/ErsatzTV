using System.Text;
using CliWrap;
using CliWrap.Buffered;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Images;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Images;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.FFmpeg;

public class WatermarkSelector(IImageCache imageCache, IMemoryCache memoryCache, ILogger<WatermarkSelector> logger)
    : IWatermarkSelector
{
    public async Task<WatermarkOptions> GetWatermarkOptions(
        string ffprobePath,
        Channel channel,
        Option<ChannelWatermark> playoutItemWatermark,
        Option<ChannelWatermark> globalWatermark,
        MediaVersion videoVersion,
        Option<ChannelWatermark> watermarkOverride,
        Option<string> watermarkPath)
    {
        if (channel.StreamingMode == StreamingMode.HttpLiveStreamingDirect)
        {
            return WatermarkOptions.NoWatermark;
        }

        if (videoVersion is CoverArtMediaVersion)
        {
            return new WatermarkOptions(
                watermarkOverride,
                await watermarkPath.IfNoneAsync(videoVersion.MediaFiles.Head().Path),
                0,
                false);
        }

        // check for playout item watermark
        foreach (ChannelWatermark watermark in playoutItemWatermark)
        {
            switch (watermark.ImageSource)
            {
                // used for song progress overlay
                case ChannelWatermarkImageSource.Resource:
                    return new WatermarkOptions(
                        await watermarkOverride.IfNoneAsync(watermark),
                        Path.Combine(FileSystemLayout.ResourcesCacheFolder, watermark.Image),
                        Option<int>.None,
                        false);
                case ChannelWatermarkImageSource.Custom:
                    // bad form validation makes this possible
                    if (string.IsNullOrWhiteSpace(watermark.Image))
                    {
                        logger.LogWarning(
                            "Watermark {Name} has custom image configured with no image; ignoring",
                            watermark.Name);
                        break;
                    }

                    logger.LogDebug("Watermark will come from playout item (custom)");

                    string customPath = imageCache.GetPathForImage(
                        watermark.Image,
                        ArtworkKind.Watermark,
                        Option<int>.None);
                    return new WatermarkOptions(
                        await watermarkOverride.IfNoneAsync(watermark),
                        customPath,
                        None,
                        await IsAnimated(ffprobePath, customPath));
                case ChannelWatermarkImageSource.ChannelLogo:
                    logger.LogDebug("Watermark will come from playout item (channel logo)");

                    Option<string> maybeChannelPath = channel.Artwork.Count == 0
                        ?
                        //We have to generate the logo on the fly and save it to a local temp path
                        ChannelLogoGenerator.GenerateChannelLogoUrl(channel)
                        :
                        //We have an artwork attached to the channel, let's use it :)
                        channel.Artwork
                            .Filter(a => a.ArtworkKind == ArtworkKind.Logo)
                            .HeadOrNone()
                            .Map(a => Artwork.IsExternalUrl(a.Path)
                                ? a.Path
                                : imageCache.GetPathForImage(a.Path, ArtworkKind.Logo, Option<int>.None));

                    return new WatermarkOptions(
                        await watermarkOverride.IfNoneAsync(watermark),
                        maybeChannelPath,
                        None,
                        await maybeChannelPath.Match(
                            p => IsAnimated(ffprobePath, p),
                            () => Task.FromResult(false)));
                default:
                    throw new NotSupportedException("Unsupported watermark image source");
            }
        }

        // check for channel watermark
        if (channel.Watermark != null)
        {
            switch (channel.Watermark.ImageSource)
            {
                case ChannelWatermarkImageSource.Custom:
                    logger.LogDebug("Watermark will come from channel (custom)");

                    string customPath = imageCache.GetPathForImage(
                        channel.Watermark.Image,
                        ArtworkKind.Watermark,
                        Option<int>.None);
                    return new WatermarkOptions(
                        await watermarkOverride.IfNoneAsync(channel.Watermark),
                        customPath,
                        None,
                        await IsAnimated(ffprobePath, customPath));
                case ChannelWatermarkImageSource.ChannelLogo:
                    logger.LogDebug("Watermark will come from channel (channel logo)");

                    Option<string> maybeChannelPath = channel.Artwork.Count == 0
                        ?
                        //We have to generate the logo on the fly and save it to a local temp path
                        ChannelLogoGenerator.GenerateChannelLogoUrl(channel)
                        :
                        //We have an artwork attached to the channel, let's use it :)
                        channel.Artwork
                            .Filter(a => a.ArtworkKind == ArtworkKind.Logo)
                            .HeadOrNone()
                            .Map(a => Artwork.IsExternalUrl(a.Path)
                                ? a.Path
                                : imageCache.GetPathForImage(a.Path, ArtworkKind.Logo, Option<int>.None));
                    return new WatermarkOptions(
                        await watermarkOverride.IfNoneAsync(channel.Watermark),
                        maybeChannelPath,
                        None,
                        await maybeChannelPath.Match(
                            p => IsAnimated(ffprobePath, p),
                            () => Task.FromResult(false)));
                default:
                    throw new NotSupportedException("Unsupported watermark image source");
            }
        }

        // check for global watermark
        foreach (ChannelWatermark watermark in globalWatermark)
        {
            switch (watermark.ImageSource)
            {
                case ChannelWatermarkImageSource.Custom:
                    logger.LogDebug("Watermark will come from global (custom)");

                    string customPath = imageCache.GetPathForImage(
                        watermark.Image,
                        ArtworkKind.Watermark,
                        Option<int>.None);
                    return new WatermarkOptions(
                        await watermarkOverride.IfNoneAsync(watermark),
                        customPath,
                        None,
                        await IsAnimated(ffprobePath, customPath));
                case ChannelWatermarkImageSource.ChannelLogo:
                    logger.LogDebug("Watermark will come from global (channel logo)");

                    Option<string> maybeChannelPath = channel.Artwork.Count == 0
                        ?
                        //We have to generate the logo on the fly and save it to a local temp path
                        ChannelLogoGenerator.GenerateChannelLogoUrl(channel)
                        :
                        //We have an artwork attached to the channel, let's use it :)
                        channel.Artwork
                            .Filter(a => a.ArtworkKind == ArtworkKind.Logo)
                            .HeadOrNone()
                            .Map(a => Artwork.IsExternalUrl(a.Path)
                                ? a.Path
                                : imageCache.GetPathForImage(a.Path, ArtworkKind.Logo, Option<int>.None));
                    return new WatermarkOptions(
                        await watermarkOverride.IfNoneAsync(watermark),
                        maybeChannelPath,
                        None,
                        await maybeChannelPath.Match(
                            p => IsAnimated(ffprobePath, p),
                            () => Task.FromResult(false)));
                default:
                    throw new NotSupportedException("Unsupported watermark image source");
            }
        }

        return WatermarkOptions.NoWatermark;
    }

    private async Task<bool> IsAnimated(string ffprobePath, string path)
    {
        try
        {
            var cacheKey = $"image.animated.{Path.GetFileName(path)}";
            if (memoryCache.TryGetValue(cacheKey, out bool animated))
            {
                return animated;
            }

            BufferedCommandResult result = await Cli.Wrap(ffprobePath)
                .WithArguments(
                [
                    "-loglevel", "error",
                    "-select_streams", "v:0",
                    "-count_frames",
                    "-show_entries", "stream=nb_read_frames",
                    "-print_format", "csv",
                    path
                ])
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync(Encoding.UTF8);

            if (result.ExitCode == 0)
            {
                string output = result.StandardOutput;
                output = output.Replace("stream,", string.Empty);
                if (int.TryParse(output, out int frameCount))
                {
                    bool isAnimated = frameCount > 1;
                    memoryCache.Set(cacheKey, isAnimated, TimeSpan.FromDays(1));
                    return isAnimated;
                }
            }
            else
            {
                logger.LogWarning(
                    "Error checking frame count for file {File}l exit code {ExitCode}",
                    path,
                    result.ExitCode);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error checking frame count for file {File}", path);
        }

        return false;
    }
}
