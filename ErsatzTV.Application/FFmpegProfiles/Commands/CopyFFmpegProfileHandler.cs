using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using static ErsatzTV.Application.FFmpegProfiles.Mapper;

namespace ErsatzTV.Application.FFmpegProfiles;

public class
    CopyFFmpegProfileHandler : IRequestHandler<CopyFFmpegProfile, Either<BaseError, FFmpegProfileViewModel>>
{
    private readonly IFFmpegProfileRepository _ffmpegProfileRepository;
    private readonly ISearchTargets _searchTargets;

    public CopyFFmpegProfileHandler(IFFmpegProfileRepository ffmpegProfileRepository, ISearchTargets searchTargets)
    {
        _ffmpegProfileRepository = ffmpegProfileRepository;
        _searchTargets = searchTargets;
    }

    public Task<Either<BaseError, FFmpegProfileViewModel>> Handle(
        CopyFFmpegProfile request,
        CancellationToken cancellationToken) =>
        Validate(request)
            .MapT(PerformCopy)
            .Bind(v => v.ToEitherAsync());

    private async Task<FFmpegProfileViewModel> PerformCopy(CopyFFmpegProfile request)
    {
        FFmpegProfile copy = await _ffmpegProfileRepository.Copy(request.FFmpegProfileId, request.Name);
        _searchTargets.SearchTargetsChanged();
        return ProjectToViewModel(copy);
    }

    private static Task<Validation<BaseError, CopyFFmpegProfile>> Validate(CopyFFmpegProfile request) =>
        ValidateName(request).AsTask().MapT(_ => request);

    private static Validation<BaseError, string> ValidateName(CopyFFmpegProfile request) =>
        request.NotEmpty(x => x.Name)
            .Bind(_ => request.NotLongerThan(50)(x => x.Name));
}
