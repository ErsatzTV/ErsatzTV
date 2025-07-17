using System.Collections.Concurrent;
using ErsatzTV.Core.Interfaces.Locking;
using ErsatzTV.Core.Notifications;
using MediatR;

namespace ErsatzTV.Infrastructure.Locking;

public class EntityLocker(IMediator mediator) : IEntityLocker
{
    private readonly ConcurrentDictionary<int, byte> _lockedLibraries = new();
    private readonly ConcurrentDictionary<int, byte> _lockedPlayouts = new();
    private readonly ConcurrentDictionary<Type, byte> _lockedRemoteMediaSourceTypes = new();
    private bool _embyCollections;
    private bool _jellyfinCollections;
    private bool _plex;
    private bool _plexCollections;
    private bool _trakt;
    private bool _troubleshootingPlayback;

    public event EventHandler OnLibraryChanged;
    public event EventHandler OnPlexChanged;
    public event EventHandler<Type> OnRemoteMediaSourceChanged;
    public event EventHandler OnTraktChanged;
    public event EventHandler OnEmbyCollectionsChanged;
    public event EventHandler OnJellyfinCollectionsChanged;
    public event EventHandler OnPlexCollectionsChanged;
    public event EventHandler OnTroubleshootingPlaybackChanged;

    public bool LockLibrary(int libraryId)
    {
        if (!_lockedLibraries.ContainsKey(libraryId) && _lockedLibraries.TryAdd(libraryId, 0))
        {
            OnLibraryChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        return false;
    }

    public bool UnlockLibrary(int libraryId)
    {
        if (_lockedLibraries.TryRemove(libraryId, out byte _))
        {
            OnLibraryChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        return false;
    }

    public bool IsLibraryLocked(int libraryId) =>
        _lockedLibraries.ContainsKey(libraryId);

    public bool LockPlex()
    {
        if (!_plex)
        {
            _plex = true;
            OnPlexChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        return false;
    }

    public bool UnlockPlex()
    {
        if (_plex)
        {
            _plex = false;
            OnPlexChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        return false;
    }

    public bool IsPlexLocked() => _plex;

    public bool IsRemoteMediaSourceLocked<TMediaSource>() =>
        _lockedRemoteMediaSourceTypes.ContainsKey(typeof(TMediaSource));

    public bool LockRemoteMediaSource<TMediaSource>()
    {
        Type mediaSourceType = typeof(TMediaSource);

        if (!_lockedRemoteMediaSourceTypes.ContainsKey(mediaSourceType) &&
            _lockedRemoteMediaSourceTypes.TryAdd(mediaSourceType, 0))
        {
            OnRemoteMediaSourceChanged?.Invoke(this, mediaSourceType);
            return true;
        }

        return false;
    }

    public bool UnlockRemoteMediaSource<TMediaSource>()
    {
        Type mediaSourceType = typeof(TMediaSource);

        if (_lockedRemoteMediaSourceTypes.TryRemove(mediaSourceType, out byte _))
        {
            OnRemoteMediaSourceChanged?.Invoke(this, mediaSourceType);
            return true;
        }

        return false;
    }

    public bool LockTrakt()
    {
        if (!_trakt)
        {
            _trakt = true;
            OnTraktChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        return false;
    }

    public bool UnlockTrakt()
    {
        if (_trakt)
        {
            _trakt = false;
            OnTraktChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        return false;
    }

    public bool IsTraktLocked() => _trakt;

    public bool LockEmbyCollections()
    {
        if (!_embyCollections)
        {
            _embyCollections = true;
            OnEmbyCollectionsChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        return false;
    }

    public bool UnlockEmbyCollections()
    {
        if (_embyCollections)
        {
            _embyCollections = false;
            OnEmbyCollectionsChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        return false;
    }

    public bool AreEmbyCollectionsLocked() => _embyCollections;

    public bool LockJellyfinCollections()
    {
        if (!_jellyfinCollections)
        {
            _jellyfinCollections = true;
            OnJellyfinCollectionsChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        return false;
    }

    public bool UnlockJellyfinCollections()
    {
        if (_jellyfinCollections)
        {
            _jellyfinCollections = false;
            OnJellyfinCollectionsChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        return false;
    }

    public bool AreJellyfinCollectionsLocked() => _jellyfinCollections;

    public bool LockPlexCollections()
    {
        if (!_plexCollections)
        {
            _plexCollections = true;
            OnPlexCollectionsChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        return false;
    }

    public bool UnlockPlexCollections()
    {
        if (_plexCollections)
        {
            _plexCollections = false;
            OnPlexCollectionsChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        return false;
    }

    public bool ArePlexCollectionsLocked() => _plexCollections;

    public async Task<bool> LockPlayout(int playoutId)
    {
        if (!_lockedPlayouts.ContainsKey(playoutId) && _lockedPlayouts.TryAdd(playoutId, 0))
        {
            await mediator.Publish(new PlayoutUpdatedNotification(playoutId, true));
            return true;
        }

        return false;
    }

    public async Task<bool> UnlockPlayout(int playoutId)
    {
        if (_lockedPlayouts.TryRemove(playoutId, out byte _))
        {
            await mediator.Publish(new PlayoutUpdatedNotification(playoutId, false));
            return true;
        }

        return false;
    }

    public bool IsPlayoutLocked(int playoutId) => _lockedPlayouts.ContainsKey(playoutId);

    public bool LockTroubleshootingPlayback()
    {
        if (!_troubleshootingPlayback)
        {
            _troubleshootingPlayback = true;
            OnTroubleshootingPlaybackChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        return false;
    }

    public bool UnlockTroubleshootingPlayback()
    {
        if (_troubleshootingPlayback)
        {
            _troubleshootingPlayback = false;
            OnTroubleshootingPlaybackChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        return false;
    }

    public bool IsTroubleshootingPlaybackLocked() => _troubleshootingPlayback;
}
