﻿using System.Runtime.InteropServices;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Plex;
using ErsatzTV.FFmpeg.Runtime;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace ErsatzTV.Core.Tests.Plex;

[TestFixture]
public class PlexPathReplacementServiceTests
{
    [Test]
    public async Task PlexWindows_To_EtvWindows()
    {
        var replacements = new List<PlexPathReplacement>
        {
            new()
            {
                Id = 1,
                PlexPath = @"C:\Something\Some Shared Folder",
                LocalPath = @"C:\Something Else\Some Shared Folder",
                PlexMediaSource = new PlexMediaSource { Platform = "Windows" }
            }
        };

        IMediaSourceRepository repo = Substitute.For<IMediaSourceRepository>();
        repo.GetPlexPathReplacementsByLibraryId(Arg.Any<int>()).Returns(replacements.AsTask());

        IRuntimeInfo runtime = Substitute.For<IRuntimeInfo>();
        runtime.IsOSPlatform(OSPlatform.Windows).Returns(true);

        var service = new PlexPathReplacementService(
            repo,
            runtime,
            Substitute.For<ILogger<PlexPathReplacementService>>());

        string result = await service.GetReplacementPlexPath(
            0,
            @"C:\Something\Some Shared Folder\Some Movie\Some Movie.mkv");

        result.Should().Be(@"C:\Something Else\Some Shared Folder\Some Movie\Some Movie.mkv");
    }

    [Test]
    public async Task PlexWindows_To_EtvLinux()
    {
        var replacements = new List<PlexPathReplacement>
        {
            new()
            {
                Id = 1,
                PlexPath = @"C:\Something\Some Shared Folder",
                LocalPath = @"/mnt/something else/Some Shared Folder",
                PlexMediaSource = new PlexMediaSource { Platform = "Windows" }
            }
        };

        IMediaSourceRepository repo = Substitute.For<IMediaSourceRepository>();
        repo.GetPlexPathReplacementsByLibraryId(Arg.Any<int>()).Returns(replacements.AsTask());

        IRuntimeInfo runtime = Substitute.For<IRuntimeInfo>();
        runtime.IsOSPlatform(OSPlatform.Windows).Returns(false);

        var service = new PlexPathReplacementService(
            repo,
            runtime,
            Substitute.For<ILogger<PlexPathReplacementService>>());

        string result = await service.GetReplacementPlexPath(
            0,
            @"C:\Something\Some Shared Folder\Some Movie\Some Movie.mkv");

        result.Should().Be(@"/mnt/something else/Some Shared Folder/Some Movie/Some Movie.mkv");
    }

    [Test]
    public async Task PlexWindows_To_EtvLinux_UncPath()
    {
        var replacements = new List<PlexPathReplacement>
        {
            new()
            {
                Id = 1,
                PlexPath = @"\\192.168.1.100\Something\Some Shared Folder",
                LocalPath = @"/mnt/something else/Some Shared Folder",
                PlexMediaSource = new PlexMediaSource { Platform = "Windows" }
            }
        };

        IMediaSourceRepository repo = Substitute.For<IMediaSourceRepository>();
        repo.GetPlexPathReplacementsByLibraryId(Arg.Any<int>()).Returns(replacements.AsTask());

        IRuntimeInfo runtime = Substitute.For<IRuntimeInfo>();
        runtime.IsOSPlatform(OSPlatform.Windows).Returns(false);

        var service = new PlexPathReplacementService(
            repo,
            runtime,
            Substitute.For<ILogger<PlexPathReplacementService>>());

        string result = await service.GetReplacementPlexPath(
            0,
            @"\\192.168.1.100\Something\Some Shared Folder\Some Movie\Some Movie.mkv");

        result.Should().Be(@"/mnt/something else/Some Shared Folder/Some Movie/Some Movie.mkv");
    }

    [Test]
    public async Task PlexWindows_To_EtvLinux_UncPathWithTrailingSlash()
    {
        var replacements = new List<PlexPathReplacement>
        {
            new()
            {
                Id = 1,
                PlexPath = @"\\192.168.1.100\Something\Some Shared Folder\",
                LocalPath = @"/mnt/something else/Some Shared Folder/",
                PlexMediaSource = new PlexMediaSource { Platform = "Windows" }
            }
        };

        IMediaSourceRepository repo = Substitute.For<IMediaSourceRepository>();
        repo.GetPlexPathReplacementsByLibraryId(Arg.Any<int>()).Returns(replacements.AsTask());

        IRuntimeInfo runtime = Substitute.For<IRuntimeInfo>();
        runtime.IsOSPlatform(OSPlatform.Windows).Returns(false);

        var service = new PlexPathReplacementService(
            repo,
            runtime,
            Substitute.For<ILogger<PlexPathReplacementService>>());

        string result = await service.GetReplacementPlexPath(
            0,
            @"\\192.168.1.100\Something\Some Shared Folder\Some Movie\Some Movie.mkv");

        result.Should().Be(@"/mnt/something else/Some Shared Folder/Some Movie/Some Movie.mkv");
    }

