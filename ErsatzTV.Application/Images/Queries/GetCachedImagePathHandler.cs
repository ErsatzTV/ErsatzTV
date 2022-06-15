using CliWrap;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Images;
using ErsatzTV.Core.Interfaces.Repositories;
using Winista.Mime;

namespace ErsatzTV.Application.Images;

public class
    GetCachedImagePathHandler : IRequestHandler<GetCachedImagePath, Either<BaseError, CachedImagePathViewModel>>
{
    private static readonly MimeTypes MimeTypes = new();
    private readonly IConfigElementRepository _configElementRepository;
    private readonly IFFmpegProcessService _ffmpegProcessService;
    private readonly IImageCache _imageCache;

    public GetCachedImagePathHandler(
        IImageCache imageCache,
        IFFmpegProcessService ffmpegProcessService,
        IConfigElementRepository configElementRepository)
    {
        _imageCache = imageCache;
        _ffmpegProcessService = ffmpegProcessService;
        _configElementRepository = configElementRepository;
    }

    public async Task<Either<BaseError, CachedImagePathViewModel>> Handle(
        GetCachedImagePath request,
        CancellationToken cancellationToken)
    {
        Validation<BaseError, string> validation = await Validate();
        return await validation.Match(
            ffmpegPath => Handle(ffmpegPath, request),
            error => Task.FromResult<Either<BaseError, CachedImagePathViewModel>>(error.Join()));
    }

    private async Task<Either<BaseError, CachedImagePathViewModel>> Handle(
        string ffmpegPath,
        GetCachedImagePath request)
    {
        try
        {
            MimeType mimeType;

            string cachePath = _imageCache.GetPathForImage(
                request.FileName,
                request.ArtworkKind,
                Optional(request.MaxHeight));

            if (cachePath == null)
            {
                return BaseError.New("Failed to generate cache path for image");
            }

            if (!File.Exists(cachePath))
            {
                if (request.MaxHeight.HasValue)
                {
                    string baseFolder = Path.GetDirectoryName(cachePath);
                    if (baseFolder != null && !Directory.Exists(baseFolder))
                    {
                        Directory.CreateDirectory(baseFolder);
                    }

                    // ffmpeg needs the extension to determine the output codec
                    string withExtension = cachePath + ".jpg";

                    string originalPath = _imageCache.GetPathForImage(request.FileName, request.ArtworkKind, None);

                    Command process = await _ffmpegProcessService.ResizeImage(
                        ffmpegPath,
                        originalPath,
                        withExtension,
                        request.MaxHeight.Value);

                    CommandResult resize = await process.ExecuteAsync();

                    if (resize.ExitCode != 0)
                    {
                        return BaseError.New($"Failed to resize image; exit code {resize.ExitCode}");
                    }

                    File.Move(withExtension, cachePath);

                    mimeType = new MimeType("image/jpeg");
                }
                else
                {
                    return BaseError.New($"Artwork does not exist on disk at {cachePath}");
                }
            }
            else
            {
                mimeType = MimeTypes.GetMimeTypeFromFile(cachePath);
            }

            return new CachedImagePathViewModel(cachePath, mimeType.Name);
        }
        catch (Exception ex)
        {
            return BaseError.New(ex.Message);
        }
    }

    private async Task<Validation<BaseError, string>> Validate() =>
        await ValidateFFmpegPath();

    private Task<Validation<BaseError, string>> ValidateFFmpegPath() =>
        _configElementRepository.GetValue<string>(ConfigElementKey.FFmpegPath)
            .FilterT(File.Exists)
            .Map(ffmpegPath => ffmpegPath.ToValidation<BaseError>("FFmpeg path does not exist on the file system"));
}
