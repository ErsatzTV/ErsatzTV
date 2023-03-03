using System.Text.RegularExpressions;
using Bugsnag;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;

namespace ErsatzTV.Core.Metadata;

public partial class FallbackMetadataProvider : IFallbackMetadataProvider
{
    private static readonly Regex SeasonPattern = SeasonNumber();
    private readonly IClient _client;

    public FallbackMetadataProvider(IClient client) => _client = client;

    public Option<int> GetSeasonNumberForFolder(string folder)
    {
        string folderName = Path.GetFileName(folder) ?? folder;
        if (int.TryParse(folderName, out int seasonNumber))
        {
            return seasonNumber;
        }

        Match match = SeasonPattern.Match(folderName);
        if (match.Success && int.TryParse(match.Groups[1].Value, out seasonNumber))
        {
            return seasonNumber;
        }

        if (folder.EndsWith("specials", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        return None;
    }
    
    [GeneratedRegex(@"s(?:eason)?\s?(\d+)(?![e\d])", RegexOptions.IgnoreCase)]
    private static partial Regex SeasonNumber();

    public ShowMetadata GetFallbackMetadataForShow(string showFolder)
    {
        string fileName = Path.GetFileName(showFolder);
        var metadata = new ShowMetadata
        {
            MetadataKind = MetadataKind.Fallback,
            Title = fileName ?? showFolder,
            Genres = new List<Genre>(),
            Tags = new List<Tag>(),
            Studios = new List<Studio>(),
            Actors = new List<Actor>()
        };
        return GetTelevisionShowMetadata(fileName, metadata);
    }

    public ArtistMetadata GetFallbackMetadataForArtist(string artistFolder)
    {
        string fileName = Path.GetFileName(artistFolder);
        return new ArtistMetadata
            { MetadataKind = MetadataKind.Fallback, Title = fileName ?? artistFolder };
    }

    public List<EpisodeMetadata> GetFallbackMetadata(Episode episode)
    {
        string path = episode.MediaVersions.Head().MediaFiles.Head().Path;
        string fileName = Path.GetFileName(path);
        var baseMetadata = new EpisodeMetadata
        {
            MetadataKind = MetadataKind.Fallback,
            Title = Path.GetFileNameWithoutExtension(path) ?? path,
            DateAdded = DateTime.UtcNow,
            EpisodeNumber = 0,
            Actors = new List<Actor>(),
            Artwork = new List<Artwork>(),
            Directors = new List<Director>(),
            Genres = new List<Genre>(),
            Guids = new List<MetadataGuid>(),
            Studios = new List<Studio>(),
            Tags = new List<Tag>(),
            Writers = new List<Writer>()
        };
        return fileName != null
            ? GetEpisodeMetadata(fileName, baseMetadata)
            : new List<EpisodeMetadata> { baseMetadata };
    }

    public MovieMetadata GetFallbackMetadata(Movie movie)
    {
        string path = movie.MediaVersions.Head().MediaFiles.Head().Path;
        string fileName = Path.GetFileName(path);
        var metadata = new MovieMetadata
        {
            MetadataKind = MetadataKind.Fallback,
            Title = Path.GetFileNameWithoutExtension(path) ?? path,
            Genres = new List<Genre>(),
            Tags = new List<Tag>(),
            Studios = new List<Studio>(),
            Actors = new List<Actor>(),
            Directors = new List<Director>(),
            Writers = new List<Writer>(),
            Guids = new List<MetadataGuid>()
        };

        return fileName != null ? GetMovieMetadata(fileName, metadata) : metadata;
    }

    public Option<MusicVideoMetadata> GetFallbackMetadata(MusicVideo musicVideo)
    {
        string path = musicVideo.MediaVersions.Head().MediaFiles.Head().Path;
        string fileName = Path.GetFileName(path);
        var metadata = new MusicVideoMetadata
        {
            MetadataKind = MetadataKind.Fallback,
            Title = fileName ?? path
        };

        return GetMusicVideoMetadata(fileName, metadata);
    }

    public Option<OtherVideoMetadata> GetFallbackMetadata(OtherVideo otherVideo)
    {
        string path = otherVideo.MediaVersions.Head().MediaFiles.Head().Path;
        string fileName = Path.GetFileNameWithoutExtension(path);
        var metadata = new OtherVideoMetadata
        {
            MetadataKind = MetadataKind.Fallback,
            Title = fileName ?? path,
            OtherVideo = otherVideo,
            Genres = new List<Genre>(),
            Tags = new List<Tag>(),
            Studios = new List<Studio>(),
            Actors = new List<Actor>(),
            Directors = new List<Director>(),
            Writers = new List<Writer>(),
            Guids = new List<MetadataGuid>()
        };

        return GetOtherVideoMetadata(path, metadata);
    }

    public Option<SongMetadata> GetFallbackMetadata(Song song)
    {
        string path = song.MediaVersions.Head().MediaFiles.Head().Path;
        string fileName = Path.GetFileNameWithoutExtension(path);
        var metadata = new SongMetadata
        {
            MetadataKind = MetadataKind.Fallback,
            Title = fileName ?? path,
            Song = song
        };

        return GetSongMetadata(path, metadata);
    }

    private List<EpisodeMetadata> GetEpisodeMetadata(string fileName, EpisodeMetadata baseMetadata)
    {
        var result = new List<EpisodeMetadata> { baseMetadata };

        try
        {
            const string PATTERN = @"[sS]\d+[\._xX]?[eE]([e\-\d{1,2}]+)";
            const string PATTERN_2 = @"\d+[\._xX]([e\-\d{1,2}]+)";
            MatchCollection matches = Regex.Matches(fileName, PATTERN);
            if (matches.Count == 0)
            {
                matches = Regex.Matches(fileName, PATTERN_2);
            }

            if (matches.Count > 0)
            {
                var episodeNumbers = matches.Bind(
                        m => m.Groups[1].Value
                            .Replace('e', '-')
                            .Split('-')
                            .Bind(ep => int.TryParse(ep, out int num) ? Some(num) : Option<int>.None))
                    .ToList();

                switch (episodeNumbers.Count)
                {
                    case 0:
                        break;
                    case 1:
                        baseMetadata.EpisodeNumber = episodeNumbers.Head();
                        break;
                    default:
                        result.Clear();
                        foreach (int episodeNumber in episodeNumbers)
                        {
                            var metadata = new EpisodeMetadata
                            {
                                MetadataKind = MetadataKind.Fallback,
                                EpisodeNumber = episodeNumber,
                                DateAdded = baseMetadata.DateAdded,
                                DateUpdated = baseMetadata.DateAdded,
                                Title = baseMetadata.Title,
                                Actors = new List<Actor>(),
                                Artwork = new List<Artwork>(),
                                Directors = new List<Director>(),
                                Genres = new List<Genre>(),
                                Guids = new List<MetadataGuid>(),
                                Studios = new List<Studio>(),
                                Tags = new List<Tag>(),
                                Writers = new List<Writer>()
                            };

                            result.Add(metadata);
                        }

                        break;
                }
            }
        }
        catch (Exception ex)
        {
            _client.Notify(ex);
        }

        return result;
    }

    private MovieMetadata GetMovieMetadata(string fileName, MovieMetadata metadata)
    {
        try
        {
            const string PATTERN = @"^(.*?)[.\(](\d{4})[.\)].*\.\w+$";
            Match match = Regex.Match(fileName, PATTERN);
            if (match.Success)
            {
                metadata.Title = match.Groups[1].Value.Trim();
                metadata.Year = int.Parse(match.Groups[2].Value);
                metadata.ReleaseDate = new DateTime(int.Parse(match.Groups[2].Value), 1, 1);
                metadata.DateUpdated = DateTime.UtcNow;
            }
        }
        catch (Exception ex)
        {
            _client.Notify(ex);
        }

        return metadata;
    }

    private Option<MusicVideoMetadata> GetMusicVideoMetadata(string fileName, MusicVideoMetadata metadata)
    {
        try
        {
            const string PATTERN = @"^(.*?) - (.*?).\w+$";
            Match match = Regex.Match(fileName, PATTERN);
            metadata.Title = match.Success
                ? match.Groups[2].Value.Trim()
                : Path.GetFileNameWithoutExtension(fileName);
            metadata.Artists = new List<MusicVideoArtist>();
            metadata.Genres = new List<Genre>();
            metadata.Tags = new List<Tag>();
            metadata.Studios = new List<Studio>();
            metadata.DateUpdated = DateTime.UtcNow;

            return metadata;
        }
        catch (Exception ex)
        {
            _client.Notify(ex);
            return None;
        }
    }

    private Option<OtherVideoMetadata> GetOtherVideoMetadata(string path, OtherVideoMetadata metadata)
    {
        try
        {
            string folder = Path.GetDirectoryName(path);
            if (folder == null)
            {
                return None;
            }

            string libraryPath = metadata.OtherVideo.LibraryPath.Path;
            string parent = Optional(Directory.GetParent(libraryPath)).Match(
                di => di.FullName,
                () => libraryPath);

            string diff = Path.GetRelativePath(parent, folder);

            var tags = diff.Split(Path.DirectorySeparatorChar)
                .Map(t => new Tag { Name = t })
                .ToList();

            metadata.Artwork = new List<Artwork>();
            metadata.Actors = new List<Actor>();
            metadata.Genres = new List<Genre>();
            metadata.Tags = tags;
            metadata.Studios = new List<Studio>();
            metadata.DateUpdated = DateTime.UtcNow;
            metadata.OriginalTitle = Path.GetRelativePath(libraryPath, path);

            return metadata;
        }
        catch (Exception ex)
        {
            _client.Notify(ex);
            return None;
        }
    }

    private Option<SongMetadata> GetSongMetadata(string path, SongMetadata metadata)
    {
        try
        {
            string folder = Path.GetDirectoryName(path);
            if (folder == null)
            {
                return None;
            }

            string libraryPath = metadata.Song.LibraryPath.Path;
            string parent = Optional(Directory.GetParent(libraryPath)).Match(
                di => di.FullName,
                () => libraryPath);

            string diff = Path.GetRelativePath(parent, folder);

            var tags = diff.Split(Path.DirectorySeparatorChar)
                .Map(t => new Tag { Name = t })
                .ToList();

            metadata.Artwork = new List<Artwork>();
            metadata.Actors = new List<Actor>();
            metadata.Genres = new List<Genre>();
            metadata.Tags = tags;
            metadata.Studios = new List<Studio>();
            metadata.DateUpdated = DateTime.UtcNow;
            metadata.OriginalTitle = Path.GetRelativePath(libraryPath, path);

            return metadata;
        }
        catch (Exception ex)
        {
            _client.Notify(ex);
            return None;
        }
    }

    private ShowMetadata GetTelevisionShowMetadata(string fileName, ShowMetadata metadata)
    {
        try
        {
            const string PATTERN = @"^(.*?)[\s.]+?[.\(](\d{4})[.\)].*$";
            Match match = Regex.Match(fileName, PATTERN);
            if (match.Success)
            {
                metadata.Title = match.Groups[1].Value;
                metadata.Year = int.Parse(match.Groups[2].Value);
                metadata.ReleaseDate = new DateTime(int.Parse(match.Groups[2].Value), 1, 1);
                metadata.DateUpdated = DateTime.UtcNow;
            }
        }
        catch (Exception ex)
        {
            _client.Notify(ex);
        }

        return metadata;
    }
}
