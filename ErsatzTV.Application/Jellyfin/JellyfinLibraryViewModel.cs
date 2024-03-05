﻿using ErsatzTV.Application.Libraries;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Jellyfin;

public record JellyfinLibraryViewModel(
    int Id,
    string Name,
    LibraryMediaKind MediaKind,
    bool ShouldSyncItems,
    int MediaSourceId)
    : LibraryViewModel("Jellyfin", Id, Name, MediaKind, MediaSourceId, string.Empty);
