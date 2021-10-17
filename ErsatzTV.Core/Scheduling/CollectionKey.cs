using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Scheduling
{
    public class CollectionKey : Record<CollectionKey>
    {
        public ProgramScheduleItemCollectionType CollectionType { get; set; }
        public int? CollectionId { get; set; }
        public int? MultiCollectionId { get; set; }
        public int? SmartCollectionId { get; set; }
        public int? MediaItemId { get; set; }
    }
}
