﻿using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.Jellyfin.Mapper;

namespace ErsatzTV.Application.Jellyfin.Queries
{
    public class
        GetJellyfinMediaSourceByIdHandler : IRequestHandler<GetJellyfinMediaSourceById,
            Option<JellyfinMediaSourceViewModel>>
    {
        private readonly IMediaSourceRepository _mediaSourceRepository;

        public GetJellyfinMediaSourceByIdHandler(IMediaSourceRepository mediaSourceRepository) =>
            _mediaSourceRepository = mediaSourceRepository;

        public Task<Option<JellyfinMediaSourceViewModel>> Handle(
            GetJellyfinMediaSourceById request,
            CancellationToken cancellationToken) =>
            _mediaSourceRepository.GetJellyfin(request.JellyfinMediaSourceId).MapT(ProjectToViewModel);
    }
}
