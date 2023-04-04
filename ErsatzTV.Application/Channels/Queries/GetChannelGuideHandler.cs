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
    private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;
    private readonly ILocalFileSystem _localFileSystem;

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

        return new ChannelGuide(
            _recyclableMemoryStreamManager,
            request.Scheme,
            request.Host,
            request.BaseUrl,
            channelsFragment,
            request.AccessToken);
    }
}
