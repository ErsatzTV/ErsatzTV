﻿using System.Runtime.InteropServices;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Emby;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.FFmpeg.Runtime;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace ErsatzTV.Core.Tests.Emby;

[TestFixture]
public class EmbyPathReplacementServiceTests
{
    [Test]
    public async Task EmbyWindows_To_EtvWindows()
    {
        var replacements = new List<EmbyPathReplacement>
        {
            new()
            {
                Id = 1,
                EmbyPath = @"C:\Something\Some Shared Folder",
                LocalPath = @"C:\Something Else\Some Shared Folder",
                EmbyMediaSource = new EmbyMediaSource { OperatingSystem = "Windows" }
            }
        };

        IMediaSourceRepository repo = Substitute.For<IMediaSourceRepository>();
        repo.GetEmbyPathReplacementsByLibraryId(Arg.Any<int>()).Returns(replacements.AsTask());

        IRuntimeInfo runtime = Substitute.For<IRuntimeInfo>();
        runtime.IsOSPlatform(OSPlatform.Windows).Returns(true);

        var service = new EmbyPathReplacementService(
            repo,
            runtime,
            Substitute.For<ILogger<EmbyPathReplacementService>>());

        string result = await service.GetReplacementEmbyPath(
            0,
            @"C:\Something\Some Shared Folder\Some Movie\Some Movie.mkv");

        result.Should().Be(@"C:\Something Else\Some Shared Folder\Some Movie\Some Movie.mkv");
    }

    [Test]
    public async Task EmbyWindows_To_EtvLinux()
    {
        var replacements = new List<EmbyPathReplacement>
        {
            new()
            {
                Id = 1,
                EmbyPath = @"C:\Something\Some Shared Folder",
                LocalPath = @"/mnt/something else/Some Shared Folder",
                EmbyMediaSource = new EmbyMediaSource { OperatingSystem = "Windows" }
            }
        };

        IMediaSourceRepository repo = Substitute.For<IMediaSourceRepository>();
        repo.GetEmbyPathReplacementsByLibraryId(Arg.Any<int>()).Returns(replacements.AsTask());

        IRuntimeInfo runtime = Substitute.For<IRuntimeInfo>();
        runtime.IsOSPlatform(OSPlatform.Windows).Returns(false);

        var service = new EmbyPathReplacementService(
            repo,
            runtime,
            Substitute.For<ILogger<EmbyPathReplacementService>>());

        string result = await service.GetReplacementEmbyPath(
            0,
            @"C:\Something\Some Shared Folder\Some Movie\Some Movie.mkv");

        result.Should().Be(@"/mnt/something else/Some Shared Folder/Some Movie/Some Movie.mkv");
    }

    [Test]
    public void EmbyWindows_To_EtvLinux_NetworkPath()
    {
        var mediaSource = new EmbyMediaSource { OperatingSystem = "Windows" };

        IMediaSourceRepository repo = Substitute.For<IMediaSourceRepository>();

        IRuntimeInfo runtime = Substitute.For<IRuntimeInfo>();
        runtime.IsOSPlatform(OSPlatform.Windows).Returns(false);

        var service = new EmbyPathReplacementService(
            repo,
            runtime,
            Substitute.For<ILogger<EmbyPathReplacementService>>());

        string result = service.ReplaceNetworkPath(
            mediaSource,
            @"\\192.168.1.100\Something\Some Shared Folder\Some Movie\Some Movie.mkv",
            @"\\192.168.1.100\Something\Some Shared Folder",
            @"C:\mnt\something else\Some Shared Folder");

        result.Should().Be(@"C:\mnt\something else\Some Shared Folder\Some Movie\Some Movie.mkv");
    }

