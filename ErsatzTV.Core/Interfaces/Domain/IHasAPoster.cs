using System;

namespace ErsatzTV.Core.Interfaces.Domain
{
    public interface IHasAPoster
    {
        public string Path { get; set; }
        public string Poster { get; set; }
        public DateTime? PosterLastWriteTime { get; set; }
    }
}
