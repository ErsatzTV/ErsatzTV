using ErsatzTV.Core;

namespace ErsatzTV.Scanner.Tests.Core.Fakes;

public record FakeFileEntry(string Path)
{
    public DateTime LastWriteTime { get; set; } = SystemTime.MinValueUtc;
}
