using System;
using System.IO;
using System.Text.RegularExpressions;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Metadata
{
    public static class FallbackMetadataProvider
    {
        public static TelevisionShowMetadata GetFallbackMetadata(TelevisionShow televisionShow)
        {
            string fileName = Path.GetFileName(televisionShow.Path);
            var metadata = new TelevisionShowMetadata
                { Source = MetadataSource.Fallback, Title = fileName ?? televisionShow.Path };
            return GetTelevisionShowMetadata(fileName, metadata);
        }

        public static TelevisionEpisodeMetadata GetFallbackMetadata(TelevisionEpisodeMediaItem mediaItem)
        {
            string fileName = Path.GetFileName(mediaItem.Path);
            var metadata = new TelevisionEpisodeMetadata
                { Source = MetadataSource.Fallback, Title = fileName ?? mediaItem.Path };

            if (fileName != null)
            {
                if (!(mediaItem.Source is LocalMediaSource))
                {
                    return metadata;
                }

                return GetEpisodeMetadata(fileName, metadata);
            }

            return metadata;
        }

        public static MovieMetadata GetFallbackMetadata(MovieMediaItem mediaItem)
        {
            string fileName = Path.GetFileName(mediaItem.Path);
            var metadata = new MovieMetadata { Source = MetadataSource.Fallback, Title = fileName ?? mediaItem.Path };

            if (fileName != null)
            {
                if (!(mediaItem.Source is LocalMediaSource))
                {
                    return metadata;
                }

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
                    metadata.Year = int.Parse(match.Groups[2].Value);
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
