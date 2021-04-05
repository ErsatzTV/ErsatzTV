using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Runtime;
using ErsatzTV.Core.Plex;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ErsatzTV.Core.Tests.Plex
{
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

            var repo = new Mock<IMediaSourceRepository>();
            repo.Setup(x => x.GetPlexPathReplacementsByLibraryId(It.IsAny<int>())).Returns(replacements.AsTask());

            var runtime = new Mock<IRuntimeInfo>();
            runtime.Setup(x => x.IsOSPlatform(OSPlatform.Windows)).Returns(true);

            var service = new PlexPathReplacementService(
                repo.Object,
                runtime.Object,
                new Mock<ILogger<PlexPathReplacementService>>().Object);

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

            var repo = new Mock<IMediaSourceRepository>();
            repo.Setup(x => x.GetPlexPathReplacementsByLibraryId(It.IsAny<int>())).Returns(replacements.AsTask());

            var runtime = new Mock<IRuntimeInfo>();
            runtime.Setup(x => x.IsOSPlatform(OSPlatform.Windows)).Returns(false);

            var service = new PlexPathReplacementService(
                repo.Object,
                runtime.Object,
                new Mock<ILogger<PlexPathReplacementService>>().Object);

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

            var repo = new Mock<IMediaSourceRepository>();
            repo.Setup(x => x.GetPlexPathReplacementsByLibraryId(It.IsAny<int>())).Returns(replacements.AsTask());

            var runtime = new Mock<IRuntimeInfo>();
            runtime.Setup(x => x.IsOSPlatform(OSPlatform.Windows)).Returns(false);

            var service = new PlexPathReplacementService(
                repo.Object,
                runtime.Object,
                new Mock<ILogger<PlexPathReplacementService>>().Object);

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

            var repo = new Mock<IMediaSourceRepository>();
            repo.Setup(x => x.GetPlexPathReplacementsByLibraryId(It.IsAny<int>())).Returns(replacements.AsTask());

            var runtime = new Mock<IRuntimeInfo>();
            runtime.Setup(x => x.IsOSPlatform(OSPlatform.Windows)).Returns(false);

            var service = new PlexPathReplacementService(
                repo.Object,
                runtime.Object,
                new Mock<ILogger<PlexPathReplacementService>>().Object);

            string result = await service.GetReplacementPlexPath(
                0,
                @"\\192.168.1.100\Something\Some Shared Folder\Some Movie\Some Movie.mkv");

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

            var repo = new Mock<IMediaSourceRepository>();
            repo.Setup(x => x.GetPlexPathReplacementsByLibraryId(It.IsAny<int>())).Returns(replacements.AsTask());

            var runtime = new Mock<IRuntimeInfo>();
            runtime.Setup(x => x.IsOSPlatform(OSPlatform.Windows)).Returns(true);

            var service = new PlexPathReplacementService(
                repo.Object,
                runtime.Object,
                new Mock<ILogger<PlexPathReplacementService>>().Object);

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

            var repo = new Mock<IMediaSourceRepository>();
            repo.Setup(x => x.GetPlexPathReplacementsByLibraryId(It.IsAny<int>())).Returns(replacements.AsTask());

            var runtime = new Mock<IRuntimeInfo>();
            runtime.Setup(x => x.IsOSPlatform(OSPlatform.Windows)).Returns(false);

            var service = new PlexPathReplacementService(
                repo.Object,
                runtime.Object,
                new Mock<ILogger<PlexPathReplacementService>>().Object);

            string result = await service.GetReplacementPlexPath(
                0,
                @"/mnt/something/Some Shared Folder/Some Movie/Some Movie.mkv");

            result.Should().Be(@"/mnt/something else/Some Shared Folder/Some Movie/Some Movie.mkv");
        }
    }
}
