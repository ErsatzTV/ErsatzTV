﻿using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Plex;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Plex
{
    public interface IPlexServerApiClient
    {
        Task<Either<BaseError, List<PlexLibrary>>> GetLibraries(
            PlexConnection connection,
            PlexServerAuthToken token);

        Task<Either<BaseError, List<PlexMovie>>> GetLibraryContents(
            PlexLibrary library,
            PlexConnection connection,
            PlexServerAuthToken token);
    }
}
