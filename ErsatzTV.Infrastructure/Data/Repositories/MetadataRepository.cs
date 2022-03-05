using Dapper;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Infrastructure.Data.Repositories;

public class MetadataRepository : IMetadataRepository
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public MetadataRepository(IDbContextFactory<TvContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<bool> RemoveActor(Actor actor)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.ExecuteAsync(
                "DELETE FROM Actor WHERE Id = @ActorId",
                new { ActorId = actor.Id })
            .Map(result => result > 0);
    }

    public async Task<bool> Update(Metadata metadata)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        dbContext.Entry(metadata).State = EntityState.Modified;
        return await dbContext.SaveChangesAsync() > 0;
    }

    public async Task<bool> Add(Metadata metadata)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        dbContext.Entry(metadata).State = EntityState.Added;

        foreach (Genre genre in Optional(metadata.Genres).Flatten())
        {
            dbContext.Entry(genre).State = EntityState.Added;
        }

        foreach (Tag tag in Optional(metadata.Tags).Flatten())
        {
            dbContext.Entry(tag).State = EntityState.Added;
        }

        foreach (Studio studio in Optional(metadata.Studios).Flatten())
        {
            dbContext.Entry(studio).State = EntityState.Added;
        }

        if (metadata is ArtistMetadata artistMetadata)
        {
            foreach (Style style in Optional(artistMetadata.Styles).Flatten())
            {
                dbContext.Entry(style).State = EntityState.Added;
            }

            foreach (Mood mood in Optional(artistMetadata.Moods).Flatten())
            {
                dbContext.Entry(mood).State = EntityState.Added;
            }
        }

        foreach (Actor actor in Optional(metadata.Actors).Flatten())
        {
            dbContext.Entry(actor).State = EntityState.Added;
            if (actor.Artwork != null)
            {
                dbContext.Entry(actor.Artwork).State = EntityState.Added;
            }
        }

        foreach (MetadataGuid guid in Optional(metadata.Guids).Flatten())
        {
            dbContext.Entry(guid).State = EntityState.Added;
        }

        if (metadata is MovieMetadata movieMetadata)
        {
            foreach (Director director in Optional(movieMetadata.Directors).Flatten())
            {
                dbContext.Entry(director).State = EntityState.Added;
            }

            foreach (Writer writer in Optional(movieMetadata.Writers).Flatten())
            {
                dbContext.Entry(writer).State = EntityState.Added;
            }
        }

        if (metadata is EpisodeMetadata episodeMetadata)
        {
            foreach (Director director in Optional(episodeMetadata.Directors).Flatten())
            {
                dbContext.Entry(director).State = EntityState.Added;
            }

            foreach (Writer writer in Optional(episodeMetadata.Writers).Flatten())
            {
                dbContext.Entry(writer).State = EntityState.Added;
            }
        }

        return await dbContext.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdateLocalStatistics(
        MediaItem mediaItem,
        MediaVersion incoming,
        bool updateVersion = true)
    {
        int mediaVersionId = mediaItem.GetHeadVersion().Id;

        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        Option<MediaVersion> maybeVersion = await dbContext.MediaVersions
            .Include(v => v.Streams)
            .Include(v => v.Chapters)
            .Include(v => v.MediaFiles)
            .OrderBy(v => v.Id)
            .SingleOrDefaultAsync(v => v.Id == mediaVersionId)
            .Map(Optional);

        return await maybeVersion.Match(
            async existing =>
            {
                if (updateVersion)
                {
                    existing.DateUpdated = incoming.DateUpdated;
                    existing.Duration = incoming.Duration;
                    existing.SampleAspectRatio = incoming.SampleAspectRatio;
                    existing.DisplayAspectRatio = incoming.DisplayAspectRatio;
                    existing.Width = incoming.Width;
                    existing.Height = incoming.Height;
                    existing.VideoScanKind = incoming.VideoScanKind;
                    existing.RFrameRate = incoming.RFrameRate;
                }

                var toAdd = incoming.Streams.Filter(s => existing.Streams.All(es => es.Index != s.Index)).ToList();
                var toRemove = existing.Streams.Filter(es => incoming.Streams.All(s => s.Index != es.Index))
                    .ToList();
                var toUpdate = incoming.Streams.Except(toAdd).ToList();

                // add
                existing.Streams.AddRange(toAdd);

                // remove
                existing.Streams.RemoveAll(s => toRemove.Contains(s));

                // update
                foreach (MediaStream incomingStream in toUpdate)
                {
                    MediaStream existingStream = existing.Streams.First(s => s.Index == incomingStream.Index);

                    existingStream.Codec = incomingStream.Codec;
                    existingStream.Profile = incomingStream.Profile;
                    existingStream.MediaStreamKind = incomingStream.MediaStreamKind;
                    existingStream.Language = incomingStream.Language;
                    existingStream.Channels = incomingStream.Channels;
                    existingStream.Title = incomingStream.Title;
                    existingStream.Default = incomingStream.Default;
                    existingStream.Forced = incomingStream.Forced;
                    existingStream.AttachedPic = incomingStream.AttachedPic;
                    existingStream.PixelFormat = incomingStream.PixelFormat;
                    existingStream.BitsPerRawSample = incomingStream.BitsPerRawSample;
                }

                var chaptersToAdd = incoming.Chapters
                    .Filter(s => existing.Chapters.All(es => es.ChapterId != s.ChapterId))
                    .ToList();
                var chaptersToRemove = existing.Chapters
                    .Filter(es => incoming.Chapters.All(s => s.ChapterId != es.ChapterId))
                    .ToList();
                var chaptersToUpdate = incoming.Chapters.Except(chaptersToAdd).ToList();

                // add
                existing.Chapters.AddRange(chaptersToAdd);

                // remove
                existing.Chapters.RemoveAll(chaptersToRemove.Contains);

                // update
                foreach (MediaChapter incomingChapter in chaptersToUpdate)
                {
                    MediaChapter existingChapter = existing.Chapters
                        .First(s => s.ChapterId == incomingChapter.ChapterId);

                    existingChapter.StartTime = incomingChapter.StartTime;
                    existingChapter.EndTime = incomingChapter.EndTime;
                    existingChapter.Title = incomingChapter.Title;
                }

                if (await dbContext.SaveChangesAsync() <= 0)
                {
                    return false;
                }

                // reload the media versions so we can properly index the duration
                switch (mediaItem)
                {
                    case Movie movie:
                        movie.MediaVersions.Clear();
                        movie.MediaVersions.Add(existing);
                        break;
                    case Episode episode:
                        episode.MediaVersions.Clear();
                        episode.MediaVersions.Add(existing);
                        break;
                    case MusicVideo musicVideo:
                        musicVideo.MediaVersions.Clear();
                        musicVideo.MediaVersions.Add(existing);
                        break;
                    case OtherVideo otherVideo:
                        otherVideo.MediaVersions.Clear();
                        otherVideo.MediaVersions.Add(existing);
                        break;
                    case Song song:
                        song.MediaVersions.Clear();
                        song.MediaVersions.Add(existing);
                        break;
                }

                return true;
            },
            () => Task.FromResult(false));
    }

    public async Task<bool> UpdatePlexStatistics(int mediaVersionId, MediaVersion incoming)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.ExecuteAsync(
            @"UPDATE MediaVersion SET
                  DateUpdated = @DateUpdated
                  WHERE Id = @MediaVersionId",
            new
            {
                incoming.DateUpdated,
                MediaVersionId = mediaVersionId
            }).Map(result => result > 0);
    }

    public async Task<Unit> UpdateArtworkPath(Artwork artwork)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.ExecuteAsync(
            "UPDATE Artwork SET Path = @Path, SourcePath = @SourcePath, DateUpdated = @DateUpdated, BlurHash43 = @BlurHash43, BlurHash54 = @BlurHash54, BlurHash64 = @BlurHash64 WHERE Id = @Id",
            new
            {
                artwork.Path, artwork.SourcePath, artwork.DateUpdated, artwork.BlurHash43, artwork.BlurHash54,
                artwork.BlurHash64, artwork.Id
            }).ToUnit();
    }

    public async Task<Unit> AddArtwork(Metadata metadata, Artwork artwork)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        var parameters = new
        {
            artwork.ArtworkKind, metadata.Id, artwork.DateAdded, artwork.DateUpdated, artwork.Path,
            artwork.SourcePath, artwork.BlurHash43, artwork.BlurHash54, artwork.BlurHash64
        };

        return metadata switch
        {
            MovieMetadata => await dbContext.Connection.ExecuteAsync(
                    @"INSERT INTO Artwork (ArtworkKind, MovieMetadataId, DateAdded, DateUpdated, Path, SourcePath, BlurHash43, BlurHash54, BlurHash64)
                            VALUES (@ArtworkKind, @Id, @DateAdded, @DateUpdated, @Path, @SourcePath, @BlurHash43, @BlurHash54, @BlurHash64)",
                    parameters)
                .ToUnit(),
            ShowMetadata => await dbContext.Connection.ExecuteAsync(
                    @"INSERT INTO Artwork (ArtworkKind, ShowMetadataId, DateAdded, DateUpdated, Path, SourcePath, BlurHash43, BlurHash54, BlurHash64)
                            VALUES (@ArtworkKind, @Id, @DateAdded, @DateUpdated, @Path, @SourcePath, @BlurHash43, @BlurHash54, @BlurHash64)",
                    parameters)
                .ToUnit(),
            SeasonMetadata => await dbContext.Connection.ExecuteAsync(
                    @"INSERT INTO Artwork (ArtworkKind, SeasonMetadataId, DateAdded, DateUpdated, Path, SourcePath, BlurHash43, BlurHash54, BlurHash64)
                            VALUES (@ArtworkKind, @Id, @DateAdded, @DateUpdated, @Path, @SourcePath, @BlurHash43, @BlurHash54, @BlurHash64)",
                    parameters)
                .ToUnit(),
            EpisodeMetadata => await dbContext.Connection.ExecuteAsync(
                    @"INSERT INTO Artwork (ArtworkKind, EpisodeMetadataId, DateAdded, DateUpdated, Path, SourcePath, BlurHash43, BlurHash54, BlurHash64)
                            VALUES (@ArtworkKind, @Id, @DateAdded, @DateUpdated, @Path, @SourcePath, @BlurHash43, @BlurHash54, @BlurHash64)",
                    parameters)
                .ToUnit(),
            ArtistMetadata => await dbContext.Connection.ExecuteAsync(
                    @"INSERT INTO Artwork (ArtworkKind, ArtistMetadataId, DateAdded, DateUpdated, Path, SourcePath, BlurHash43, BlurHash54, BlurHash64)
                            Values (@ArtworkKind, @Id, @DateAdded, @DateUpdated, @Path, @SourcePath, @BlurHash43, @BlurHash54, @BlurHash64)",
                    parameters)
                .ToUnit(),
            MusicVideoMetadata => await dbContext.Connection.ExecuteAsync(
                    @"INSERT INTO Artwork (ArtworkKind, MusicVideoMetadataId, DateAdded, DateUpdated, Path, SourcePath, BlurHash43, BlurHash54, BlurHash64)
                            VALUES (@ArtworkKind, @Id, @DateAdded, @DateUpdated, @Path, @SourcePath, @BlurHash43, @BlurHash54, @BlurHash64)",
                    parameters)
                .ToUnit(),
            SongMetadata => await dbContext.Connection.ExecuteAsync(
                    @"INSERT INTO Artwork (ArtworkKind, SongMetadataId, DateAdded, DateUpdated, Path, SourcePath, BlurHash43, BlurHash54, BlurHash64)
                            VALUES (@ArtworkKind, @Id, @DateAdded, @DateUpdated, @Path, @SourcePath, @BlurHash43, @BlurHash54, @BlurHash64)",
                    parameters)
                .ToUnit(),
            _ => Unit.Default
        };
    }

    public async Task<Unit> RemoveArtwork(Metadata metadata, ArtworkKind artworkKind)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.ExecuteAsync(
            @"DELETE FROM Artwork WHERE ArtworkKind = @ArtworkKind AND (MovieMetadataId = @Id
                OR ShowMetadataId = @Id OR SeasonMetadataId = @Id OR EpisodeMetadataId = @Id)",
            new { ArtworkKind = artworkKind, metadata.Id }).ToUnit();
    }

    public async Task<bool> CloneArtwork(
        Metadata metadata,
        Option<Artwork> maybeArtwork,
        ArtworkKind artworkKind,
        string sourcePath,
        DateTime lastWriteTime)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        Option<Artwork> maybeExisting = await dbContext.Artwork
            .AsNoTracking()
            .Filter(
                a => a.SourcePath == sourcePath && a.ArtworkKind == artworkKind && a.DateUpdated == lastWriteTime)
            .FirstOrDefaultAsync()
            .Map(Optional);
        foreach (Artwork existing in maybeExisting)
        {
            Artwork artwork = await maybeArtwork.IfNoneAsync(new Artwork());
            if (maybeArtwork.IsNone)
            {
                metadata.Artwork.Add(artwork);
            }

            artwork.Path = existing.Path;
            artwork.SourcePath = existing.SourcePath;
            artwork.ArtworkKind = artworkKind;
            artwork.BlurHash43 = existing.BlurHash43;
            artwork.BlurHash54 = existing.BlurHash54;
            artwork.BlurHash64 = existing.BlurHash64;
            artwork.DateAdded = existing.DateAdded;
            artwork.DateUpdated = existing.DateUpdated;

            if (maybeArtwork.IsNone)
            {
                await AddArtwork(metadata, artwork);
            }
            else
            {
                await UpdateArtworkPath(artwork);
            }

            return true;
        }

        return false;
    }

    public async Task<Unit> MarkAsUpdated(ShowMetadata metadata, DateTime dateUpdated)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.ExecuteAsync(
            @"UPDATE ShowMetadata SET DateUpdated = @DateUpdated WHERE Id = @Id",
            new { DateUpdated = dateUpdated, metadata.Id }).ToUnit();
    }

    public async Task<Unit> MarkAsUpdated(SeasonMetadata metadata, DateTime dateUpdated)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.ExecuteAsync(
            @"UPDATE SeasonMetadata SET DateUpdated = @DateUpdated WHERE Id = @Id",
            new { DateUpdated = dateUpdated, metadata.Id }).ToUnit();
    }

    public async Task<Unit> MarkAsUpdated(MovieMetadata metadata, DateTime dateUpdated)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.ExecuteAsync(
            @"UPDATE MovieMetadata SET DateUpdated = @DateUpdated WHERE Id = @Id",
            new { DateUpdated = dateUpdated, metadata.Id }).ToUnit();
    }

    public async Task<Unit> MarkAsUpdated(EpisodeMetadata metadata, DateTime dateUpdated)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.ExecuteAsync(
            @"UPDATE EpisodeMetadata SET DateUpdated = @DateUpdated WHERE Id = @Id",
            new { DateUpdated = dateUpdated, metadata.Id }).ToUnit();
    }

    public async Task<Unit> MarkAsExternal(ShowMetadata metadata)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.ExecuteAsync(
            @"UPDATE ShowMetadata SET MetadataKind = @Kind WHERE Id = @Id",
            new { metadata.Id, Kind = (int)MetadataKind.External }).ToUnit();
    }

    public async Task<Unit> SetContentRating(ShowMetadata metadata, string contentRating)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.ExecuteAsync(
            @"UPDATE ShowMetadata SET ContentRating = @ContentRating WHERE Id = @Id",
            new { metadata.Id, ContentRating = contentRating }).ToUnit();
    }

    public async Task<Unit> MarkAsExternal(MovieMetadata metadata)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.ExecuteAsync(
            @"UPDATE MovieMetadata SET MetadataKind = @Kind WHERE Id = @Id",
            new { metadata.Id, Kind = (int)MetadataKind.External }).ToUnit();
    }

    public async Task<Unit> SetContentRating(MovieMetadata metadata, string contentRating)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.ExecuteAsync(
            @"UPDATE MovieMetadata SET ContentRating = @ContentRating WHERE Id = @Id",
            new { metadata.Id, ContentRating = contentRating }).ToUnit();
    }

    public async Task<bool> RemoveGuid(MetadataGuid guid)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.ExecuteAsync(
                "DELETE FROM MetadataGuid WHERE Id = @GuidId",
                new { GuidId = guid.Id })
            .Map(result => result > 0);
    }

    public async Task<bool> AddGuid(Metadata metadata, MetadataGuid guid)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return metadata switch
        {
            MovieMetadata =>
                await dbContext.Connection.ExecuteAsync(
                    "INSERT INTO MetadataGuid (Guid, MovieMetadataId) VALUES (@Guid, @MetadataId)",
                    new { guid.Guid, MetadataId = metadata.Id }).Map(result => result > 0),
            ShowMetadata =>
                await dbContext.Connection.ExecuteAsync(
                    "INSERT INTO MetadataGuid (Guid, ShowMetadataId) VALUES (@Guid, @MetadataId)",
                    new { guid.Guid, MetadataId = metadata.Id }).Map(result => result > 0),
            SeasonMetadata =>
                await dbContext.Connection.ExecuteAsync(
                    "INSERT INTO MetadataGuid (Guid, SeasonMetadataId) VALUES (@Guid, @MetadataId)",
                    new { guid.Guid, MetadataId = metadata.Id }).Map(result => result > 0),
            EpisodeMetadata =>
                await dbContext.Connection.ExecuteAsync(
                    "INSERT INTO MetadataGuid (Guid, EpisodeMetadataId) VALUES (@Guid, @MetadataId)",
                    new { guid.Guid, MetadataId = metadata.Id }).Map(result => result > 0),
            ArtistMetadata =>
                await dbContext.Connection.ExecuteAsync(
                    "INSERT INTO MetadataGuid (Guid, ArtistMetadataId) VALUES (@Guid, @MetadataId)",
                    new { guid.Guid, MetadataId = metadata.Id }).Map(result => result > 0),
            _ => throw new NotSupportedException()
        };
    }

    public async Task<bool> RemoveDirector(Director director)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.ExecuteAsync(
                "DELETE FROM Director WHERE Id = @DirectorId",
                new { DirectorId = director.Id })
            .Map(result => result > 0);
    }

    public async Task<bool> RemoveWriter(Writer writer)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.ExecuteAsync(
                "DELETE FROM Writer WHERE Id = @WriterId",
                new { WriterId = writer.Id })
            .Map(result => result > 0);
    }

    public async Task<bool> RemoveGenre(Genre genre)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.ExecuteAsync(
                "DELETE FROM Genre WHERE Id = @GenreId",
                new { GenreId = genre.Id })
            .Map(result => result > 0);
    }

    public async Task<bool> RemoveTag(Tag tag)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.ExecuteAsync("DELETE FROM Tag WHERE Id = @TagId", new { TagId = tag.Id })
            .Map(result => result > 0);
    }

    public async Task<bool> RemoveStudio(Studio studio)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.ExecuteAsync(
                "DELETE FROM Studio WHERE Id = @StudioId",
                new { StudioId = studio.Id })
            .Map(result => result > 0);
    }

    public async Task<bool> RemoveStyle(Style style)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.ExecuteAsync(
                "DELETE FROM Style WHERE Id = @StyleId",
                new { StyleId = style.Id })
            .Map(result => result > 0);
    }

    public async Task<bool> RemoveMood(Mood mood)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.ExecuteAsync("DELETE FROM Mood WHERE Id = @MoodId", new { MoodId = mood.Id })
            .Map(result => result > 0);
    }
}