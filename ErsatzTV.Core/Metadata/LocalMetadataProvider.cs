using System.Xml.Serialization;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Metadata.Nfo;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Metadata.Nfo;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Metadata;

public class LocalMetadataProvider : ILocalMetadataProvider
{
    private static readonly XmlSerializer MovieSerializer = new(typeof(MovieNfo));
    private static readonly XmlSerializer TvShowSerializer = new(typeof(TvShowNfo));
    private static readonly XmlSerializer ArtistSerializer = new(typeof(ArtistNfo));
    private static readonly XmlSerializer MusicVideoSerializer = new(typeof(MusicVideoNfo));
    private readonly IArtistRepository _artistRepository;
    private readonly IEpisodeNfoReader _episodeNfoReader;
    private readonly ILocalStatisticsProvider _localStatisticsProvider;
    private readonly IFallbackMetadataProvider _fallbackMetadataProvider;
    private readonly ILocalFileSystem _localFileSystem;
    private readonly ILogger<LocalMetadataProvider> _logger;

    private readonly IMetadataRepository _metadataRepository;
    private readonly IMovieRepository _movieRepository;
    private readonly IMusicVideoRepository _musicVideoRepository;
    private readonly IOtherVideoRepository _otherVideoRepository;
    private readonly ISongRepository _songRepository;
    private readonly ITelevisionRepository _televisionRepository;

    public LocalMetadataProvider(
        IMetadataRepository metadataRepository,
        IMovieRepository movieRepository,
        ITelevisionRepository televisionRepository,
        IArtistRepository artistRepository,
        IMusicVideoRepository musicVideoRepository,
        IOtherVideoRepository otherVideoRepository,
        ISongRepository songRepository,
        IFallbackMetadataProvider fallbackMetadataProvider,
        ILocalFileSystem localFileSystem,
        IEpisodeNfoReader episodeNfoReader,
        ILocalStatisticsProvider localStatisticsProvider,
        ILogger<LocalMetadataProvider> logger)
    {
        _metadataRepository = metadataRepository;
        _movieRepository = movieRepository;
        _televisionRepository = televisionRepository;
        _artistRepository = artistRepository;
        _musicVideoRepository = musicVideoRepository;
        _otherVideoRepository = otherVideoRepository;
        _songRepository = songRepository;
        _fallbackMetadataProvider = fallbackMetadataProvider;
        _localFileSystem = localFileSystem;
        _episodeNfoReader = episodeNfoReader;
        _localStatisticsProvider = localStatisticsProvider;
        _logger = logger;
    }

    public async Task<ShowMetadata> GetMetadataForShow(string showFolder)
    {
        string nfoFileName = Path.Combine(showFolder, "tvshow.nfo");
        Option<ShowMetadata> maybeMetadata = None;
        if (_localFileSystem.FileExists(nfoFileName))
        {
            maybeMetadata = await LoadTelevisionShowMetadata(nfoFileName);
        }

        return maybeMetadata.Match(
            metadata =>
            {
                metadata.SortTitle = _fallbackMetadataProvider.GetSortTitle(metadata.Title);
                return metadata;
            },
            () =>
            {
                ShowMetadata metadata = _fallbackMetadataProvider.GetFallbackMetadataForShow(showFolder);
                metadata.SortTitle = _fallbackMetadataProvider.GetSortTitle(metadata.Title);
                return metadata;
            });
    }

    public async Task<ArtistMetadata> GetMetadataForArtist(string artistFolder)
    {
        string nfoFileName = Path.Combine(artistFolder, "artist.nfo");
        Option<ArtistMetadata> maybeMetadata = None;
        if (_localFileSystem.FileExists(nfoFileName))
        {
            maybeMetadata = await LoadArtistMetadata(nfoFileName);
        }

        return maybeMetadata.Match(
            metadata =>
            {
                metadata.SortTitle = _fallbackMetadataProvider.GetSortTitle(metadata.Title);
                return metadata;
            },
            () =>
            {
                ArtistMetadata metadata = _fallbackMetadataProvider.GetFallbackMetadataForArtist(artistFolder);
                metadata.SortTitle = _fallbackMetadataProvider.GetSortTitle(metadata.Title);
                return metadata;
            });
    }

    public Task<bool> RefreshSidecarMetadata(Movie movie, string nfoFileName) =>
        LoadMovieMetadata(movie, nfoFileName).Bind(
            maybeMetadata => maybeMetadata.Match(
                metadata => ApplyMetadataUpdate(movie, metadata),
                () => Task.FromResult(false)));

    public Task<bool> RefreshSidecarMetadata(Show televisionShow, string nfoFileName) =>
        LoadTelevisionShowMetadata(nfoFileName).Bind(
            maybeMetadata => maybeMetadata.Match(
                metadata => ApplyMetadataUpdate(televisionShow, metadata),
                () => Task.FromResult(false)));

    public Task<bool> RefreshSidecarMetadata(Episode episode, string nfoFileName) =>
        LoadEpisodeMetadata(episode, nfoFileName).Bind(metadata => ApplyMetadataUpdate(episode, metadata));

    public Task<bool> RefreshSidecarMetadata(Artist artist, string nfoFileName) =>
        LoadArtistMetadata(nfoFileName).Bind(
            maybeMetadata => maybeMetadata.Match(
                metadata => ApplyMetadataUpdate(artist, metadata),
                () => Task.FromResult(false)));

