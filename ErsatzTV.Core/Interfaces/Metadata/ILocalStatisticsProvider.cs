﻿using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Metadata;

public interface ILocalStatisticsProvider
{
    Task<Either<BaseError, bool>> RefreshStatistics(string ffprobePath, MediaItem mediaItem);
    Task<Either<BaseError, bool>> RefreshStatistics(string ffprobePath, MediaItem mediaItem, string mediaItemPath);

    Task<Either<BaseError, Dictionary<string, string>>> GetFormatTags(string ffprobePath, MediaItem mediaItem);
}