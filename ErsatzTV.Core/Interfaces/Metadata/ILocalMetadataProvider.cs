﻿using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Metadata
{
    public interface ILocalMetadataProvider
    {
        Task<ShowMetadata> GetMetadataForShow(string showFolder);
        Task<Option<MusicVideoMetadata>> GetMetadataForMusicVideo(string filePath);
        Task<bool> RefreshSidecarMetadata(MediaItem mediaItem, string path);
        Task<bool> RefreshSidecarMetadata(Show televisionShow, string showFolder);
        Task<bool> RefreshFallbackMetadata(MediaItem mediaItem);
        Task<bool> RefreshFallbackMetadata(Show televisionShow, string showFolder);
    }
}
