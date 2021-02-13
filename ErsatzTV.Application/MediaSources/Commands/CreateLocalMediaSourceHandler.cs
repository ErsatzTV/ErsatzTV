using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.MediaSources.Mapper;
using static LanguageExt.Prelude;

namespace ErsatzTV.Application.MediaSources.Commands
{
    public class CreateLocalMediaSourceHandler : IRequestHandler<CreateLocalMediaSource,
        Either<BaseError, MediaSourceViewModel>>
    {
        private readonly IMediaSourceRepository _mediaSourceRepository;

        public CreateLocalMediaSourceHandler(IMediaSourceRepository mediaSourceRepository) =>
            _mediaSourceRepository = mediaSourceRepository;

        public Task<Either<BaseError, MediaSourceViewModel>> Handle(
            CreateLocalMediaSource request,
            CancellationToken cancellationToken) =>
            Validate(request).MapT(PersistLocalMediaSource).Bind(v => v.ToEitherAsync());

        private Task<MediaSourceViewModel> PersistLocalMediaSource(LocalMediaSource c) =>
            _mediaSourceRepository.Add(c).Map(ProjectToViewModel);

        private async Task<Validation<BaseError, LocalMediaSource>> Validate(CreateLocalMediaSource request) =>
            (await ValidateName(request), await ValidateFolder(request))
            .Apply(
                (name, folder) =>
                    new LocalMediaSource
                    {
                        Name = name,
                        MediaType = request.MediaType,
                        Folder = folder
                    });

        private async Task<Validation<BaseError, string>> ValidateName(CreateLocalMediaSource request)
        {
            List<string> allNames = await _mediaSourceRepository.GetAll()
                .Map(list => list.Map(c => c.Name).ToList());

            Validation<BaseError, string> result1 = request.NotEmpty(c => c.Name)
                .Bind(_ => request.NotLongerThan(50)(c => c.Name));

            var result2 = Optional(request.Name)
                .Filter(name => !allNames.Contains(name))
                .ToValidation<BaseError>("Media source name must be unique");

            return (result1, result2).Apply((_, _) => request.Name);
        }

        private async Task<Validation<BaseError, string>> ValidateFolder(CreateLocalMediaSource request)
        {
            List<string> allFolders = await _mediaSourceRepository.GetAll()
                .Map(list => list.OfType<LocalMediaSource>().Map(c => c.Folder).ToList());


            return Optional(request.Folder)
                .Filter(folder => allFolders.ForAll(f => !AreSubPaths(f, folder)))
                .ToValidation<BaseError>("Folder must not belong to another media source");
        }

        private static bool AreSubPaths(string path1, string path2)
        {
            string one = path1 + Path.DirectorySeparatorChar;
            string two = path2 + Path.DirectorySeparatorChar;
            return one == two || one.StartsWith(two, StringComparison.OrdinalIgnoreCase) ||
                   two.StartsWith(one, StringComparison.OrdinalIgnoreCase);
        }
    }
}
