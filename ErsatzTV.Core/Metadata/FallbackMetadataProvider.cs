using System.IO;
using System.Text.RegularExpressions;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Metadata
{
    public static class FallbackMetadataProvider
    {
        public static MediaMetadata GetFallbackMetadata(string path)
        {
            string fileName = Path.GetFileName(path);
            var metadata = new MediaMetadata { Title = fileName ?? path };

            if (fileName != null)
            {
                const string PATTERN = @"^(.*?)[\s-]+[sS](\d+)[eE](\d+).*\.\w+$";
                Match match = Regex.Match(fileName, PATTERN);
                if (match.Success)
                {
                    metadata.MediaType = MediaType.TvShow;
                    metadata.Title = match.Groups[1].Value;
                    metadata.SeasonNumber = int.Parse(match.Groups[2].Value);
                    metadata.EpisodeNumber = int.Parse(match.Groups[3].Value);
                }
            }

            return metadata;
        }
    }
}
