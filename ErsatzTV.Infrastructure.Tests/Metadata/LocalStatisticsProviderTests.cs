﻿using Bugsnag;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Infrastructure.Metadata;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Shouldly;

namespace ErsatzTV.Infrastructure.Tests.Metadata;

[TestFixture]
public class LocalStatisticsProviderTests
{
    [Test]
    // this needs to be a culture where '.' is a group separator
    [SetCulture("it-IT")]
    public void Test()
    {
        var provider = new LocalStatisticsProvider(
            Substitute.For<IMetadataRepository>(),
            Substitute.For<ILocalFileSystem>(),
            Substitute.For<IClient>(),
            Substitute.For<ILogger<LocalStatisticsProvider>>());

        var input = new LocalStatisticsProvider.FFprobe(
            new LocalStatisticsProvider.FFprobeFormat(
                "123.45",
                new LocalStatisticsProvider.FFprobeTags(
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty)),
            [],
            []);

        MediaVersion result = provider.ProjectToMediaVersion("test", input);

        result.Duration.ShouldBe(TimeSpan.FromSeconds(123.45));
    }
}
