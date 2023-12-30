using System.Data.Common;
using System.Xml;
using Dapper;
using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IO;

namespace ErsatzTV.Application.Channels;

public class RefreshChannelListHandler : IRequestHandler<RefreshChannelList>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly ILocalFileSystem _localFileSystem;
    private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

    public RefreshChannelListHandler(
        RecyclableMemoryStreamManager recyclableMemoryStreamManager,
        IDbContextFactory<TvContext> dbContextFactory,
        ILocalFileSystem localFileSystem)
    {
        _recyclableMemoryStreamManager = recyclableMemoryStreamManager;
        _dbContextFactory = dbContextFactory;
        _localFileSystem = localFileSystem;
    }

    public async Task Handle(RefreshChannelList request, CancellationToken cancellationToken)
    {
        _localFileSystem.EnsureFolderExists(FileSystemLayout.ChannelGuideCacheFolder);

        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        await using RecyclableMemoryStream ms = _recyclableMemoryStreamManager.GetStream();
        await using var xml = XmlWriter.Create(
            ms,
            new XmlWriterSettings { Async = true, ConformanceLevel = ConformanceLevel.Fragment });

        await foreach (ChannelResult channel in GetChannels(dbContext).WithCancellation(cancellationToken))
        {
            await xml.WriteStartElementAsync(null, "channel", null);
            await xml.WriteAttributeStringAsync(null, "id", null, $"{channel.Number}.etv");

            await xml.WriteStartElementAsync(null, "display-name", null);
            await xml.WriteStringAsync($"{channel.Number} {channel.Name}");
            await xml.WriteEndElementAsync(); // display-name (number and name)

            await xml.WriteStartElementAsync(null, "display-name", null);
            await xml.WriteStringAsync(channel.Number);
            await xml.WriteEndElementAsync(); // display-name (number)

            await xml.WriteStartElementAsync(null, "display-name", null);
            await xml.WriteStringAsync(channel.Name);
            await xml.WriteEndElementAsync(); // display-name (name)

            foreach (string category in GetCategories(channel.Categories))
            {
                await xml.WriteStartElementAsync(null, "category", null);
                await xml.WriteAttributeStringAsync(null, "lang", null, "en");
                await xml.WriteStringAsync(category);
                await xml.WriteEndElementAsync(); // category
            }

            await xml.WriteStartElementAsync(null, "icon", null);
            await xml.WriteAttributeStringAsync(null, "src", null, GetIconUrl(channel));
            await xml.WriteEndElementAsync(); // icon

            await xml.WriteEndElementAsync(); // channel
        }

        await xml.FlushAsync();

        string tempFile = Path.GetTempFileName();
        await File.WriteAllBytesAsync(tempFile, ms.ToArray(), cancellationToken);

        string targetFile = Path.Combine(FileSystemLayout.ChannelGuideCacheFolder, "channels.xml");
        File.Move(tempFile, targetFile, true);
    }

    private static async IAsyncEnumerable<ChannelResult> GetChannels(TvContext dbContext)
    {
        const string QUERY = @"select C.Number, C.Name, C.Categories, A.Path as ArtworkPath
                               from Channel C
                               left outer join Artwork A on C.Id = A.ChannelId and A.ArtworkKind = 2
                               where C.Id in (select ChannelId from Playout)
                               order by CAST(C.Number as double)";
        // TODO: this needs to be fixed for sqlite/mariadb

        await using var reader = (DbDataReader)await dbContext.Connection.ExecuteReaderAsync(QUERY);
        Func<DbDataReader, ChannelResult> rowParser = reader.GetRowParser<ChannelResult>();

        while (await reader.ReadAsync())
        {
            yield return rowParser(reader);
        }

        while (await reader.NextResultAsync())
        {
        }
    }

    private static List<string> GetCategories(string categories) =>
        (categories ?? string.Empty).Split(',')
        .Map(s => s.Trim())
        .Filter(s => !string.IsNullOrWhiteSpace(s))
        .Distinct()
        .ToList();

    private static string GetIconUrl(ChannelResult channel) =>
        string.IsNullOrWhiteSpace(channel.ArtworkPath)
            ? "{RequestBase}/iptv/images/ersatztv-500.png{AccessTokenUri}"
            : $"{{RequestBase}}/iptv/logos/{channel.ArtworkPath}.jpg{{AccessTokenUri}}";

    // ReSharper disable once ClassNeverInstantiated.Local
    private sealed record ChannelResult(string Number, string Name, string Categories, string ArtworkPath);
}
