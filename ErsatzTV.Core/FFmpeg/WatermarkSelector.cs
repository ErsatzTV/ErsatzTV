using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Core.Images;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Images;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.FFmpeg;

public class WatermarkSelector(IImageCache imageCache, IDecoSelector decoSelector, ILogger<WatermarkSelector> logger)
    : IWatermarkSelector
{
    public List<WatermarkOptions> SelectWatermarks(
        Option<ChannelWatermark> globalWatermark,
        Channel channel,
        PlayoutItem playoutItem,
        DateTimeOffset now)
    {
        logger.LogDebug("Checking for watermark at {Now}", now);

        var result = new List<WatermarkOptions>();

        if (channel.StreamingMode == StreamingMode.HttpLiveStreamingDirect)
        {
            return result;
        }

        if (playoutItem.DisableWatermarks)
        {
            logger.LogDebug("Watermark is disabled by playout item");
            return result;
        }

        DecoEntries decoEntries = decoSelector.GetDecoEntries(playoutItem.Playout, now);

        // first, check deco template / active deco
        foreach (Deco templateDeco in decoEntries.TemplateDeco)
        {
            var done = false;

            switch (templateDeco.WatermarkMode)
            {
                case DecoMode.Merge:
                    if (playoutItem.FillerKind is FillerKind.None || templateDeco.UseWatermarkDuringFiller)
                    {
                        logger.LogDebug("Watermark will come from template deco (merge)");
                        result.AddRange(
                            OptionsForWatermarks(channel, templateDeco.DecoWatermarks.Map(dwm => dwm.Watermark)));
                        break;
                    }

                    logger.LogDebug("Watermark is disabled by template deco during filler");
                    result.Clear();
                    done = true;
                    break;
                case DecoMode.Override:
                    if (playoutItem.FillerKind is FillerKind.None || templateDeco.UseWatermarkDuringFiller)
                    {
                        logger.LogDebug("Watermark will come from template deco (replace)");
                        result.AddRange(
                            OptionsForWatermarks(channel, templateDeco.DecoWatermarks.Map(dwm => dwm.Watermark)));
                        done = true;
                        break;
                    }

                    logger.LogDebug("Watermark is disabled by template deco during filler");
                    result.Clear();
                    done = true;
                    break;
                case DecoMode.Disable:
                    logger.LogDebug("Watermark is disabled by template deco");
                    done = true;
                    break;
                case DecoMode.Inherit:
                    logger.LogDebug("Watermark will inherit from playout deco");
                    break;
            }

            if (done)
            {
                return result;
            }
        }

        // second, check playout deco
        foreach (Deco playoutDeco in decoEntries.PlayoutDeco)
        {
            var done = false;

            switch (playoutDeco.WatermarkMode)
            {
                case DecoMode.Merge:
                    if (playoutItem.FillerKind is FillerKind.None || playoutDeco.UseWatermarkDuringFiller)
                    {
                        logger.LogDebug("Watermark will come from playout deco (merge)");
                        result.AddRange(
                            OptionsForWatermarks(channel, playoutDeco.DecoWatermarks.Map(dwm => dwm.Watermark)));
                        break;
                    }

                    logger.LogDebug("Watermark is disabled by playout deco during filler");
                    result.Clear();
                    done = true;
                    break;
                case DecoMode.Override:
                    if (playoutItem.FillerKind is FillerKind.None || playoutDeco.UseWatermarkDuringFiller)
                    {
                        logger.LogDebug("Watermark will come from playout deco (replace)");
                        result.AddRange(
                            OptionsForWatermarks(channel, playoutDeco.DecoWatermarks.Map(dwm => dwm.Watermark)));
                        done = true;
                        break;
                    }

                    logger.LogDebug("Watermark is disabled by playout deco during filler");
                    result.Clear();
                    done = true;
                    break;
                case DecoMode.Disable:
                    logger.LogDebug("Watermark is disabled by playout deco");
                    done = true;
                    break;
                case DecoMode.Inherit:
                    logger.LogDebug("Watermark will inherit from channel and/or global setting");
                    break;
            }

            if (done)
            {
                return result;
            }
        }

        if (playoutItem.Watermarks.Count > 0)
        {
            foreach (var watermark in playoutItem.Watermarks)
            {
                Option<WatermarkOptions> options = GetWatermarkOptions(
                    channel,
                    watermark,
                    Option<ChannelWatermark>.None);
                result.AddRange(options);
            }

            return result;
        }


        var finalOptions = GetWatermarkOptions(channel, Option<ChannelWatermark>.None, globalWatermark);
        result.AddRange(finalOptions);

        return result;
    }

    public Option<WatermarkOptions> GetWatermarkOptions(
        Channel channel,
        Option<ChannelWatermark> playoutItemWatermark,
        Option<ChannelWatermark> globalWatermark)
    {
        if (channel.StreamingMode == StreamingMode.HttpLiveStreamingDirect)
        {
            return Option<WatermarkOptions>.None;
        }

        // check for playout item watermark
        foreach (ChannelWatermark watermark in playoutItemWatermark)
        {
            switch (watermark.ImageSource)
            {
                // used for song progress overlay
                case ChannelWatermarkImageSource.Resource:
                    return new WatermarkOptions(
                        watermark,
                        Path.Combine(FileSystemLayout.ResourcesCacheFolder, watermark.Image),
                        Option<int>.None);
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
                        watermark,
                        customPath,
                        None);
                case ChannelWatermarkImageSource.ChannelLogo:
                    logger.LogDebug("Watermark will come from playout item (channel logo)");

                    string channelPath = ChannelLogoGenerator.GenerateChannelLogoUrl(channel);
                    Option<Artwork> maybeLogoArtwork =
                        Optional(channel.Artwork.Find(a => a.ArtworkKind == ArtworkKind.Logo));
                    foreach (var logoArtwork in maybeLogoArtwork)
                    {
                        channelPath = Artwork.IsExternalUrl(logoArtwork.Path)
                            ? logoArtwork.Path
                            : imageCache.GetPathForImage(logoArtwork.Path, ArtworkKind.Logo, Option<int>.None);
                    }

                    return new WatermarkOptions(watermark, channelPath, None);
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
                        channel.Watermark,
                        customPath,
                        None);
                case ChannelWatermarkImageSource.ChannelLogo:
                    logger.LogDebug("Watermark will come from channel (channel logo)");

                    string channelPath = ChannelLogoGenerator.GenerateChannelLogoUrl(channel);
                    Option<Artwork> maybeLogoArtwork =
                        Optional(channel.Artwork.Find(a => a.ArtworkKind == ArtworkKind.Logo));
                    foreach (var logoArtwork in maybeLogoArtwork)
                    {
                        channelPath = Artwork.IsExternalUrl(logoArtwork.Path)
                            ? logoArtwork.Path
                            : imageCache.GetPathForImage(logoArtwork.Path, ArtworkKind.Logo, Option<int>.None);
                    }

                    return new WatermarkOptions(channel.Watermark, channelPath, None);
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
                        watermark,
                        customPath,
                        None);
                case ChannelWatermarkImageSource.ChannelLogo:
                    logger.LogDebug("Watermark will come from global (channel logo)");

                    string channelPath = ChannelLogoGenerator.GenerateChannelLogoUrl(channel);
                    Option<Artwork> maybeLogoArtwork =
                        Optional(channel.Artwork.Find(a => a.ArtworkKind == ArtworkKind.Logo));
                    foreach (var logoArtwork in maybeLogoArtwork)
                    {
                        channelPath = Artwork.IsExternalUrl(logoArtwork.Path)
                            ? logoArtwork.Path
                            : imageCache.GetPathForImage(logoArtwork.Path, ArtworkKind.Logo, Option<int>.None);
                    }

                    return new WatermarkOptions(watermark, channelPath, None);
                default:
                    throw new NotSupportedException("Unsupported watermark image source");
            }
        }

        return Option<WatermarkOptions>.None;
    }

    private List<WatermarkOptions> OptionsForWatermarks(Channel channel, IEnumerable<ChannelWatermark> watermarks)
    {
        var result = new List<WatermarkOptions>();

        foreach (var watermark in watermarks)
        {
            result.AddRange(GetWatermarkOptions(channel, watermark));
        }

        return result;
    }

    private Option<WatermarkOptions> GetWatermarkOptions(Channel channel, ChannelWatermark watermark)
    {
        switch (watermark.ImageSource)
        {
            // used for song progress overlay
            case ChannelWatermarkImageSource.Resource:
                return new WatermarkOptions(
                    watermark,
                    Path.Combine(FileSystemLayout.ResourcesCacheFolder, watermark.Image),
                    Option<int>.None);
            case ChannelWatermarkImageSource.Custom:
                // bad form validation makes this possible
                if (string.IsNullOrWhiteSpace(watermark.Image))
                {
                    logger.LogWarning(
                        "Watermark {Name} has custom image configured with no image; ignoring",
                        watermark.Name);
                    break;
                }

                string customPath = imageCache.GetPathForImage(
                    watermark.Image,
                    ArtworkKind.Watermark,
                    Option<int>.None);
                return new WatermarkOptions(
                    watermark,
                    customPath,
                    None);
            case ChannelWatermarkImageSource.ChannelLogo:
                string channelPath = ChannelLogoGenerator.GenerateChannelLogoUrl(channel);
                Option<Artwork> maybeLogoArtwork =
                    Optional(channel.Artwork.Find(a => a.ArtworkKind == ArtworkKind.Logo));
                foreach (var logoArtwork in maybeLogoArtwork)
                {
                    channelPath = Artwork.IsExternalUrl(logoArtwork.Path)
                        ? logoArtwork.Path
                        : imageCache.GetPathForImage(logoArtwork.Path, ArtworkKind.Logo, Option<int>.None);
                }

                return new WatermarkOptions(watermark, channelPath, None);
            default:
                throw new NotSupportedException("Unsupported watermark image source");
        }

        return Option<WatermarkOptions>.None;
    }
}
