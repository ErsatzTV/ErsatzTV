using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Emby;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Emby
{
    public interface IEmbyApiClient
    {
        Task<Either<BaseError, EmbyServerInformation>> GetServerInformation(string address, string apiKey);
        Task<Either<BaseError, List<EmbyLibrary>>> GetLibraries(string address, string apiKey);
        // Task<Either<BaseError, string>> GetAdminUserId(string address, string apiKey);
        //
        Task<Either<BaseError, List<EmbyMovie>>> GetMovieLibraryItems(
            string address,
            string apiKey,
            int mediaSourceId,
            string libraryId);
        
        // Task<Either<BaseError, List<EmbyShow>>> GetShowLibraryItems(
        //     string address,
        //     string apiKey,
        //     int mediaSourceId,
        //     string libraryId);
        //
        // Task<Either<BaseError, List<EmbySeason>>> GetSeasonLibraryItems(
        //     string address,
        //     string apiKey,
        //     int mediaSourceId,
        //     string showId);
        //
        // Task<Either<BaseError, List<EmbyEpisode>>> GetEpisodeLibraryItems(
        //     string address,
        //     string apiKey,
        //     int mediaSourceId,
        //     string seasonId);
    }
}