    [Test]
    public async Task EmbyWindows_To_EtvLinux_UncPath()
    {
        var replacements = new List<EmbyPathReplacement>
        {
            new()
            {
                Id = 1,
                EmbyPath = @"\\192.168.1.100\Something\Some Shared Folder",
                LocalPath = @"/mnt/something else/Some Shared Folder",
                EmbyMediaSource = new EmbyMediaSource { OperatingSystem = "Windows" }
            }
        };

        IMediaSourceRepository repo = Substitute.For<IMediaSourceRepository>();
        repo.GetEmbyPathReplacementsByLibraryId(Arg.Any<int>()).Returns(replacements.AsTask());

        IRuntimeInfo runtime = Substitute.For<IRuntimeInfo>();
        runtime.IsOSPlatform(OSPlatform.Windows).Returns(false);

        var service = new EmbyPathReplacementService(
            repo,
            runtime,
            Substitute.For<ILogger<EmbyPathReplacementService>>());

        string result = await service.GetReplacementEmbyPath(
            0,
            @"\\192.168.1.100\Something\Some Shared Folder\Some Movie\Some Movie.mkv");

        result.Should().Be(@"/mnt/something else/Some Shared Folder/Some Movie/Some Movie.mkv");
    }

    [Test]
    public async Task EmbyWindows_To_EtvLinux_UncPathWithTrailingSlash()
    {
        var replacements = new List<EmbyPathReplacement>
        {
            new()
            {
                Id = 1,
                EmbyPath = @"\\192.168.1.100\Something\Some Shared Folder\",
                LocalPath = @"/mnt/something else/Some Shared Folder/",
                EmbyMediaSource = new EmbyMediaSource { OperatingSystem = "Windows" }
            }
        };

        IMediaSourceRepository repo = Substitute.For<IMediaSourceRepository>();
        repo.GetEmbyPathReplacementsByLibraryId(Arg.Any<int>()).Returns(replacements.AsTask());

        IRuntimeInfo runtime = Substitute.For<IRuntimeInfo>();
        runtime.IsOSPlatform(OSPlatform.Windows).Returns(false);

        var service = new EmbyPathReplacementService(
            repo,
            runtime,
            Substitute.For<ILogger<EmbyPathReplacementService>>());

        string result = await service.GetReplacementEmbyPath(
            0,
            @"\\192.168.1.100\Something\Some Shared Folder\Some Movie\Some Movie.mkv");

        result.Should().Be(@"/mnt/something else/Some Shared Folder/Some Movie/Some Movie.mkv");
    }

    [Test]
    public async Task EmbyWindows_To_EtvLinux_UncPathWithMixedCaseServerName()
    {
        var replacements = new List<EmbyPathReplacement>
        {
            new()
            {
                Id = 1,
                EmbyPath = @"\\ServerName\Something\Some Shared Folder\",
                LocalPath = @"/mnt/something else/Some Shared Folder/",
                EmbyMediaSource = new EmbyMediaSource { OperatingSystem = "Windows" }
            }
        };

        IMediaSourceRepository repo = Substitute.For<IMediaSourceRepository>();
        repo.GetEmbyPathReplacementsByLibraryId(Arg.Any<int>()).Returns(replacements.AsTask());

        IRuntimeInfo runtime = Substitute.For<IRuntimeInfo>();
        runtime.IsOSPlatform(OSPlatform.Windows).Returns(false);

        var service = new EmbyPathReplacementService(
            repo,
            runtime,
            Substitute.For<ILogger<EmbyPathReplacementService>>());

        string result = await service.GetReplacementEmbyPath(
            0,
            @"\\SERVERNAME\Something\Some Shared Folder\Some Movie\Some Movie.mkv");

        result.Should().Be(@"/mnt/something else/Some Shared Folder/Some Movie/Some Movie.mkv");
    }

    [Test]
    public async Task EmbyLinux_To_EtvWindows()
    {
        var replacements = new List<EmbyPathReplacement>
        {
            new()
            {
                Id = 1,
                EmbyPath = @"/mnt/something/Some Shared Folder",
                LocalPath = @"C:\Something Else\Some Shared Folder",
                EmbyMediaSource = new EmbyMediaSource { OperatingSystem = "Linux" }
            }
        };

        IMediaSourceRepository repo = Substitute.For<IMediaSourceRepository>();
        repo.GetEmbyPathReplacementsByLibraryId(Arg.Any<int>()).Returns(replacements.AsTask());

        IRuntimeInfo runtime = Substitute.For<IRuntimeInfo>();
        runtime.IsOSPlatform(OSPlatform.Windows).Returns(true);

        var service = new EmbyPathReplacementService(
            repo,
            runtime,
            Substitute.For<ILogger<EmbyPathReplacementService>>());

        string result = await service.GetReplacementEmbyPath(
            0,
            @"/mnt/something/Some Shared Folder/Some Movie/Some Movie.mkv");

        result.Should().Be(@"C:\Something Else\Some Shared Folder\Some Movie\Some Movie.mkv");
    }

