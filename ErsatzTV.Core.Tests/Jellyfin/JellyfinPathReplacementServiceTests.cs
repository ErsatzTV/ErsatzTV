using System.Runtime.InteropServices;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Jellyfin;
using ErsatzTV.FFmpeg.Runtime;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
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

        IMediaSourceRepository repo = Substitute.For<IMediaSourceRepository>();
        repo.GetJellyfinPathReplacementsByLibraryId(Arg.Any<int>()).Returns(replacements.AsTask());

        IRuntimeInfo runtime = Substitute.For<IRuntimeInfo>();
        runtime.IsOSPlatform(OSPlatform.Windows).Returns(true);

        var service = new JellyfinPathReplacementService(
            repo,
            runtime,
            Substitute.For<ILogger<JellyfinPathReplacementService>>());

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

        IMediaSourceRepository repo = Substitute.For<IMediaSourceRepository>();
        repo.GetJellyfinPathReplacementsByLibraryId(Arg.Any<int>()).Returns(replacements.AsTask());

        IRuntimeInfo runtime = Substitute.For<IRuntimeInfo>();
        runtime.IsOSPlatform(OSPlatform.Windows).Returns(false);

        var service = new JellyfinPathReplacementService(
            repo,
            runtime,
            Substitute.For<ILogger<JellyfinPathReplacementService>>());

        string result = await service.GetReplacementJellyfinPath(
            0,
            @"C:\Something\Some Shared Folder\Some Movie\Some Movie.mkv");

        result.Should().Be(@"/mnt/something else/Some Shared Folder/Some Movie/Some Movie.mkv");
    }

    [Test]
    public void JellyfinWindows_To_EtvLinux_NetworkPath()
    {
        var mediaSource = new JellyfinMediaSource { OperatingSystem = "Windows" };

        IMediaSourceRepository repo = Substitute.For<IMediaSourceRepository>();

        IRuntimeInfo runtime = Substitute.For<IRuntimeInfo>();
        runtime.IsOSPlatform(OSPlatform.Windows).Returns(false);

        var service = new JellyfinPathReplacementService(
            repo,
            runtime,
            Substitute.For<ILogger<JellyfinPathReplacementService>>());

        string result = service.ReplaceNetworkPath(
            mediaSource,
            @"\\192.168.1.100\Something\Some Shared Folder\Some Movie\Some Movie.mkv",
            @"\\192.168.1.100\Something\Some Shared Folder",
            @"C:\mnt\something else\Some Shared Folder");

        result.Should().Be(@"C:\mnt\something else\Some Shared Folder\Some Movie\Some Movie.mkv");
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

        IMediaSourceRepository repo = Substitute.For<IMediaSourceRepository>();
        repo.GetJellyfinPathReplacementsByLibraryId(Arg.Any<int>()).Returns(replacements.AsTask());

        IRuntimeInfo runtime = Substitute.For<IRuntimeInfo>();
        runtime.IsOSPlatform(OSPlatform.Windows).Returns(false);

        var service = new JellyfinPathReplacementService(
            repo,
            runtime,
            Substitute.For<ILogger<JellyfinPathReplacementService>>());

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

        IMediaSourceRepository repo = Substitute.For<IMediaSourceRepository>();
        repo.GetJellyfinPathReplacementsByLibraryId(Arg.Any<int>()).Returns(replacements.AsTask());

        IRuntimeInfo runtime = Substitute.For<IRuntimeInfo>();
        runtime.IsOSPlatform(OSPlatform.Windows).Returns(false);

        var service = new JellyfinPathReplacementService(
            repo,
            runtime,
            Substitute.For<ILogger<JellyfinPathReplacementService>>());

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

        IMediaSourceRepository repo = Substitute.For<IMediaSourceRepository>();
        repo.GetJellyfinPathReplacementsByLibraryId(Arg.Any<int>()).Returns(replacements.AsTask());

        IRuntimeInfo runtime = Substitute.For<IRuntimeInfo>();
        runtime.IsOSPlatform(OSPlatform.Windows).Returns(true);

        var service = new JellyfinPathReplacementService(
            repo,
            runtime,
            Substitute.For<ILogger<JellyfinPathReplacementService>>());

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

        IMediaSourceRepository repo = Substitute.For<IMediaSourceRepository>();
        repo.GetJellyfinPathReplacementsByLibraryId(Arg.Any<int>()).Returns(replacements.AsTask());

        IRuntimeInfo runtime = Substitute.For<IRuntimeInfo>();
        runtime.IsOSPlatform(OSPlatform.Windows).Returns(false);

        var service = new JellyfinPathReplacementService(
            repo,
            runtime,
            Substitute.For<ILogger<JellyfinPathReplacementService>>());

        string result = await service.GetReplacementJellyfinPath(
            0,
            @"/mnt/something/Some Shared Folder/Some Movie/Some Movie.mkv");

        result.Should().Be(@"/mnt/something else/Some Shared Folder/Some Movie/Some Movie.mkv");
    }

    [Test]
    public async Task JellyfinLinux_To_EtvLinux_UncPath()
    {
        var replacements = new List<JellyfinPathReplacement>
        {
            new()
            {
                Id = 1,
                JellyfinPath = @"\\192.168.1.100\Something\Some Shared Folder",
                LocalPath = @"/mnt/something else/Some Shared Folder",
                JellyfinMediaSource = new JellyfinMediaSource { OperatingSystem = "Linux" }
            }
        };

        IMediaSourceRepository repo = Substitute.For<IMediaSourceRepository>();
        repo.GetJellyfinPathReplacementsByLibraryId(Arg.Any<int>()).Returns(replacements.AsTask());

        IRuntimeInfo runtime = Substitute.For<IRuntimeInfo>();
        runtime.IsOSPlatform(OSPlatform.Windows).Returns(false);

        var service = new JellyfinPathReplacementService(
            repo,
            runtime,
            Substitute.For<ILogger<JellyfinPathReplacementService>>());

        string result = await service.GetReplacementJellyfinPath(
            0,
            @"\\192.168.1.100\Something\Some Shared Folder\Some Movie\Some Movie.mkv");

        result.Should().Be(@"/mnt/something else/Some Shared Folder/Some Movie/Some Movie.mkv");
    }

    [Test]
    public async Task Should_Not_Throw_For_Null_JellyfinPath()
    {
        var replacements = new List<JellyfinPathReplacement>
        {
            new()
            {
                Id = 1,
                JellyfinPath = null,
                LocalPath = @"/mnt/something else/Some Shared Folder",
                JellyfinMediaSource = new JellyfinMediaSource { OperatingSystem = "Linux" }
            }
        };

        IMediaSourceRepository repo = Substitute.For<IMediaSourceRepository>();
        repo.GetJellyfinPathReplacementsByLibraryId(Arg.Any<int>()).Returns(replacements.AsTask());

        IRuntimeInfo runtime = Substitute.For<IRuntimeInfo>();
        runtime.IsOSPlatform(OSPlatform.Windows).Returns(false);

        var service = new JellyfinPathReplacementService(
            repo,
            runtime,
            Substitute.For<ILogger<JellyfinPathReplacementService>>());

        string result = await service.GetReplacementJellyfinPath(
            0,
            @"/mnt/something/Some Shared Folder/Some Movie/Some Movie.mkv");

        result.Should().Be(@"/mnt/something/Some Shared Folder/Some Movie/Some Movie.mkv");
    }

    [Test]
    public async Task Should_Not_Throw_For_Null_LocalPath()
    {
        var replacements = new List<JellyfinPathReplacement>
        {
            new()
            {
                Id = 1,
                JellyfinPath = @"/mnt/something/Some Shared Folder",
                LocalPath = null,
                JellyfinMediaSource = new JellyfinMediaSource { OperatingSystem = "Linux" }
            }
        };

        IMediaSourceRepository repo = Substitute.For<IMediaSourceRepository>();
        repo.GetJellyfinPathReplacementsByLibraryId(Arg.Any<int>()).Returns(replacements.AsTask());

        IRuntimeInfo runtime = Substitute.For<IRuntimeInfo>();
        runtime.IsOSPlatform(OSPlatform.Windows).Returns(false);

        var service = new JellyfinPathReplacementService(
            repo,
            runtime,
            Substitute.For<ILogger<JellyfinPathReplacementService>>());

        string result = await service.GetReplacementJellyfinPath(
            0,
            @"/mnt/something/Some Shared Folder/Some Movie/Some Movie.mkv");

        result.Should().Be(@"/Some Movie/Some Movie.mkv");
    }
}
