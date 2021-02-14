using System;
using System.Collections.Concurrent;
using ErsatzTV.Core.Interfaces.Locking;

namespace ErsatzTV.Infrastructure.Locking
{
    public class EntityLocker : IEntityLocker
    {
        private readonly ConcurrentDictionary<int, byte> _lockedMediaSources;

        public EntityLocker() => _lockedMediaSources = new ConcurrentDictionary<int, byte>();

        public event EventHandler OnMediaSourceChanged;

        public bool LockMediaSource(int mediaSourceId)
        {
            if (!_lockedMediaSources.ContainsKey(mediaSourceId) && _lockedMediaSources.TryAdd(mediaSourceId, 0))
            {
                OnMediaSourceChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }

            return false;
        }

        public bool UnlockMediaSource(int mediaSourceId)
        {
            if (_lockedMediaSources.TryRemove(mediaSourceId, out byte _))
            {
                OnMediaSourceChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }

            return false;
        }

        public bool IsMediaSourceLocked(int mediaSourceId) =>
            _lockedMediaSources.ContainsKey(mediaSourceId);
    }
}
