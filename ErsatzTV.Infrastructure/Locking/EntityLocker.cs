using System;
using System.Collections.Concurrent;
using ErsatzTV.Core.Interfaces.Locking;

namespace ErsatzTV.Infrastructure.Locking
{
    public class EntityLocker : IEntityLocker
    {
        private readonly ConcurrentDictionary<int, byte> _lockedMediaSources;
        private bool _jellyfin;
        private bool _plex;

        public EntityLocker() => _lockedMediaSources = new ConcurrentDictionary<int, byte>();

        public event EventHandler OnLibraryChanged;

        public event EventHandler OnPlexChanged;

        public event EventHandler OnJellyfinChanged;

        public bool LockLibrary(int mediaSourceId)
        {
            if (!_lockedMediaSources.ContainsKey(mediaSourceId) && _lockedMediaSources.TryAdd(mediaSourceId, 0))
            {
                OnLibraryChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }

            return false;
        }

        public bool UnlockLibrary(int mediaSourceId)
        {
            if (_lockedMediaSources.TryRemove(mediaSourceId, out byte _))
            {
                OnLibraryChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }

            return false;
        }

        public bool IsLibraryLocked(int mediaSourceId) =>
            _lockedMediaSources.ContainsKey(mediaSourceId);

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

        public bool LockJellyfin()
        {
            if (!_jellyfin)
            {
                _jellyfin = true;
                OnJellyfinChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }

            return false;
        }

        public bool UnlockJellyfin()
        {
            if (_jellyfin)
            {
                _jellyfin = false;
                OnJellyfinChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }

            return false;
        }
    }
}
