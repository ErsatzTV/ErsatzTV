﻿using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Configuration.Queries
{
    public class GetPlayoutDaysToBuildHandler : IRequestHandler<GetPlayoutDaysToBuild, int>
    {
        private readonly IConfigElementRepository _configElementRepository;

        public GetPlayoutDaysToBuildHandler(IConfigElementRepository configElementRepository) =>
            _configElementRepository = configElementRepository;

        public Task<int> Handle(GetPlayoutDaysToBuild request, CancellationToken cancellationToken) =>
            _configElementRepository.GetValue<int>(ConfigElementKey.PlayoutDaysToBuild)
                .Map(result => result.IfNone(2));
    }
}