    public Task<bool> RefreshSidecarMetadata(MusicVideo musicVideo, string nfoFileName) =>
        LoadMusicVideoMetadata(nfoFileName).Bind(
            maybeMetadata => maybeMetadata.Match(
                metadata => ApplyMetadataUpdate(musicVideo, metadata),
                () => RefreshFallbackMetadata(musicVideo)));

    public Task<bool> RefreshTagMetadata(Song song, string ffprobePath) =>
        LoadSongMetadata(song, ffprobePath).Bind(
            maybeMetadata => maybeMetadata.Match(
                metadata => ApplyMetadataUpdate(song, metadata),
                () => RefreshFallbackMetadata(song)));

    public Task<bool> RefreshFallbackMetadata(Movie movie) =>
        ApplyMetadataUpdate(movie, _fallbackMetadataProvider.GetFallbackMetadata(movie));

    public Task<bool> RefreshFallbackMetadata(Episode episode) =>
        ApplyMetadataUpdate(episode, _fallbackMetadataProvider.GetFallbackMetadata(episode));

    public Task<bool> RefreshFallbackMetadata(Artist artist, string artistFolder) =>
        ApplyMetadataUpdate(artist, _fallbackMetadataProvider.GetFallbackMetadataForArtist(artistFolder));

    public Task<bool> RefreshFallbackMetadata(OtherVideo otherVideo) =>
        _fallbackMetadataProvider.GetFallbackMetadata(otherVideo).Match(
            metadata => ApplyMetadataUpdate(otherVideo, metadata),
            () => Task.FromResult(false));

    public Task<bool> RefreshFallbackMetadata(Song song) =>
        _fallbackMetadataProvider.GetFallbackMetadata(song).Match(
            metadata => ApplyMetadataUpdate(song, metadata),
            () => Task.FromResult(false));

    public Task<bool> RefreshFallbackMetadata(MusicVideo musicVideo) =>
        _fallbackMetadataProvider.GetFallbackMetadata(musicVideo).Match(
            metadata => ApplyMetadataUpdate(musicVideo, metadata),
            () => Task.FromResult(false));

    public Task<bool> RefreshFallbackMetadata(Show televisionShow, string showFolder) =>
        ApplyMetadataUpdate(televisionShow, _fallbackMetadataProvider.GetFallbackMetadataForShow(showFolder));

