using System.Data.Common;
using System.Xml;
using Dapper;
using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IO;
using Scriban;
using Scriban.Runtime;
using WebMarkupMin.Core;

namespace ErsatzTV.Application.Channels;

public class RefreshChannelListHandler : IRequestHandler<RefreshChannelList>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly ILocalFileSystem _localFileSystem;
    private readonly ILogger<RefreshChannelListHandler> _logger;
    private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

    public RefreshChannelListHandler(
        RecyclableMemoryStreamManager recyclableMemoryStreamManager,
        IDbContextFactory<TvContext> dbContextFactory,
        ILocalFileSystem localFileSystem,
        ILogger<RefreshChannelListHandler> logger)
    {
        _recyclableMemoryStreamManager = recyclableMemoryStreamManager;
        _dbContextFactory = dbContextFactory;
        _localFileSystem = localFileSystem;
        _logger = logger;
    }

    public async Task Handle(RefreshChannelList request, CancellationToken cancellationToken)
    {
        _localFileSystem.EnsureFolderExists(FileSystemLayout.ChannelGuideCacheFolder);

        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        string templateFileName = Path.Combine(FileSystemLayout.ChannelGuideTemplatesFolder, "channel.sbntxt");

        // fall back to default template
        if (!_localFileSystem.FileExists(templateFileName))
        {
            templateFileName = Path.Combine(FileSystemLayout.ChannelGuideTemplatesFolder, "_channel.sbntxt");
        }

        // fail if file doesn't exist
        if (!_localFileSystem.FileExists(templateFileName))
        {
            _logger.LogError(
                "Unable to generate channel list without template file {File}; please restart ErsatzTV",
                templateFileName);

            return;
        }

        var minifier = new XmlMinifier(
            new XmlMinificationSettings
            {
                MinifyWhitespace = true,
                RemoveXmlComments = true,
                CollapseTagsWithoutContent = true
            });

        string text = await File.ReadAllTextAsync(templateFileName, cancellationToken);
        var template = Template.Parse(text, templateFileName);
        var templateContext = new XmlTemplateContext();

        await using RecyclableMemoryStream ms = _recyclableMemoryStreamManager.GetStream();
        await using var xml = XmlWriter.Create(
            ms,
            new XmlWriterSettings { Async = true, ConformanceLevel = ConformanceLevel.Fragment });

        await foreach (ChannelResult channel in GetChannels(dbContext).WithCancellation(cancellationToken))
        {
            var data = new
            {
                ChannelNumber = channel.Number,
                ChannelName = channel.Name,
                ChannelCategories = GetCategories(channel.Categories),
                ChannelHasArtwork = !string.IsNullOrWhiteSpace(channel.ArtworkPath),
                ChannelArtworkPath = channel.ArtworkPath,
                ChannelNameEncoded = channel.Name.Replace(" ", "%20")
            };

            var scriptObject = new ScriptObject();
            scriptObject.Import(data);
            templateContext.PushGlobal(scriptObject);

            string result = await template.RenderAsync(templateContext);

            MarkupMinificationResult minified = minifier.Minify(result);
            await xml.WriteRawAsync(minified.MinifiedContent);
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

    // ReSharper disable once ClassNeverInstantiated.Local
    private sealed record ChannelResult(string Number, string Name, string Categories, string ArtworkPath);
}
