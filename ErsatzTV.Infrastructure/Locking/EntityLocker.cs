using System.Collections.Concurrent;
using ErsatzTV.Core.Interfaces.Locking;

namespace ErsatzTV.Infrastructure.Locking;

public class EntityLocker : IEntityLocker
{
    private readonly ConcurrentDictionary<int, byte> _lockedLibraries;
    private readonly ConcurrentDictionary<Type, byte> _lockedRemoteMediaSourceTypes;
    private bool _embyCollections;
    private bool _plex;
    private bool _trakt;

    public EntityLocker()
    {
        _lockedLibraries = new ConcurrentDictionary<int, byte>();
        _lockedRemoteMediaSourceTypes = new ConcurrentDictionary<Type, byte>();
    }

    public event EventHandler OnLibraryChanged;
    public event EventHandler OnPlexChanged;
    public event EventHandler<Type> OnRemoteMediaSourceChanged;
    public event EventHandler OnTraktChanged;
    public event EventHandler OnEmbyCollectionsChanged;

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
}
