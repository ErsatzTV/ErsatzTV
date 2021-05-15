using ErsatzTV.Core.Domain;

namespace ErsatzTV.ViewModels
{
    public class JellyfinMediaSourceLibraryEditViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public LibraryMediaKind MediaKind { get; init; }
        public bool ShouldSyncItems { get; set; }
    }
}
