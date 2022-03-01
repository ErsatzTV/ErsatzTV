using System;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;

namespace ErsatzTV.Core.FFmpeg;

public class FFmpegProcessServiceFactory : IFFmpegProcessServiceFactory
{
    private readonly IConfigElementRepository _configElementRepository;
    private readonly IServiceProvider _serviceProvider;

    public FFmpegProcessServiceFactory(IConfigElementRepository configElementRepository, IServiceProvider serviceProvider)
    {
        _configElementRepository = configElementRepository;
        _serviceProvider = serviceProvider;
    }

    public async Task<IFFmpegProcessService> GetService()
    {
        Option<bool> useLegacyTranscoder =
            await _configElementRepository.GetValue<bool>(ConfigElementKey.FFmpegUseLegacyTranscoder);

        return await useLegacyTranscoder.IfNoneAsync(false)
            ? _serviceProvider.GetRequiredService<FFmpegProcessService>()
            : _serviceProvider.GetRequiredService<FFmpegLibraryProcessService>();
    }
}