    [Test]
    public async Task EmbyLinux_To_EtvLinux()
    {
        var replacements = new List<EmbyPathReplacement>
        {
            new()
            {
                Id = 1,
                EmbyPath = @"/mnt/something/Some Shared Folder",
                LocalPath = @"/mnt/something else/Some Shared Folder",
                EmbyMediaSource = new EmbyMediaSource { OperatingSystem = "Linux" }
            }
        };

        IMediaSourceRepository repo = Substitute.For<IMediaSourceRepository>();
        repo.GetEmbyPathReplacementsByLibraryId(Arg.Any<int>()).Returns(replacements.AsTask());

        IRuntimeInfo runtime = Substitute.For<IRuntimeInfo>();
        runtime.IsOSPlatform(OSPlatform.Windows).Returns(false);

        var service = new EmbyPathReplacementService(
            repo,
            runtime,
            Substitute.For<ILogger<EmbyPathReplacementService>>());

        string result = await service.GetReplacementEmbyPath(
            0,
            @"/mnt/something/Some Shared Folder/Some Movie/Some Movie.mkv");

        result.Should().Be(@"/mnt/something else/Some Shared Folder/Some Movie/Some Movie.mkv");
    }

    [Test]
    public async Task Should_Not_Throw_For_Null_EmbyPath()
    {
        var replacements = new List<EmbyPathReplacement>
        {
            new()
            {
                Id = 1,
                EmbyPath = null,
                LocalPath = @"/mnt/something else/Some Shared Folder",
                EmbyMediaSource = new EmbyMediaSource { OperatingSystem = "Linux" }
            }
        };

        IMediaSourceRepository repo = Substitute.For<IMediaSourceRepository>();
        repo.GetEmbyPathReplacementsByLibraryId(Arg.Any<int>()).Returns(replacements.AsTask());

        IRuntimeInfo runtime = Substitute.For<IRuntimeInfo>();
        runtime.IsOSPlatform(OSPlatform.Windows).Returns(false);

        var service = new EmbyPathReplacementService(
            repo,
            runtime,
            Substitute.For<ILogger<EmbyPathReplacementService>>());

        string result = await service.GetReplacementEmbyPath(
            0,
            @"/mnt/something/Some Shared Folder/Some Movie/Some Movie.mkv");

        result.Should().Be(@"/mnt/something/Some Shared Folder/Some Movie/Some Movie.mkv");
    }

    [Test]
    public async Task Should_Not_Throw_For_Null_LocalPath()
    {
        var replacements = new List<EmbyPathReplacement>
        {
            new()
            {
                Id = 1,
                EmbyPath = @"/mnt/something/Some Shared Folder",
                LocalPath = null,
                EmbyMediaSource = new EmbyMediaSource { OperatingSystem = "Linux" }
            }
        };

        IMediaSourceRepository repo = Substitute.For<IMediaSourceRepository>();
        repo.GetEmbyPathReplacementsByLibraryId(Arg.Any<int>()).Returns(replacements.AsTask());

        IRuntimeInfo runtime = Substitute.For<IRuntimeInfo>();
        runtime.IsOSPlatform(OSPlatform.Windows).Returns(false);

        var service = new EmbyPathReplacementService(
            repo,
            runtime,
            Substitute.For<ILogger<EmbyPathReplacementService>>());

        string result = await service.GetReplacementEmbyPath(
            0,
            @"/mnt/something/Some Shared Folder/Some Movie/Some Movie.mkv");

        result.Should().Be(@"/Some Movie/Some Movie.mkv");
    }
}
