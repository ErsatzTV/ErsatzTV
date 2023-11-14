namespace ErsatzTV.Core.Interfaces.Locking;

public interface IEntityLocker
{
    event EventHandler OnLibraryChanged;
    event EventHandler OnPlexChanged;
    event EventHandler<Type> OnRemoteMediaSourceChanged;
    event EventHandler OnTraktChanged;
    event EventHandler OnEmbyCollectionsChanged;
    event EventHandler OnJellyfinCollectionsChanged;
    event EventHandler OnPlexCollectionsChanged;
    event EventHandler<int> OnPlayoutChanged;
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
    bool LockEmbyCollections();
    bool UnlockEmbyCollections();
    bool AreEmbyCollectionsLocked();
    bool LockJellyfinCollections();
    bool UnlockJellyfinCollections();
    bool AreJellyfinCollectionsLocked();
    bool LockPlexCollections();
    bool UnlockPlexCollections();
    bool ArePlexCollectionsLocked();
    bool LockPlayout(int playoutId);
    bool UnlockPlayout(int playoutId);
    bool IsPlayoutLocked(int playoutId);
}
