﻿using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Jellyfin;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Jellyfin
{
    public interface IJellyfinApiClient
    {
        Task<Either<BaseError, JellyfinServerInformation>> GetServerInformation(string address, string apiKey);
        Task<Either<BaseError, List<JellyfinLibrary>>> GetLibraries(string address, string apiKey);
        Task<Either<BaseError, string>> GetAdminUserId(string address, string apiKey);

        Task<Either<BaseError, List<JellyfinMovie>>> GetMovieLibraryItems(
            string address,
            string apiKey,
            int mediaSourceId,
            string libraryId);

        Task<Either<BaseError, List<JellyfinShow>>> GetShowLibraryItems(
            string address,
            string apiKey,
            int mediaSourceId,
            string libraryId);

        Task<Either<BaseError, List<JellyfinSeason>>> GetSeasonLibraryItems(
            string address,
            string apiKey,
            int mediaSourceId,
            string showId);

        Task<Either<BaseError, List<JellyfinEpisode>>> GetEpisodeLibraryItems(
            string address,
            string apiKey,
            int mediaSourceId,
            string seasonId);
    }
}
