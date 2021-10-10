using System;

namespace ErsatzTV.Core.Interfaces.FFmpeg
{
    public interface IHlsSessionWorker
    {
        DateTimeOffset PlaylistStart { get; }
        void Touch();
    }
}
