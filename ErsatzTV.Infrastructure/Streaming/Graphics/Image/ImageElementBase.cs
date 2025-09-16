using System.Runtime.InteropServices;
using ErsatzTV.Core.Domain;
using ErsatzTV.FFmpeg.State;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Metadata;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SkiaSharp;
using Image = SixLabors.ImageSharp.Image;

namespace ErsatzTV.Infrastructure.Streaming.Graphics;

public abstract class ImageElementBase : GraphicsElement, IDisposable
{
    private readonly List<double> _frameDelays = [];
    private readonly List<SKBitmap> _scaledFrames = [];
    private double _animatedDurationSeconds;

    private Image _sourceImage;

    protected SKPointI Location { get; private set; }

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
        _sourceImage?.Dispose();
        _scaledFrames?.ForEach(f => f.Dispose());
    }

    protected async Task LoadImage(
        Resolution squarePixelFrameSize,
        Resolution frameSize,
        string image,
        WatermarkLocation location,
        bool scale,
        double? scaleWidthPercent,
        double? horizontalMarginPercent,
        double? verticalMarginPercent,
        bool placeWithinSourceContent,
        CancellationToken cancellationToken)
    {
        bool isRemoteUri = Uri.TryCreate(image, UriKind.Absolute, out Uri uriResult)
                           && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

        if (isRemoteUri)
        {
            using var client = new HttpClient();
            await using Stream imageStream = await client.GetStreamAsync(uriResult, cancellationToken);
            _sourceImage = await Image.LoadAsync(imageStream, cancellationToken);
        }
        else
        {
            _sourceImage = await Image.LoadAsync(image!, cancellationToken);
        }

        int scaledWidth = _sourceImage.Width;
        int scaledHeight = _sourceImage.Height;
        if (scale)
        {
            scaledWidth = (int)Math.Round((scaleWidthPercent ?? 100) / 100.0 * frameSize.Width);
            double aspectRatio = (double)_sourceImage.Height / _sourceImage.Width;
            scaledHeight = (int)(scaledWidth * aspectRatio);
        }

        (int horizontalMargin, int verticalMargin) = placeWithinSourceContent
            ? SourceContentMargins(
                squarePixelFrameSize,
                frameSize,
                horizontalMarginPercent ?? 0,
                verticalMarginPercent ?? 0)
            : NormalMargins(frameSize, horizontalMarginPercent ?? 0, verticalMarginPercent ?? 0);

        Location = CalculatePosition(
            location,
            frameSize.Width,
            frameSize.Height,
            scaledWidth,
            scaledHeight,
            horizontalMargin,
            verticalMargin);

        _animatedDurationSeconds = 0;

        for (var i = 0; i < _sourceImage.Frames.Count; i++)
        {
            Image frame = _sourceImage.Frames.CloneFrame(i);
            frame.Mutate(ctx => ctx.Resize(scaledWidth, scaledHeight));
            _scaledFrames.Add(ToSkiaBitmap(frame));

            double frameDelay = GetFrameDelaySeconds(_sourceImage, i);
            _animatedDurationSeconds += frameDelay;
            _frameDelays.Add(frameDelay);
        }
    }

    protected static SKBitmap ToSkiaBitmap(Image image)
    {
        using Image<Rgba32> rgbaImage = image.CloneAs<Rgba32>();

        int width = rgbaImage.Width;
        int height = rgbaImage.Height;

        var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        var skBitmap = new SKBitmap(info);
        if (!skBitmap.TryAllocPixels(info))
        {
            skBitmap.Dispose();
            throw new InvalidOperationException("Failed to allocate pixels for SKBitmap.");
        }

        var pixelArray = new Rgba32[width * height];
        rgbaImage.CopyPixelDataTo(pixelArray);

        var bytes = new byte[pixelArray.Length * 4];
        MemoryMarshal.AsBytes(pixelArray.AsSpan()).CopyTo(bytes);

        IntPtr dstPtr = skBitmap.GetPixels(out _);
        Marshal.Copy(bytes, 0, dstPtr, bytes.Length);

        return skBitmap;
    }

    protected static double GetFrameDelaySeconds(Image image, int frameIndex)
    {
        IImageFormat format = image.Metadata.DecodedImageFormat;
        ImageFrameMetadata frameMeta = image.Frames[frameIndex].Metadata;

        if (format == GifFormat.Instance)
        {
            // GIF frame delay is in hundredths of a second
            GifFrameMetadata gifMeta = frameMeta.GetFormatMetadata(GifFormat.Instance);
            return gifMeta.FrameDelay / 100.0;
        }

        if (format == PngFormat.Instance)
        {
            // PNG animated frame delay is in seconds (as double)
            PngFrameMetadata pngMeta = frameMeta.GetFormatMetadata(PngFormat.Instance);
            return pngMeta.FrameDelay.ToDouble();
        }

        if (format == WebpFormat.Instance)
        {
            // WEBP animated frame delay is in milliseconds
            WebpFrameMetadata webpMeta = frameMeta.GetFormatMetadata(WebpFormat.Instance);
            return webpMeta.FrameDelay / 1000.0;
        }

        // Default: assume 1/60th second (~16.67 ms) if unknown
        return 1.0 / 60.0;
    }

    protected SKBitmap GetFrameForTimestamp(TimeSpan timestamp)
    {
        if (_scaledFrames.Count <= 1)
        {
            return _scaledFrames[0];
        }

        double currentTime = timestamp.TotalSeconds % _animatedDurationSeconds;

        double frameTime = 0;
        for (var i = 0; i < _sourceImage.Frames.Count; i++)
        {
            frameTime += _frameDelays[i];
            if (currentTime <= frameTime)
            {
                return _scaledFrames[i];
            }
        }

        return _scaledFrames.Last();
    }
}
