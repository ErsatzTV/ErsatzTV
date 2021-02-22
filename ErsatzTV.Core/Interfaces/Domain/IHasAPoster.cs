using System;

namespace ErsatzTV.Core.Interfaces.Domain
{
    public interface IHasAPoster
    {
        string Path { get; set; }
        string Poster { get; set; }
        DateTime? PosterLastWriteTime { get; set; }
    }
}
