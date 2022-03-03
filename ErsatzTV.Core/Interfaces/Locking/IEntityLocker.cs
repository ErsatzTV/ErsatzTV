namespace ErsatzTV.Core.Interfaces.Locking;

public interface IEntityLocker
{
    event EventHandler OnLibraryChanged;
    event EventHandler OnPlexChanged;
    event EventHandler<Type> OnRemoteMediaSourceChanged;
    event EventHandler OnTraktChanged;
    bool LockLibrary(int libraryId);
    bool UnlockLibrary(int libraryId);
    bool IsLibraryLocked(int libraryId);
    bool LockPlex();
    bool UnlockPlex();
    bool IsPlexLocked();
    bool IsRemoteMediaSourceLocked<TMediaSource>();
    bool LockRemoteMediaSource<TMediaSource>();
    bool UnlockRemoteMediaSource<TMediaSource>();
    bool IsTraktLocked();
    bool LockTrakt();
    bool UnlockTrakt();
}