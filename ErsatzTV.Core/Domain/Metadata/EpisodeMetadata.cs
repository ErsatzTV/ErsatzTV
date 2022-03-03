using System.Collections.Generic;

namespace ErsatzTV.Core.Domain;

public class EpisodeMetadata : Metadata
{
    public int EpisodeNumber { get; set; }
    public string Outline { get; set; }
    public string Plot { get; set; }
    public string Tagline { get; set; }
    public int EpisodeId { get; set; }
    public Episode Episode { get; set; }
    public List<Director> Directors { get; set; }
    public List<Writer> Writers { get; set; }
}