using System.Runtime.InteropServices;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Runtime;
using ErsatzTV.Core.Jellyfin;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ErsatzTV.Core.Tests.Jellyfin;

[TestFixture]
public class JellyfinPathReplacementServiceTests
{
    [Test]
    public async Task JellyfinWindows_To_EtvWindows()
    {
        var replacements = new List<JellyfinPathReplacement>
        {
            new()
            {
                Id = 1,
                JellyfinPath = @"C:\Something\Some Shared Folder",
                LocalPath = @"C:\Something Else\Some Shared Folder",
                JellyfinMediaSource = new JellyfinMediaSource { OperatingSystem = "Windows" }
            }
        };

        var repo = new Mock<IMediaSourceRepository>();
        repo.Setup(x => x.GetJellyfinPathReplacementsByLibraryId(It.IsAny<int>())).Returns(replacements.AsTask());

        var runtime = new Mock<IRuntimeInfo>();
        runtime.Setup(x => x.IsOSPlatform(OSPlatform.Windows)).Returns(true);

        var service = new JellyfinPathReplacementService(
            repo.Object,
            runtime.Object,
            new Mock<ILogger<JellyfinPathReplacementService>>().Object);

        string result = await service.GetReplacementJellyfinPath(
            0,
            @"C:\Something\Some Shared Folder\Some Movie\Some Movie.mkv");

        result.Should().Be(@"C:\Something Else\Some Shared Folder\Some Movie\Some Movie.mkv");
    }

    [Test]
    public async Task JellyfinWindows_To_EtvLinux()
    {
        var replacements = new List<JellyfinPathReplacement>
        {
            new()
            {
                Id = 1,
                JellyfinPath = @"C:\Something\Some Shared Folder",
                LocalPath = @"/mnt/something else/Some Shared Folder",
                JellyfinMediaSource = new JellyfinMediaSource { OperatingSystem = "Windows" }
            }
        };

        var repo = new Mock<IMediaSourceRepository>();
        repo.Setup(x => x.GetJellyfinPathReplacementsByLibraryId(It.IsAny<int>())).Returns(replacements.AsTask());

        var runtime = new Mock<IRuntimeInfo>();
        runtime.Setup(x => x.IsOSPlatform(OSPlatform.Windows)).Returns(false);

        var service = new JellyfinPathReplacementService(
            repo.Object,
            runtime.Object,
            new Mock<ILogger<JellyfinPathReplacementService>>().Object);

        string result = await service.GetReplacementJellyfinPath(
            0,
            @"C:\Something\Some Shared Folder\Some Movie\Some Movie.mkv");

        result.Should().Be(@"/mnt/something else/Some Shared Folder/Some Movie/Some Movie.mkv");
    }

    [Test]
    public async Task JellyfinWindows_To_EtvLinux_UncPath()
    {
        var replacements = new List<JellyfinPathReplacement>
        {
            new()
            {
                Id = 1,
                JellyfinPath = @"\\192.168.1.100\Something\Some Shared Folder",
                LocalPath = @"/mnt/something else/Some Shared Folder",
                JellyfinMediaSource = new JellyfinMediaSource { OperatingSystem = "Windows" }
            }
        };

        var repo = new Mock<IMediaSourceRepository>();
        repo.Setup(x => x.GetJellyfinPathReplacementsByLibraryId(It.IsAny<int>())).Returns(replacements.AsTask());

        var runtime = new Mock<IRuntimeInfo>();
        runtime.Setup(x => x.IsOSPlatform(OSPlatform.Windows)).Returns(false);

        var service = new JellyfinPathReplacementService(
            repo.Object,
            runtime.Object,
            new Mock<ILogger<JellyfinPathReplacementService>>().Object);

        string result = await service.GetReplacementJellyfinPath(
            0,
            @"\\192.168.1.100\Something\Some Shared Folder\Some Movie\Some Movie.mkv");

        result.Should().Be(@"/mnt/something else/Some Shared Folder/Some Movie/Some Movie.mkv");
    }

