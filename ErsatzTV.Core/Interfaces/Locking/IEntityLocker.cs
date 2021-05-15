using System;

namespace ErsatzTV.Core.Interfaces.Locking
{
    public interface IEntityLocker
    {
        event EventHandler OnLibraryChanged;
        event EventHandler OnPlexChanged;
        event EventHandler OnJellyfinChanged;
        bool LockLibrary(int libraryId);
        bool UnlockLibrary(int libraryId);
        bool IsLibraryLocked(int libraryId);
        bool LockPlex();
        bool UnlockPlex();
        bool IsPlexLocked();
        bool LockJellyfin();
        bool UnlockJellyfin();
    }
}
