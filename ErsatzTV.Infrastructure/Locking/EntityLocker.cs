using System;
using System.Collections.Concurrent;
using ErsatzTV.Core.Interfaces.Locking;

namespace ErsatzTV.Infrastructure.Locking
{
    public class EntityLocker : IEntityLocker
    {
        private readonly ConcurrentDictionary<int, byte> _lockedMediaSources;

        public EntityLocker() => _lockedMediaSources = new ConcurrentDictionary<int, byte>();

        public event EventHandler OnLibraryChanged;

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
    }
}
