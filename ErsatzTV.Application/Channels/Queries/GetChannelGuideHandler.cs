using System.Text;
using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Iptv;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IO;

namespace ErsatzTV.Application.Channels;

public class GetChannelGuideHandler : IRequestHandler<GetChannelGuide, Either<BaseError, ChannelGuide>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly ILocalFileSystem _localFileSystem;
    private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

    public GetChannelGuideHandler(
        IDbContextFactory<TvContext> dbContextFactory,
        RecyclableMemoryStreamManager recyclableMemoryStreamManager,
        ILocalFileSystem localFileSystem)
    {
        _dbContextFactory = dbContextFactory;
        _recyclableMemoryStreamManager = recyclableMemoryStreamManager;
        _localFileSystem = localFileSystem;
    }

    public async Task<Either<BaseError, ChannelGuide>> Handle(
        GetChannelGuide request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        string channelsFile = Path.Combine(FileSystemLayout.ChannelGuideCacheFolder, "channels.xml");
        if (!_localFileSystem.FileExists(channelsFile))
        {
            return BaseError.New($"Required file {channelsFile} is missing");
        }

        string accessTokenUri = string.Empty;
        if (!string.IsNullOrWhiteSpace(request.AccessToken))
        {
            accessTokenUri = $"?access_token={request.AccessToken}";
        }

        string channelsFragment = await File.ReadAllTextAsync(channelsFile, Encoding.UTF8, cancellationToken);

        // TODO: is regex faster?
        channelsFragment = channelsFragment
            .Replace("{RequestBase}", $"{request.Scheme}://{request.Host}{request.BaseUrl}")
            .Replace("{AccessTokenUri}", accessTokenUri);

        var channelDataFragments = new Dictionary<string, string>();

        foreach (string fileName in _localFileSystem.ListFiles(FileSystemLayout.ChannelGuideCacheFolder))
        {
            if (fileName.Contains("channels"))
            {
                continue;
            }

            string channelDataFragment = await File.ReadAllTextAsync(fileName, Encoding.UTF8, cancellationToken);

            channelDataFragment = channelDataFragment
                .Replace("{RequestBase}", $"{request.Scheme}://{request.Host}{request.BaseUrl}")
                .Replace("{AccessTokenUri}", accessTokenUri);

            channelDataFragments.Add(Path.GetFileNameWithoutExtension(fileName), channelDataFragment);
        }

        return new ChannelGuide(_recyclableMemoryStreamManager, channelsFragment, channelDataFragments);
    }
}
