using System;

namespace ErsatzTV.Core.Tests.Fakes
{
    public record FakeFileEntry(string Path)
    {
        public DateTime LastWriteTime { get; set; } = SystemTime.MinValueUtc;
    }
}
