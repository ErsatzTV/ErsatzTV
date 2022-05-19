using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Jellyfin;
using FluentAssertions;
using Flurl;
using NUnit.Framework;

namespace ErsatzTV.Core.Tests.Jellyfin;

public class JellyfinUrlTests
{
    [Test]
    public void Should_Work_Without_Trailing_Slash()
    {
        var artwork = "jellyfin://Items/2/Images/3?tag=4";
        var address = "https://some.jellyfin.server";
        var mediaSource = new JellyfinMediaSource
        {
            Connections = new List<JellyfinConnection>
            {
                new() { Address = address }
            }
        };

        Url url = JellyfinUrl.ForArtwork(Some(mediaSource), artwork);

        url.ToString().Should().Be("https://some.jellyfin.server/Items/2/Images/3?tag=4");
    }

    [Test]
    public void Should_Work_With_Trailing_Slash()
    {
        var artwork = "jellyfin://Items/2/Images/3?tag=4";
        var address = "https://some.jellyfin.server/";
        var mediaSource = new JellyfinMediaSource
        {
            Connections = new List<JellyfinConnection>
            {
                new() { Address = address }
            }
        };

        Url url = JellyfinUrl.ForArtwork(Some(mediaSource), artwork);

        url.ToString().Should().Be("https://some.jellyfin.server/Items/2/Images/3?tag=4");
    }

    [Test]
    public void Should_Work_With_Port_Without_Trailing_Slash()
    {
        var artwork = "jellyfin://Items/2/Images/3?tag=4";
        var address = "https://some.jellyfin.server:1000";
        var mediaSource = new JellyfinMediaSource
        {
            Connections = new List<JellyfinConnection>
            {
                new() { Address = address }
            }
        };

        Url url = JellyfinUrl.ForArtwork(Some(mediaSource), artwork);

        url.ToString().Should().Be("https://some.jellyfin.server:1000/Items/2/Images/3?tag=4");
    }

    [Test]
    public void Should_Work_With_Port_With_Trailing_Slash()
    {
        var artwork = "jellyfin://Items/2/Images/3?tag=4";
        var address = "https://some.jellyfin.server:1000/";
        var mediaSource = new JellyfinMediaSource
        {
            Connections = new List<JellyfinConnection>
            {
                new() { Address = address }
            }
        };

        Url url = JellyfinUrl.ForArtwork(Some(mediaSource), artwork);

        url.ToString().Should().Be("https://some.jellyfin.server:1000/Items/2/Images/3?tag=4");
    }

    [Test]
    public void Should_Work_With_Path_Prefix_Without_Trailing_Slash()
    {
        var artwork = "jellyfin://Items/2/Images/3?tag=4";
        var address = "https://some.jellyfin.server/jellyfin";
        var mediaSource = new JellyfinMediaSource
        {
            Connections = new List<JellyfinConnection>
            {
                new() { Address = address }
            }
        };

        Url url = JellyfinUrl.ForArtwork(Some(mediaSource), artwork);

        url.ToString().Should().Be("https://some.jellyfin.server/jellyfin/Items/2/Images/3?tag=4");
    }

    [Test]
    public void Should_Work_With_Path_Prefix_With_Trailing_Slash()
    {
        var artwork = "jellyfin://Items/2/Images/3?tag=4";
        var address = "https://some.jellyfin.server/jellyfin/";
        var mediaSource = new JellyfinMediaSource
        {
            Connections = new List<JellyfinConnection>
            {
                new() { Address = address }
            }
        };

        Url url = JellyfinUrl.ForArtwork(Some(mediaSource), artwork);

        url.ToString().Should().Be("https://some.jellyfin.server/jellyfin/Items/2/Images/3?tag=4");
    }
}
