using System;

namespace ErsatzTV.Core.Interfaces.Locking
{
    public interface IEntityLocker
    {
        event EventHandler OnLibraryChanged;
        bool LockLibrary(int libraryId);
        bool UnlockLibrary(int libraryId);
        bool IsLibraryLocked(int libraryId);
    }
}
