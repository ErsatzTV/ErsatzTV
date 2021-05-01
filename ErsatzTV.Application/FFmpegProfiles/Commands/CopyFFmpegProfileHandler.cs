﻿using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.FFmpegProfiles.Mapper;

namespace ErsatzTV.Application.FFmpegProfiles.Commands
{
    public class
        CopyFFmpegProfileHandler : IRequestHandler<CopyFFmpegProfile, Either<BaseError, FFmpegProfileViewModel>>
    {
        private readonly IFFmpegProfileRepository _ffmpegProfileRepository;

        public CopyFFmpegProfileHandler(IFFmpegProfileRepository ffmpegProfileRepository) =>
            _ffmpegProfileRepository = ffmpegProfileRepository;

        public Task<Either<BaseError, FFmpegProfileViewModel>> Handle(
            CopyFFmpegProfile request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(PerformCopy)
                .Bind(v => v.ToEitherAsync());

        private Task<FFmpegProfileViewModel> PerformCopy(CopyFFmpegProfile request) =>
            _ffmpegProfileRepository.Copy(request.FFmpegProfileId, request.Name)
                .Map(ProjectToViewModel);

        private Task<Validation<BaseError, CopyFFmpegProfile>> Validate(CopyFFmpegProfile request) =>
            ValidateName(request).AsTask().MapT(_ => request);

        private Validation<BaseError, string> ValidateName(CopyFFmpegProfile request) =>
            request.NotEmpty(x => x.Name)
                .Bind(_ => request.NotLongerThan(50)(x => x.Name));
    }
}
