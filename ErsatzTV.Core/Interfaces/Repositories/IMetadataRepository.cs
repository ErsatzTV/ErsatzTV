﻿using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Repositories;

public interface IMetadataRepository
{
    Task<bool> RemoveGenre(Genre genre);
    Task<bool> RemoveTag(Tag tag);
    Task<bool> RemoveStudio(Studio studio);
    Task<bool> RemoveStyle(Style style);
    Task<bool> RemoveMood(Mood mood);
    Task<bool> RemoveActor(Actor actor);
    Task<bool> Update(Domain.Metadata metadata);
    Task<bool> Add(Domain.Metadata metadata);
    Task<bool> UpdateStatistics(MediaItem mediaItem, MediaVersion incoming, bool updateVersion = true);
    Task<Unit> UpdateArtworkPath(Artwork artwork);
    Task<Unit> AddArtwork(Domain.Metadata metadata, Artwork artwork);
    Task<Unit> RemoveArtwork(Domain.Metadata metadata, ArtworkKind artworkKind);

    Task<bool> CloneArtwork(
        Domain.Metadata metadata,
        Option<Artwork> maybeArtwork,
        ArtworkKind artworkKind,
        string sourcePath,
        DateTime lastWriteTime);

    Task<Unit> MarkAsUpdated(ShowMetadata metadata, DateTime dateUpdated);
    Task<Unit> MarkAsUpdated(SeasonMetadata metadata, DateTime dateUpdated);
    Task<Unit> MarkAsUpdated(MovieMetadata metadata, DateTime dateUpdated);
    Task<Unit> MarkAsUpdated(EpisodeMetadata metadata, DateTime dateUpdated);
    Task<Unit> MarkAsExternal(ShowMetadata metadata);
    Task<Unit> SetContentRating(ShowMetadata metadata, string contentRating);
    Task<Unit> MarkAsExternal(MovieMetadata metadata);
    Task<Unit> SetContentRating(MovieMetadata metadata, string contentRating);
    Task<bool> RemoveGuid(MetadataGuid guid);
    Task<bool> AddGuid(Domain.Metadata metadata, MetadataGuid guid);
    Task<bool> RemoveDirector(Director director);
    Task<bool> RemoveWriter(Writer writer);
    Task<bool> UpdateSubtitles(Domain.Metadata metadata, List<Subtitle> subtitles);
}
