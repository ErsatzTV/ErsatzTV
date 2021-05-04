﻿using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Infrastructure.Jellyfin.Models;
using Refit;

namespace ErsatzTV.Infrastructure.Jellyfin
{
    [Headers("Accept: application/json")]
    public interface IJellyfinApi
    {
        [Get("/System/Configuration")]
        public Task<JellyfinConfigurationResponse> GetConfiguration(
            [Header("X-Emby-Token")]
            string apiKey);

        [Get("/Users")]
        public Task<List<JellyfinUserResponse>> GetUsers(
            [Header("X-Emby-Token")]
            string apiKey);

        [Get("/Library/VirtualFolders")]
        public Task<List<JellyfinLibraryResponse>> GetLibraries(
            [Header("X-Emby-Token")]
            string apiKey);

        [Get("/Items")]
        public Task<JellyfinLibraryItemsResponse> GetLibraryItems(
            [Header("X-Emby-Token")]
            string apiKey,
            [Query]
            string userId,
            [Query]
            string parentId,
            [Query]
            string fields = "Path,MediaStreams,Genres,Tags",
            [Query]
            string includeItemTypes = "movie,tvshow");
    }
}