    [Test]
    public async Task JellyfinWindows_To_EtvLinux_UncPathWithTrailingSlash()
    {
        var replacements = new List<JellyfinPathReplacement>
        {
            new()
            {
                Id = 1,
                JellyfinPath = @"\\192.168.1.100\Something\Some Shared Folder\",
                LocalPath = @"/mnt/something else/Some Shared Folder/",
                JellyfinMediaSource = new JellyfinMediaSource { OperatingSystem = "Windows" }
            }
        };

        var repo = new Mock<IMediaSourceRepository>();
        repo.Setup(x => x.GetJellyfinPathReplacementsByLibraryId(It.IsAny<int>())).Returns(replacements.AsTask());

        var runtime = new Mock<IRuntimeInfo>();
        runtime.Setup(x => x.IsOSPlatform(OSPlatform.Windows)).Returns(false);

        var service = new JellyfinPathReplacementService(
            repo.Object,
            runtime.Object,
            new Mock<ILogger<JellyfinPathReplacementService>>().Object);

        string result = await service.GetReplacementJellyfinPath(
            0,
            @"\\192.168.1.100\Something\Some Shared Folder\Some Movie\Some Movie.mkv");

        result.Should().Be(@"/mnt/something else/Some Shared Folder/Some Movie/Some Movie.mkv");
    }

    [Test]
    public async Task JellyfinLinux_To_EtvWindows()
    {
        var replacements = new List<JellyfinPathReplacement>
        {
            new()
            {
                Id = 1,
                JellyfinPath = @"/mnt/something/Some Shared Folder",
                LocalPath = @"C:\Something Else\Some Shared Folder",
                JellyfinMediaSource = new JellyfinMediaSource { OperatingSystem = "Linux" }
            }
        };

        var repo = new Mock<IMediaSourceRepository>();
        repo.Setup(x => x.GetJellyfinPathReplacementsByLibraryId(It.IsAny<int>())).Returns(replacements.AsTask());

        var runtime = new Mock<IRuntimeInfo>();
        runtime.Setup(x => x.IsOSPlatform(OSPlatform.Windows)).Returns(true);

        var service = new JellyfinPathReplacementService(
            repo.Object,
            runtime.Object,
            new Mock<ILogger<JellyfinPathReplacementService>>().Object);

        string result = await service.GetReplacementJellyfinPath(
            0,
            @"/mnt/something/Some Shared Folder/Some Movie/Some Movie.mkv");

        result.Should().Be(@"C:\Something Else\Some Shared Folder\Some Movie\Some Movie.mkv");
    }

    [Test]
    public async Task JellyfinLinux_To_EtvLinux()
    {
        var replacements = new List<JellyfinPathReplacement>
        {
            new()
            {
                Id = 1,
                JellyfinPath = @"/mnt/something/Some Shared Folder",
                LocalPath = @"/mnt/something else/Some Shared Folder",
                JellyfinMediaSource = new JellyfinMediaSource { OperatingSystem = "Linux" }
            }
        };

        var repo = new Mock<IMediaSourceRepository>();
        repo.Setup(x => x.GetJellyfinPathReplacementsByLibraryId(It.IsAny<int>())).Returns(replacements.AsTask());

        var runtime = new Mock<IRuntimeInfo>();
        runtime.Setup(x => x.IsOSPlatform(OSPlatform.Windows)).Returns(false);

        var service = new JellyfinPathReplacementService(
            repo.Object,
            runtime.Object,
            new Mock<ILogger<JellyfinPathReplacementService>>().Object);

        string result = await service.GetReplacementJellyfinPath(
            0,
            @"/mnt/something/Some Shared Folder/Some Movie/Some Movie.mkv");

        result.Should().Be(@"/mnt/something else/Some Shared Folder/Some Movie/Some Movie.mkv");
    }
}