    private async Task<Option<MusicVideoMetadata>> LoadMusicVideoMetadata(string nfoFileName)
    {
        try
        {
            await using FileStream fileStream = File.Open(nfoFileName, FileMode.Open, FileAccess.Read);
            Option<MusicVideoNfo> maybeNfo = MusicVideoSerializer.Deserialize(fileStream) as MusicVideoNfo;
            return maybeNfo.Match<Option<MusicVideoMetadata>>(
                nfo => new MusicVideoMetadata
                {
                    MetadataKind = MetadataKind.Sidecar,
                    DateAdded = DateTime.UtcNow,
                    DateUpdated = File.GetLastWriteTimeUtc(nfoFileName),
                    Album = nfo.Album,
                    Title = nfo.Title,
                    Plot = nfo.Plot,
                    Year = GetYear(nfo.Year, nfo.Premiered),
                    ReleaseDate = GetAired(nfo.Year, nfo.Premiered),
                    Genres = nfo.Genres.Map(g => new Genre { Name = g }).ToList(),
                    Tags = nfo.Tags.Map(t => new Tag { Name = t }).ToList(),
                    Studios = nfo.Studios.Map(s => new Studio { Name = s }).ToList()
                },
                None);
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex, "Failed to read music video nfo metadata from {Path}", nfoFileName);
            return None;
        }
    }

    private async Task<Option<SongMetadata>> LoadSongMetadata(Song song, string ffprobePath)
    {
        string path = song.GetHeadVersion().MediaFiles.Head().Path;

        try
        {
            Either<BaseError, Dictionary<string, string>> maybeTags =
                await _localStatisticsProvider.GetFormatTags(ffprobePath, song);

            return maybeTags.Match(
                tags =>
                {
                    Option<SongMetadata> maybeFallbackMetadata =
                        _fallbackMetadataProvider.GetFallbackMetadata(song);
                        
                    var result = new SongMetadata
                    {
                        MetadataKind = MetadataKind.Embedded,
                        DateAdded = DateTime.UtcNow,
                        DateUpdated = File.GetLastWriteTimeUtc(path),
                            
                        Artwork = new List<Artwork>(),
                        Actors = new List<Actor>(),
                        Genres = new List<Genre>(),
                        Studios = new List<Studio>(),
                        Tags = new List<Tag>()
                    };
                        
                    if (tags.TryGetValue(MetadataFormatTag.Album, out string album))
                    {
                        result.Album = album;
                    }

                    if (tags.TryGetValue(MetadataFormatTag.Artist, out string artist))
                    {
                        result.Artist = artist;
                    }

                    if (tags.TryGetValue(MetadataFormatTag.Date, out string date))
                    {
                        result.Date = date;
                    }

                    if (tags.TryGetValue(MetadataFormatTag.Genre, out string genre))
                    {
                        result.Genres.AddRange(SplitGenres(genre).Map(n => new Genre { Name = n }));
                    }

                    if (tags.TryGetValue(MetadataFormatTag.Title, out string title))
                    {
                        result.Title = title;
                    }

                    if (tags.TryGetValue(MetadataFormatTag.Track, out string track))
                    {
                        result.Track = track;
                    }

                    foreach (SongMetadata fallbackMetadata in maybeFallbackMetadata)
                    {
                        if (string.IsNullOrWhiteSpace(result.Title))
                        {
                            result.Title = fallbackMetadata.Title;
                        }

                        result.OriginalTitle = fallbackMetadata.OriginalTitle;

                        // preserve folder tagging - maybe someone uses this
                        foreach (Tag tag in fallbackMetadata.Tags)
                        {
                            result.Tags.Add(tag);
                        }
                    }

                    return result;
                },
                _ => Option<SongMetadata>.None);
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex, "Failed to read embedded song metadata from {Path}", path);
            return None;
        }
    }

    private async Task<bool> ApplyMetadataUpdate(Episode episode, List<EpisodeMetadata> episodeMetadata)
    {
        var updated = false;

        episode.EpisodeMetadata ??= new List<EpisodeMetadata>();

        var toUpdate = episode.EpisodeMetadata
            .Where(em => episodeMetadata.Any(em2 => em2.EpisodeNumber == em.EpisodeNumber))
            .ToList();
        var toRemove = episode.EpisodeMetadata.Except(toUpdate).ToList();
        var toAdd = episodeMetadata
            .Where(em => episode.EpisodeMetadata.All(em2 => em2.EpisodeNumber != em.EpisodeNumber))
            .ToList();

        foreach (EpisodeMetadata metadata in toRemove)
        {
            await _televisionRepository.RemoveMetadata(episode, metadata);
            updated = true;
        }

        foreach (EpisodeMetadata metadata in toAdd)
        {
            metadata.SortTitle = string.IsNullOrWhiteSpace(metadata.SortTitle)
                ? _fallbackMetadataProvider.GetSortTitle(metadata.Title)
                : metadata.SortTitle;
            metadata.EpisodeId = episode.Id;
            metadata.Episode = episode;
            episode.EpisodeMetadata.Add(metadata);

            updated = await _metadataRepository.Add(metadata) || updated;
        }

        foreach (EpisodeMetadata existing in toUpdate)
        {
            Option<EpisodeMetadata> maybeIncoming =
                episodeMetadata.Find(em => em.EpisodeNumber == existing.EpisodeNumber);
            updated = await maybeIncoming.Match(
                async metadata =>
                {
                    existing.Outline = metadata.Outline;
                    existing.Plot = metadata.Plot;
                    existing.Tagline = metadata.Tagline;
                    existing.Title = metadata.Title;

                    if (existing.DateAdded == SystemTime.MinValueUtc)
                    {
                        existing.DateAdded = metadata.DateAdded;
                    }

                    existing.DateUpdated = metadata.DateUpdated;
                    existing.MetadataKind = metadata.MetadataKind;
                    existing.OriginalTitle = metadata.OriginalTitle;
                    existing.ReleaseDate = metadata.ReleaseDate;
                    existing.Year = metadata.Year;
                    existing.SortTitle = string.IsNullOrWhiteSpace(metadata.SortTitle)
                        ? _fallbackMetadataProvider.GetSortTitle(metadata.Title)
                        : metadata.SortTitle;

                    updated = await UpdateMetadataCollections(
                        existing,
                        metadata,
                        (_, _) => Task.FromResult(false),
                        (_, _) => Task.FromResult(false),
                        (_, _) => Task.FromResult(false),
                        _televisionRepository.AddActor) || updated;

                    foreach (Director director in existing.Directors
                                 .Filter(d => metadata.Directors.All(d2 => d2.Name != d.Name)).ToList())
                    {
                        existing.Directors.Remove(director);
                        if (await _metadataRepository.RemoveDirector(director))
                        {
                            updated = true;
                        }
                    }

                    foreach (Director director in metadata.Directors
                                 .Filter(d => existing.Directors.All(d2 => d2.Name != d.Name)).ToList())
                    {
                        existing.Directors.Add(director);
                        if (await _televisionRepository.AddDirector(existing, director))
                        {
                            updated = true;
                        }
                    }

                    foreach (Writer writer in existing.Writers
                                 .Filter(w => metadata.Writers.All(w2 => w2.Name != w.Name)).ToList())
                    {
                        existing.Writers.Remove(writer);
                        if (await _metadataRepository.RemoveWriter(writer))
                        {
                            updated = true;
                        }
                    }

                    foreach (Writer writer in metadata.Writers
                                 .Filter(w => existing.Writers.All(w2 => w2.Name != w.Name)).ToList())
                    {
                        existing.Writers.Add(writer);
                        if (await _televisionRepository.AddWriter(existing, writer))
                        {
                            updated = true;
                        }
                    }

                    foreach (MetadataGuid guid in existing.Guids
                                 .Filter(g => metadata.Guids.All(g2 => g2.Guid != g.Guid)).ToList())
                    {
                        existing.Guids.Remove(guid);
                        if (await _metadataRepository.RemoveGuid(guid))
                        {
                            updated = true;
                        }
                    }

                    foreach (MetadataGuid guid in metadata.Guids
                                 .Filter(g => existing.Guids.All(g2 => g2.Guid != g.Guid)).ToList())
                    {
                        existing.Guids.Add(guid);
                        if (await _metadataRepository.AddGuid(existing, guid))
                        {
                            updated = true;
                        }
                    }

                    return await _metadataRepository.Update(existing) || updated;
                },
                () => Task.FromResult(updated)) || updated;
        }

        return updated;
    }

    private Task<bool> ApplyMetadataUpdate(Movie movie, MovieMetadata metadata) =>
        Optional(movie.MovieMetadata).Flatten().HeadOrNone().Match(
            async existing =>
            {
                existing.ContentRating = metadata.ContentRating;
                existing.Outline = metadata.Outline;
                existing.Plot = metadata.Plot;
                existing.Tagline = metadata.Tagline;
                existing.Title = metadata.Title;

                if (existing.DateAdded == SystemTime.MinValueUtc)
                {
                    existing.DateAdded = metadata.DateAdded;
                }

                existing.DateUpdated = metadata.DateUpdated;
                existing.MetadataKind = metadata.MetadataKind;
                existing.OriginalTitle = metadata.OriginalTitle;
                existing.ReleaseDate = metadata.ReleaseDate;
                existing.Year = metadata.Year;
                existing.SortTitle = string.IsNullOrWhiteSpace(metadata.SortTitle)
                    ? _fallbackMetadataProvider.GetSortTitle(metadata.Title)
                    : metadata.SortTitle;

                bool updated = await UpdateMetadataCollections(
                    existing,
                    metadata,
                    _movieRepository.AddGenre,
                    _movieRepository.AddTag,
                    _movieRepository.AddStudio,
                    _movieRepository.AddActor);

                foreach (Director director in existing.Directors
                             .Filter(d => metadata.Directors.All(d2 => d2.Name != d.Name)).ToList())
                {
                    existing.Directors.Remove(director);
                    if (await _metadataRepository.RemoveDirector(director))
                    {
                        updated = true;
                    }
                }

                foreach (Director director in metadata.Directors
                             .Filter(d => existing.Directors.All(d2 => d2.Name != d.Name)).ToList())
                {
                    existing.Directors.Add(director);
                    if (await _movieRepository.AddDirector(existing, director))
                    {
                        updated = true;
                    }
                }

                foreach (Writer writer in existing.Writers
                             .Filter(w => metadata.Writers.All(w2 => w2.Name != w.Name)).ToList())
                {
                    existing.Writers.Remove(writer);
                    if (await _metadataRepository.RemoveWriter(writer))
                    {
                        updated = true;
                    }
                }

                foreach (Writer writer in metadata.Writers
                             .Filter(w => existing.Writers.All(w2 => w2.Name != w.Name)).ToList())
                {
                    existing.Writers.Add(writer);
                    if (await _movieRepository.AddWriter(existing, writer))
                    {
                        updated = true;
                    }
                }

                foreach (MetadataGuid guid in existing.Guids
                             .Filter(g => metadata.Guids.All(g2 => g2.Guid != g.Guid)).ToList())
                {
                    existing.Guids.Remove(guid);
                    if (await _metadataRepository.RemoveGuid(guid))
                    {
                        updated = true;
                    }
                }

                foreach (MetadataGuid guid in metadata.Guids
                             .Filter(g => existing.Guids.All(g2 => g2.Guid != g.Guid)).ToList())
                {
                    existing.Guids.Add(guid);
                    if (await _metadataRepository.AddGuid(existing, guid))
                    {
                        updated = true;
                    }
                }

                return await _metadataRepository.Update(existing) || updated;
            },
            async () =>
            {
                metadata.SortTitle = string.IsNullOrWhiteSpace(metadata.SortTitle)
                    ? _fallbackMetadataProvider.GetSortTitle(metadata.Title)
                    : metadata.SortTitle;
                metadata.MovieId = movie.Id;
                movie.MovieMetadata = new List<MovieMetadata> { metadata };

                return await _metadataRepository.Add(metadata);
            });

    private Task<bool> ApplyMetadataUpdate(Show show, ShowMetadata metadata) =>
        Optional(show.ShowMetadata).Flatten().HeadOrNone().Match(
            async existing =>
            {
                existing.ContentRating = metadata.ContentRating;
                existing.Outline = metadata.Outline;
                existing.Plot = metadata.Plot;
                existing.Tagline = metadata.Tagline;
                existing.Title = metadata.Title;

                if (existing.DateAdded == SystemTime.MinValueUtc)
                {
                    existing.DateAdded = metadata.DateAdded;
                }

                existing.DateUpdated = metadata.DateUpdated;
                existing.MetadataKind = metadata.MetadataKind;
                existing.OriginalTitle = metadata.OriginalTitle;
                existing.ReleaseDate = metadata.ReleaseDate;
                existing.Year = metadata.Year;
                existing.SortTitle = string.IsNullOrWhiteSpace(metadata.SortTitle)
                    ? _fallbackMetadataProvider.GetSortTitle(metadata.Title)
                    : metadata.SortTitle;

                bool updated = await UpdateMetadataCollections(
                    existing,
                    metadata,
                    _televisionRepository.AddGenre,
                    _televisionRepository.AddTag,
                    _televisionRepository.AddStudio,
                    _televisionRepository.AddActor);

                foreach (MetadataGuid guid in existing.Guids
                             .Filter(g => metadata.Guids.All(g2 => g2.Guid != g.Guid)).ToList())
                {
                    existing.Guids.Remove(guid);
                    if (await _metadataRepository.RemoveGuid(guid))
                    {
                        updated = true;
                    }
                }

                foreach (MetadataGuid guid in metadata.Guids
                             .Filter(g => existing.Guids.All(g2 => g2.Guid != g.Guid)).ToList())
                {
                    existing.Guids.Add(guid);
                    if (await _metadataRepository.AddGuid(existing, guid))
                    {
                        updated = true;
                    }
                }

                return await _metadataRepository.Update(existing) || updated;
            },
            async () =>
            {
                metadata.SortTitle = string.IsNullOrWhiteSpace(metadata.SortTitle)
                    ? _fallbackMetadataProvider.GetSortTitle(metadata.Title)
                    : metadata.SortTitle;
                metadata.ShowId = show.Id;
                show.ShowMetadata = new List<ShowMetadata> { metadata };

                return await _metadataRepository.Add(metadata);
            });

    private Task<bool> ApplyMetadataUpdate(Artist artist, ArtistMetadata metadata) =>
        Optional(artist.ArtistMetadata).Flatten().HeadOrNone().Match(
            async existing =>
            {
                existing.Title = metadata.Title;
                existing.Disambiguation = metadata.Disambiguation;
                existing.Biography = metadata.Biography;

                if (existing.DateAdded == SystemTime.MinValueUtc)
                {
                    existing.DateAdded = metadata.DateAdded;
                }

                existing.DateUpdated = metadata.DateUpdated;
                existing.MetadataKind = metadata.MetadataKind;
                existing.SortTitle = string.IsNullOrWhiteSpace(metadata.SortTitle)
                    ? _fallbackMetadataProvider.GetSortTitle(metadata.Title)
                    : metadata.SortTitle;

                var updated = false;

                foreach (Genre genre in existing.Genres.Filter(g => metadata.Genres.All(g2 => g2.Name != g.Name))
                             .ToList())
                {
                    existing.Genres.Remove(genre);
                    if (await _metadataRepository.RemoveGenre(genre))
                    {
                        updated = true;
                    }
                }

                foreach (Genre genre in metadata.Genres.Filter(g => existing.Genres.All(g2 => g2.Name != g.Name))
                             .ToList())
                {
                    existing.Genres.Add(genre);
                    if (await _artistRepository.AddGenre(existing, genre))
                    {
                        updated = true;
                    }
                }

                foreach (Style style in existing.Styles.Filter(s => metadata.Styles.All(s2 => s2.Name != s.Name))
                             .ToList())
                {
                    existing.Styles.Remove(style);
                    if (await _metadataRepository.RemoveStyle(style))
                    {
                        updated = true;
                    }
                }

                foreach (Style style in metadata.Styles.Filter(s => existing.Styles.All(s2 => s2.Name != s.Name))
                             .ToList())
                {
                    existing.Styles.Add(style);
                    if (await _artistRepository.AddStyle(existing, style))
                    {
                        updated = true;
                    }
                }

                foreach (Mood mood in existing.Moods.Filter(m => metadata.Moods.All(m2 => m2.Name != m.Name))
                             .ToList())
                {
                    existing.Moods.Remove(mood);
                    if (await _metadataRepository.RemoveMood(mood))
                    {
                        updated = true;
                    }
                }

                foreach (Mood mood in metadata.Moods.Filter(s => existing.Moods.All(m2 => m2.Name != s.Name))
                             .ToList())
                {
                    existing.Moods.Add(mood);
                    if (await _artistRepository.AddMood(existing, mood))
                    {
                        updated = true;
                    }
                }

                return await _metadataRepository.Update(existing) || updated;
            },
            async () =>
            {
                metadata.SortTitle = string.IsNullOrWhiteSpace(metadata.SortTitle)
                    ? _fallbackMetadataProvider.GetSortTitle(metadata.Title)
                    : metadata.SortTitle;
                metadata.ArtistId = artist.Id;
                artist.ArtistMetadata = new List<ArtistMetadata> { metadata };

                return await _metadataRepository.Add(metadata);
            });

    private Task<bool> ApplyMetadataUpdate(MusicVideo musicVideo, MusicVideoMetadata metadata) =>
        Optional(musicVideo.MusicVideoMetadata).Flatten().HeadOrNone().Match(
            async existing =>
            {
                existing.Title = metadata.Title;
                existing.Year = metadata.Year;
                existing.Plot = metadata.Plot;
                existing.Album = metadata.Album;

                if (existing.DateAdded == SystemTime.MinValueUtc)
                {
                    existing.DateAdded = metadata.DateAdded;
                }

                existing.DateUpdated = metadata.DateUpdated;
                existing.MetadataKind = metadata.MetadataKind;
                existing.OriginalTitle = metadata.OriginalTitle;
                existing.ReleaseDate = metadata.ReleaseDate;
                existing.SortTitle = string.IsNullOrWhiteSpace(metadata.SortTitle)
                    ? _fallbackMetadataProvider.GetSortTitle(metadata.Title)
                    : metadata.SortTitle;

                bool updated = await UpdateMetadataCollections(
                    existing,
                    metadata,
                    _musicVideoRepository.AddGenre,
                    _musicVideoRepository.AddTag,
                    _musicVideoRepository.AddStudio,
                    (_, _) => Task.FromResult(false));

                return await _metadataRepository.Update(existing) || updated;
            },
            async () =>
            {
                metadata.SortTitle = string.IsNullOrWhiteSpace(metadata.SortTitle)
                    ? _fallbackMetadataProvider.GetSortTitle(metadata.Title)
                    : metadata.SortTitle;
                metadata.MusicVideoId = musicVideo.Id;
                musicVideo.MusicVideoMetadata = new List<MusicVideoMetadata> { metadata };

                return await _metadataRepository.Add(metadata);
            });
        
    private Task<bool> ApplyMetadataUpdate(OtherVideo otherVideo, OtherVideoMetadata metadata) =>
        Optional(otherVideo.OtherVideoMetadata).Flatten().HeadOrNone().Match(
            async existing =>
            {
                existing.Title = metadata.Title;

                if (existing.DateAdded == SystemTime.MinValueUtc)
                {
                    existing.DateAdded = metadata.DateAdded;
                }

                existing.DateUpdated = metadata.DateUpdated;
                existing.MetadataKind = metadata.MetadataKind;
                existing.SortTitle = string.IsNullOrWhiteSpace(metadata.SortTitle)
                    ? _fallbackMetadataProvider.GetSortTitle(metadata.Title)
                    : metadata.SortTitle;
                existing.OriginalTitle = metadata.OriginalTitle;

                bool updated = await UpdateMetadataCollections(
                    existing,
                    metadata,
                    (_, _) => Task.FromResult(false),
                    _otherVideoRepository.AddTag,
                    (_, _) => Task.FromResult(false),
                    (_, _) => Task.FromResult(false));

                return await _metadataRepository.Update(existing) || updated;
            },
            async () =>
            {
                metadata.SortTitle = string.IsNullOrWhiteSpace(metadata.SortTitle)
                    ? _fallbackMetadataProvider.GetSortTitle(metadata.Title)
                    : metadata.SortTitle;
                metadata.OtherVideoId = otherVideo.Id;
                otherVideo.OtherVideoMetadata = new List<OtherVideoMetadata> { metadata };

                return await _metadataRepository.Add(metadata);
            });
        
    private Task<bool> ApplyMetadataUpdate(Song song, SongMetadata metadata) =>
        Optional(song.SongMetadata).Flatten().HeadOrNone().Match(
            async existing =>
            {
                existing.Title = metadata.Title;
                existing.Artist = metadata.Artist;
                existing.Album = metadata.Album;
                existing.Date = metadata.Date;
                existing.Track = metadata.Track;

                if (existing.DateAdded == SystemTime.MinValueUtc)
                {
                    existing.DateAdded = metadata.DateAdded;
                }

                existing.DateUpdated = metadata.DateUpdated;
                existing.MetadataKind = metadata.MetadataKind;
                existing.SortTitle = string.IsNullOrWhiteSpace(metadata.SortTitle)
                    ? _fallbackMetadataProvider.GetSortTitle(metadata.Title)
                    : metadata.SortTitle;
                existing.OriginalTitle = metadata.OriginalTitle;

                bool updated = await UpdateMetadataCollections(
                    existing,
                    metadata,
                    _songRepository.AddGenre,
                    _songRepository.AddTag,
                    (_, _) => Task.FromResult(false),
                    (_, _) => Task.FromResult(false));

                return await _metadataRepository.Update(existing) || updated;
            },
            async () =>
            {
                metadata.SortTitle = string.IsNullOrWhiteSpace(metadata.SortTitle)
                    ? _fallbackMetadataProvider.GetSortTitle(metadata.Title)
                    : metadata.SortTitle;
                metadata.SongId = song.Id;
                song.SongMetadata = new List<SongMetadata> { metadata };

                return await _metadataRepository.Add(metadata);
            });

    private async Task<Option<ShowMetadata>> LoadTelevisionShowMetadata(string nfoFileName)
    {
        try
        {
            await using FileStream fileStream = File.Open(nfoFileName, FileMode.Open, FileAccess.Read);
            Option<TvShowNfo> maybeNfo = TvShowSerializer.Deserialize(fileStream) as TvShowNfo;
            return maybeNfo.Match<Option<ShowMetadata>>(
                nfo =>
                {
                    DateTime dateAdded = DateTime.UtcNow;
                    DateTime dateUpdated = File.GetLastWriteTimeUtc(nfoFileName);

                    return new ShowMetadata
                    {
                        MetadataKind = MetadataKind.Sidecar,
                        DateAdded = dateAdded,
                        DateUpdated = dateUpdated,
                        Title = nfo.Title,
                        Plot = nfo.Plot,
                        Outline = nfo.Outline,
                        Tagline = nfo.Tagline,
                        ContentRating = nfo.ContentRating,
                        Year = GetYear(nfo.Year, nfo.Premiered),
                        ReleaseDate = GetAired(nfo.Year, nfo.Premiered),
                        Genres = nfo.Genres.Map(g => new Genre { Name = g }).ToList(),
                        Tags = nfo.Tags.Map(t => new Tag { Name = t }).ToList(),
                        Studios = nfo.Studios.Map(s => new Studio { Name = s }).ToList(),
                        Actors = Actors(nfo.Actors, dateAdded, dateUpdated),
                        Guids = nfo.UniqueIds
                            .Map(id => new MetadataGuid { Guid = $"{id.Type}://{id.Guid}" })
                            .ToList()
                    };
                },
                None);
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex, "Failed to read TV show nfo metadata from {Path}", nfoFileName);
            return None;
        }
    }

    private async Task<Option<ArtistMetadata>> LoadArtistMetadata(string nfoFileName)
    {
        try
        {
            await using FileStream fileStream = File.Open(nfoFileName, FileMode.Open, FileAccess.Read);
            Option<ArtistNfo> maybeNfo = ArtistSerializer.Deserialize(fileStream) as ArtistNfo;
            return maybeNfo.Match<Option<ArtistMetadata>>(
                nfo => new ArtistMetadata
                {
                    MetadataKind = MetadataKind.Sidecar,
                    DateAdded = DateTime.UtcNow,
                    DateUpdated = File.GetLastWriteTimeUtc(nfoFileName),
                    Title = nfo.Name,
                    Disambiguation = nfo.Disambiguation,
                    Biography = nfo.Biography,
                    Genres = nfo.Genres.Map(g => new Genre { Name = g }).ToList(),
                    Styles = nfo.Styles.Map(s => new Style { Name = s }).ToList(),
                    Moods = nfo.Moods.Map(m => new Mood { Name = m }).ToList()
                },
                None);
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex, "Failed to read artist nfo metadata from {Path}", nfoFileName);
            return None;
        }
    }

    private async Task<List<EpisodeMetadata>> LoadEpisodeMetadata(Episode episode, string nfoFileName)
    {
        try
        {
            await using FileStream fileStream = File.Open(nfoFileName, FileMode.Open, FileAccess.Read);
            List<TvShowEpisodeNfo> nfos = await _episodeNfoReader.Read(fileStream);
            var result = new List<EpisodeMetadata>();
            foreach (TvShowEpisodeNfo nfo in nfos)
            {
                DateTime dateAdded = DateTime.UtcNow;
                DateTime dateUpdated = File.GetLastWriteTimeUtc(nfoFileName);

                var metadata = new EpisodeMetadata
                {
                    MetadataKind = MetadataKind.Sidecar,
                    DateAdded = dateAdded,
                    DateUpdated = dateUpdated,
                    Title = nfo.Title,
                    SortTitle = _fallbackMetadataProvider.GetSortTitle(nfo.Title),
                    EpisodeNumber = nfo.Episode,
                    Year = GetYear(0, nfo.Aired),
                    ReleaseDate = GetAired(0, nfo.Aired),
                    Plot = nfo.Plot,
                    Actors = Actors(nfo.Actors, dateAdded, dateUpdated),
                    Guids = nfo.UniqueIds
                        .Map(id => new MetadataGuid { Guid = $"{id.Type}://{id.Guid}" })
                        .ToList(),
                    Directors = nfo.Directors.Map(d => new Director { Name = d }).ToList(),
                    Writers = nfo.Writers.Map(w => new Writer { Name = w }).ToList(),
                    Genres = new List<Genre>(),
                    Tags = new List<Tag>(),
                    Studios = new List<Studio>(),
                    Artwork = new List<Artwork>()
                };

                result.Add(metadata);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex, "Failed to read TV episode nfo metadata from {Path}", nfoFileName);
            return _fallbackMetadataProvider.GetFallbackMetadata(episode);
        }
    }

    private async Task<Option<MovieMetadata>> LoadMovieMetadata(Movie movie, string nfoFileName)
    {
        try
        {
            await using FileStream fileStream = File.Open(nfoFileName, FileMode.Open, FileAccess.Read);
            Option<MovieNfo> maybeNfo = MovieSerializer.Deserialize(fileStream) as MovieNfo;
            return maybeNfo.Match<Option<MovieMetadata>>(
                nfo =>
                {
                    DateTime dateAdded = DateTime.UtcNow;
                    DateTime dateUpdated = File.GetLastWriteTimeUtc(nfoFileName);

                    return new MovieMetadata
                    {
                        MetadataKind = MetadataKind.Sidecar,
                        DateAdded = dateAdded,
                        DateUpdated = dateUpdated,
                        Title = nfo.Title,
                        Year = nfo.Year,
                        ContentRating = nfo.ContentRating,
                        ReleaseDate = nfo.Premiered,
                        Plot = nfo.Plot,
                        Outline = nfo.Outline,
                        Tagline = nfo.Tagline,
                        Genres = nfo.Genres.Map(g => new Genre { Name = g }).ToList(),
                        Tags = nfo.Tags.Map(t => new Tag { Name = t }).ToList(),
                        Studios = nfo.Studios.Map(s => new Studio { Name = s }).ToList(),
                        Actors = Actors(nfo.Actors, dateAdded, dateUpdated),
                        Directors = nfo.Directors.Map(d => new Director { Name = d }).ToList(),
                        Writers = nfo.Writers.Map(w => new Writer { Name = w }).ToList(),
                        Guids = nfo.UniqueIds
                            .Map(id => new MetadataGuid { Guid = $"{id.Type}://{id.Guid}" })
                            .ToList()
                    };
                },
                None);
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex, "Failed to read Movie nfo metadata from {Path}", nfoFileName);
            return _fallbackMetadataProvider.GetFallbackMetadata(movie);
        }
    }

    private static int? GetYear(int? year, string premiered)
    {
        if (year is > 1000)
        {
            return year;
        }

        if (string.IsNullOrWhiteSpace(premiered))
        {
            return null;
        }

        if (DateTime.TryParse(premiered, out DateTime parsed))
        {
            return parsed.Year;
        }

        return null;
    }

    private static DateTime? GetAired(int? year, string aired)
    {
        DateTime? fallback = year is > 1000 ? new DateTime(year.Value, 1, 1) : null;

        if (string.IsNullOrWhiteSpace(aired))
        {
            return fallback;
        }

        return DateTime.TryParse(aired, out DateTime parsed) ? parsed : fallback;
    }

    private async Task<bool> UpdateMetadataCollections<T>(
        T existing,
        T incoming,
        Func<T, Genre, Task<bool>> addGenre,
        Func<T, Tag, Task<bool>> addTag,
        Func<T, Studio, Task<bool>> addStudio,
        Func<T, Actor, Task<bool>> addActor)
        where T : Domain.Metadata
    {
        var updated = false;

        if (existing is not EpisodeMetadata)
        {
            foreach (Genre genre in existing.Genres.Filter(g => incoming.Genres.All(g2 => g2.Name != g.Name))
                         .ToList())
            {
                existing.Genres.Remove(genre);
                if (await _metadataRepository.RemoveGenre(genre))
                {
                    updated = true;
                }
            }

            foreach (Genre genre in incoming.Genres.Filter(g => existing.Genres.All(g2 => g2.Name != g.Name))
                         .ToList())
            {
                existing.Genres.Add(genre);
                if (await addGenre(existing, genre))
                {
                    updated = true;
                }
            }

            foreach (Tag tag in existing.Tags.Filter(t => incoming.Tags.All(t2 => t2.Name != t.Name))
                         .ToList())
            {
                existing.Tags.Remove(tag);
                if (await _metadataRepository.RemoveTag(tag))
                {
                    updated = true;
                }
            }

            foreach (Tag tag in incoming.Tags.Filter(t => existing.Tags.All(t2 => t2.Name != t.Name))
                         .ToList())
            {
                existing.Tags.Add(tag);
                if (await addTag(existing, tag))
                {
                    updated = true;
                }
            }

            foreach (Studio studio in existing.Studios
                         .Filter(s => incoming.Studios.All(s2 => s2.Name != s.Name))
                         .ToList())
            {
                existing.Studios.Remove(studio);
                if (await _metadataRepository.RemoveStudio(studio))
                {
                    updated = true;
                }
            }

            foreach (Studio studio in incoming.Studios
                         .Filter(s => existing.Studios.All(s2 => s2.Name != s.Name))
                         .ToList())
            {
                existing.Studios.Add(studio);
                if (await addStudio(existing, studio))
                {
                    updated = true;
                }
            }
        }

        if (existing is not MusicVideoMetadata and not SongMetadata)
        {
            foreach (Actor actor in existing.Actors
                         .Filter(a => incoming.Actors.All(a2 => a2.Name != a.Name))
                         .ToList())
            {
                existing.Actors.Remove(actor);
                if (await _metadataRepository.RemoveActor(actor))
                {
                    updated = true;
                }
            }

            foreach (Actor actor in incoming.Actors
                         .Filter(a => existing.Actors.All(a2 => a2.Name != a.Name))
                         .ToList())
            {
                existing.Actors.Add(actor);
                if (await addActor(existing, actor))
                {
                    updated = true;
                }
            }
        }

        return updated;
    }

    private static List<Actor> Actors(List<ActorNfo> actorNfos, DateTime dateAdded, DateTime dateUpdated)
    {
        var result = new List<Actor>();

        for (var i = 0; i < actorNfos.Count; i++)
        {
            ActorNfo actorNfo = actorNfos[i];

            var actor = new Actor
            {
                Name = actorNfo.Name,
                Role = actorNfo.Role,
                Order = actorNfo.Order ?? i
            };

            if (!string.IsNullOrWhiteSpace(actorNfo.Thumb))
            {
                actor.Artwork = new Artwork
                {
                    Path = actorNfo.Thumb,
                    ArtworkKind = ArtworkKind.Thumbnail,
                    DateAdded = dateAdded,
                    DateUpdated = dateUpdated
                };
            }

            result.Add(actor);
        }

        return result;
    }
        
    private static IEnumerable<string> SplitGenres(string genre)
    {
        char[] delimiters = new[] { '/', '|', ';', '\\' }
            .Filter(d => genre.IndexOf(d, StringComparison.OrdinalIgnoreCase) != -1)
            .DefaultIfEmpty(',')
            .ToArray();
            
        return genre.Split(delimiters, StringSplitOptions.RemoveEmptyEntries)
            .Where(i => !string.IsNullOrWhiteSpace(i))
            .Select(i => i.Trim());
    }
}