    [Test]
    public async Task PlexWindows_To_EtvLinux_UncPathWithMixedCaseServerName()
    {
        var replacements = new List<PlexPathReplacement>
        {
            new()
            {
                Id = 1,
                PlexPath = @"\\ServerName\Something\Some Shared Folder\",
                LocalPath = @"/mnt/something else/Some Shared Folder/",
                PlexMediaSource = new PlexMediaSource { Platform = "Windows" }
            }
        };

        IMediaSourceRepository repo = Substitute.For<IMediaSourceRepository>();
        repo.GetPlexPathReplacementsByLibraryId(Arg.Any<int>()).Returns(replacements.AsTask());

        IRuntimeInfo runtime = Substitute.For<IRuntimeInfo>();
        runtime.IsOSPlatform(OSPlatform.Windows).Returns(false);

        var service = new PlexPathReplacementService(
            repo,
            runtime,
            Substitute.For<ILogger<PlexPathReplacementService>>());

        string result = await service.GetReplacementPlexPath(
            0,
            @"\\SERVERNAME\Something\Some Shared Folder\Some Movie\Some Movie.mkv");

        result.Should().Be(@"/mnt/something else/Some Shared Folder/Some Movie/Some Movie.mkv");
    }

    [Test]
    public async Task PlexLinux_To_EtvWindows()
    {
        var replacements = new List<PlexPathReplacement>
        {
            new()
            {
                Id = 1,
                PlexPath = @"/mnt/something/Some Shared Folder",
                LocalPath = @"C:\Something Else\Some Shared Folder",
                PlexMediaSource = new PlexMediaSource { Platform = "Linux" }
            }
        };

        IMediaSourceRepository repo = Substitute.For<IMediaSourceRepository>();
        repo.GetPlexPathReplacementsByLibraryId(Arg.Any<int>()).Returns(replacements.AsTask());

        IRuntimeInfo runtime = Substitute.For<IRuntimeInfo>();
        runtime.IsOSPlatform(OSPlatform.Windows).Returns(true);

        var service = new PlexPathReplacementService(
            repo,
            runtime,
            Substitute.For<ILogger<PlexPathReplacementService>>());

        string result = await service.GetReplacementPlexPath(
            0,
            @"/mnt/something/Some Shared Folder/Some Movie/Some Movie.mkv");

        result.Should().Be(@"C:\Something Else\Some Shared Folder\Some Movie\Some Movie.mkv");
    }

    [Test]
    public async Task PlexLinux_To_EtvLinux()
    {
        var replacements = new List<PlexPathReplacement>
        {
            new()
            {
                Id = 1,
                PlexPath = @"/mnt/something/Some Shared Folder",
                LocalPath = @"/mnt/something else/Some Shared Folder",
                PlexMediaSource = new PlexMediaSource { Platform = "Linux" }
            }
        };

        IMediaSourceRepository repo = Substitute.For<IMediaSourceRepository>();
        repo.GetPlexPathReplacementsByLibraryId(Arg.Any<int>()).Returns(replacements.AsTask());

        IRuntimeInfo runtime = Substitute.For<IRuntimeInfo>();
        runtime.IsOSPlatform(OSPlatform.Windows).Returns(false);

        var service = new PlexPathReplacementService(
            repo,
            runtime,
            Substitute.For<ILogger<PlexPathReplacementService>>());

        string result = await service.GetReplacementPlexPath(
            0,
            @"/mnt/something/Some Shared Folder/Some Movie/Some Movie.mkv");

        result.Should().Be(@"/mnt/something else/Some Shared Folder/Some Movie/Some Movie.mkv");
    }

    [Test]
    public async Task Should_Not_Throw_For_Null_PlexPath()
    {
        var replacements = new List<PlexPathReplacement>
        {
            new()
            {
                Id = 1,
                PlexPath = null,
                LocalPath = @"/mnt/something else/Some Shared Folder",
                PlexMediaSource = new PlexMediaSource { Platform = "Linux" }
            }
        };

        IMediaSourceRepository repo = Substitute.For<IMediaSourceRepository>();
        repo.GetPlexPathReplacementsByLibraryId(Arg.Any<int>()).Returns(replacements.AsTask());

        IRuntimeInfo runtime = Substitute.For<IRuntimeInfo>();
        runtime.IsOSPlatform(OSPlatform.Windows).Returns(false);

        var service = new PlexPathReplacementService(
            repo,
            runtime,
            Substitute.For<ILogger<PlexPathReplacementService>>());

        string result = await service.GetReplacementPlexPath(
            0,
            @"/mnt/something/Some Shared Folder/Some Movie/Some Movie.mkv");

        result.Should().Be(@"/mnt/something/Some Shared Folder/Some Movie/Some Movie.mkv");
    }

    [Test]
    public async Task Should_Not_Throw_For_Null_LocalPath()
    {
        var replacements = new List<PlexPathReplacement>
        {
            new()
            {
                Id = 1,
                PlexPath = @"/mnt/something/Some Shared Folder",
                LocalPath = null,
                PlexMediaSource = new PlexMediaSource { Platform = "Linux" }
            }
        };

        IMediaSourceRepository repo = Substitute.For<IMediaSourceRepository>();
        repo.GetPlexPathReplacementsByLibraryId(Arg.Any<int>()).Returns(replacements.AsTask());

        IRuntimeInfo runtime = Substitute.For<IRuntimeInfo>();
        runtime.IsOSPlatform(OSPlatform.Windows).Returns(false);

        var service = new PlexPathReplacementService(
            repo,
            runtime,
            Substitute.For<ILogger<PlexPathReplacementService>>());

        string result = await service.GetReplacementPlexPath(
            0,
            @"/mnt/something/Some Shared Folder/Some Movie/Some Movie.mkv");

        result.Should().Be(@"/Some Movie/Some Movie.mkv");
    }
}
