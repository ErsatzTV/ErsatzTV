using System;

namespace ErsatzTV.Core.Interfaces.Locking
{
    public interface IEntityLocker
    {
        event EventHandler OnMediaSourceChanged;
        bool LockMediaSource(int mediaSourceId);
        bool UnlockMediaSource(int mediaSourceId);
        bool IsMediaSourceLocked(int mediaSourceId);
    }
}
