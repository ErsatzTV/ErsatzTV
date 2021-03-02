using System;
using System.IO;
using System.Text.RegularExpressions;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.Metadata
{
    public class FallbackMetadataProvider : IFallbackMetadataProvider
    {
        public ShowMetadata GetFallbackMetadataForShow(string showFolder)
        {
            string fileName = Path.GetFileName(showFolder);
            var metadata = new ShowMetadata
                { MetadataKind = MetadataKind.Fallback, Title = fileName ?? showFolder };
            return GetTelevisionShowMetadata(fileName, metadata);
        }

        public Tuple<EpisodeMetadata, int> GetFallbackMetadata(Episode episode)
        {
            string path = episode.MediaVersions.Head().MediaFiles.Head().Path;
            string fileName = Path.GetFileName(path);
            var metadata = new EpisodeMetadata
                { MetadataKind = MetadataKind.Fallback, Title = fileName ?? path };
            return fileName != null ? GetEpisodeMetadata(fileName, metadata) : Tuple(metadata, 0);
        }

        public MovieMetadata GetFallbackMetadata(Movie movie)
        {
            string path = movie.MediaVersions.Head().MediaFiles.Head().Path;
            string fileName = Path.GetFileName(path);
            var metadata = new MovieMetadata { MetadataKind = MetadataKind.Fallback, Title = fileName ?? path };

            return fileName != null ? GetMovieMetadata(fileName, metadata) : metadata;
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

            if (title.StartsWith("Æ"))
            {
                return title.Replace("Æ", "E");
            }

            return title;
        }

        private Tuple<EpisodeMetadata, int> GetEpisodeMetadata(string fileName, EpisodeMetadata metadata)
        {
            try
            {
                const string PATTERN = @"^(.*?)[.\s-]+[sS](\d+)[eE](\d+).*\.\w+$";
                Match match = Regex.Match(fileName, PATTERN);
                if (match.Success)
                {
                    metadata.Title = match.Groups[1].Value;
                    return Tuple(metadata, int.Parse(match.Groups[3].Value));
                }
            }
            catch (Exception)
            {
                // ignored
            }

            return Tuple(metadata, 0);
        }

        private MovieMetadata GetMovieMetadata(string fileName, MovieMetadata metadata)
        {
            try
            {
                const string PATTERN = @"^(.*?)[.\(](\d{4})[.\)].*\.\w+$";
                Match match = Regex.Match(fileName, PATTERN);
                if (match.Success)
                {
                    metadata.Title = match.Groups[1].Value;
                    metadata.Year = int.Parse(match.Groups[2].Value);
                    metadata.ReleaseDate = new DateTime(int.Parse(match.Groups[2].Value), 1, 1);
                }
            }
            catch (Exception)
            {
                // ignored
            }

            return metadata;
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
                }
            }
            catch (Exception)
            {
                // ignored
            }

            return metadata;
        }
    }
}
