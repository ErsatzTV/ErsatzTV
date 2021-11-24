using System;
using System.Collections.Generic;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Metadata;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ErsatzTV.Core.Tests.Metadata
{
    [TestFixture]
    public class LocalStatisticsProviderTests
    {
        [Test]
        // this needs to be a culture where '.' is a group separator
        [SetCulture("it-IT")] 
        public void Test()
        {
            var provider = new LocalStatisticsProvider(
                new Mock<IMetadataRepository>().Object,
                new Mock<ILocalFileSystem>().Object,
                new Mock<ILogger<LocalStatisticsProvider>>().Object);

            var input = new LocalStatisticsProvider.FFprobe(
                new LocalStatisticsProvider.FFprobeFormat("123.45", null),
                new List<LocalStatisticsProvider.FFprobeStream>(),
                new List<LocalStatisticsProvider.FFprobeChapter>());

            MediaVersion result = provider.ProjectToMediaVersion("test", input);

            result.Duration.Should().Be(TimeSpan.FromSeconds(123.45));
        }
    }
}
