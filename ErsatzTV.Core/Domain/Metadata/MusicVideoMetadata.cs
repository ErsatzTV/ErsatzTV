using System;

namespace ErsatzTV.Core.Domain
{
    public class MusicVideoMetadata : Metadata
    {
        public string Album { get; set; }

        public string Plot { get; set; }

        // TODO: delete this
        [Obsolete("Need to delete this")]
        public string Artist { get; set; }

        public int MusicVideoId { get; set; }
        public MusicVideo MusicVideo { get; set; }
    }
}
