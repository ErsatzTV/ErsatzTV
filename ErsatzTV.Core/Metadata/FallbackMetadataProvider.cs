using System;
using System.IO;
using System.Text.RegularExpressions;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Metadata
{
    public static class FallbackMetadataProvider
    {
        public static MediaMetadata GetFallbackMetadata(MediaItem mediaItem)
        {
            string fileName = Path.GetFileName(mediaItem.Path);
            var metadata = new MediaMetadata { Source = MetadataSource.Fallback, Title = fileName ?? mediaItem.Path };

            if (fileName != null)
            {
                if (!(mediaItem.Source is LocalMediaSource localMediaSource))
                {
                    return metadata;
                }

                return localMediaSource.MediaType switch
                {
                    MediaType.TvShow => GetTvShowMetadata(fileName, metadata),
                    MediaType.Movie => GetMovieMetadata(fileName, metadata),
                    _ => metadata
                };
            }

            return metadata;
        }

        private static MediaMetadata GetTvShowMetadata(string fileName, MediaMetadata metadata)
        {
            try
            {
                const string PATTERN = @"^(.*?)[.\s-]+[sS](\d+)[eE](\d+).*\.\w+$";
                Match match = Regex.Match(fileName, PATTERN);
                if (match.Success)
                {
                    metadata.MediaType = MediaType.TvShow;
                    metadata.Title = match.Groups[1].Value;
                    metadata.SeasonNumber = int.Parse(match.Groups[2].Value);
                    metadata.EpisodeNumber = int.Parse(match.Groups[3].Value);
                }
            }
            catch (Exception)
            {
                // ignored
            }

            return metadata;
        }

        private static MediaMetadata GetMovieMetadata(string fileName, MediaMetadata metadata)
        {
            try
            {
                const string PATTERN = @"^(.*?)[.\(](\d{4})[.\)].*\.\w+$";
                Match match = Regex.Match(fileName, PATTERN);
                if (match.Success)
                {
                    metadata.MediaType = MediaType.Movie;
                    metadata.Title = match.Groups[1].Value;
                    metadata.Aired = new DateTime(int.Parse(match.Groups[2].Value), 1, 1);
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
