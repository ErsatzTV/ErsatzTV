using System;
using System.IO;
using System.Text.RegularExpressions;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;

namespace ErsatzTV.Core.Metadata
{
    public class FallbackMetadataProvider : IFallbackMetadataProvider
    {
        public TelevisionShowMetadata GetFallbackMetadataForShow(string showFolder)
        {
            string fileName = Path.GetFileName(showFolder);
            var metadata = new TelevisionShowMetadata
                { Source = MetadataSource.Fallback, Title = fileName ?? showFolder };
            return GetTelevisionShowMetadata(fileName, metadata);
        }

        public static TelevisionEpisodeMetadata GetFallbackMetadata(TelevisionEpisodeMediaItem mediaItem)
        {
            string fileName = Path.GetFileName(mediaItem.Path);
            var metadata = new TelevisionEpisodeMetadata
                { Source = MetadataSource.Fallback, Title = fileName ?? mediaItem.Path };

            if (fileName != null)
            {
                // TODO: ensure local?
                // if (!(mediaItem.LibraryPath is LocalMediaSource))
                // {
                //     return metadata;
                // }

                return GetEpisodeMetadata(fileName, metadata);
            }

            return metadata;
        }

        public static MovieMetadata GetFallbackMetadata(Movie movie)
        {
            string fileName = Path.GetFileName(movie.Path);
            var metadata = new MovieMetadata { MetadataKind = MetadataKind.Fallback, Title = fileName ?? movie.Path };

            if (fileName != null)
            {
                // TODO: ensure local?
                // if (!(mediaItem.Source is LocalMediaSource))
                // {
                //     return metadata;
                // }

                return GetMovieMetadata(fileName, metadata);
            }

            return metadata;
        }

        private static TelevisionEpisodeMetadata GetEpisodeMetadata(string fileName, TelevisionEpisodeMetadata metadata)
        {
            try
            {
                const string PATTERN = @"^(.*?)[.\s-]+[sS](\d+)[eE](\d+).*\.\w+$";
                Match match = Regex.Match(fileName, PATTERN);
                if (match.Success)
                {
                    metadata.Title = match.Groups[1].Value;
                    metadata.Season = int.Parse(match.Groups[2].Value);
                    metadata.Episode = int.Parse(match.Groups[3].Value);
                }
            }
            catch (Exception)
            {
                // ignored
            }

            return metadata;
        }

        private static MovieMetadata GetMovieMetadata(string fileName, MovieMetadata metadata)
        {
            try
            {
                const string PATTERN = @"^(.*?)[.\(](\d{4})[.\)].*\.\w+$";
                Match match = Regex.Match(fileName, PATTERN);
                if (match.Success)
                {
                    metadata.Title = match.Groups[1].Value;
                    metadata.ReleaseDate = new DateTime(int.Parse(match.Groups[2].Value), 1, 1);
                }
            }
            catch (Exception)
            {
                // ignored
            }

            return metadata;
        }

        private static TelevisionShowMetadata GetTelevisionShowMetadata(
            string fileName,
            TelevisionShowMetadata metadata)
        {
            try
            {
                const string PATTERN = @"^(.*?)[\s.]+?[.\(](\d{4})[.\)].*$";
                Match match = Regex.Match(fileName, PATTERN);
                if (match.Success)
                {
                    metadata.Title = match.Groups[1].Value;
                    metadata.Year = int.Parse(match.Groups[2].Value);
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
