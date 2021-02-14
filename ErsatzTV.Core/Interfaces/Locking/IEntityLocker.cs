using System;

namespace ErsatzTV.Core.Interfaces.Locking
{
    public interface IEntityLocker
    {
        public event EventHandler OnMediaSourceChanged;
        public bool LockMediaSource(int mediaSourceId);
        public bool UnlockMediaSource(int mediaSourceId);
        public bool IsMediaSourceLocked(int mediaSourceId);
    }
}
