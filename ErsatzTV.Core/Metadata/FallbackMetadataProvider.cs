using System.Text.RegularExpressions;
using Bugsnag;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;

namespace ErsatzTV.Core.Metadata;

public class FallbackMetadataProvider : IFallbackMetadataProvider
{
    private readonly IClient _client;

    public FallbackMetadataProvider(IClient client) => _client = client;

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
            Writers = new List<Writer>()
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
            OtherVideo = otherVideo
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

    public string GetSortTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return title;
        }

        if (title.StartsWith("the ", StringComparison.OrdinalIgnoreCase))
        {
            return title.Substring(4);
        }

        if (title.StartsWith("a ", StringComparison.OrdinalIgnoreCase))
        {
            return title.Substring(2);
        }

        if (title.StartsWith("an ", StringComparison.OrdinalIgnoreCase))
        {
            return title.Substring(3);
        }

        if (title.StartsWith("Æ"))
        {
            return title.Replace("Æ", "E");
        }

        return title;
    }

    private List<EpisodeMetadata> GetEpisodeMetadata(string fileName, EpisodeMetadata baseMetadata)
    {
        var result = new List<EpisodeMetadata>();

        try
        {
            const string PATTERN = @"[sS]\d+[eE]([e\-\d{1,2}]+)";
            MatchCollection matches = Regex.Matches(fileName, PATTERN);
            if (matches.Count > 0)
            {
                foreach (Match match in matches)
                {
                    string[] split = match.Groups[1].Value.Replace('e', '-').Split('-');
                    foreach (string ep in split)
                    {
                        if (!int.TryParse(ep, out int episodeNumber))
                        {
                            continue;
                        }

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
                }

                return result;
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
