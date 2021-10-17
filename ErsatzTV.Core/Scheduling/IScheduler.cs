using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Scheduling;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Scheduling
{
    public interface IScheduler
    {
        Task<Tuple<PlayoutBuilderState, Option<PlayoutItem>>> Schedule(
            PlayoutBuilderState playoutBuilderState,
            Dictionary<CollectionKey, IMediaCollectionEnumerator> collectionEnumerators,
            Map<CollectionKey, List<MediaItem>> collectionMediaItems,
            List<ProgramScheduleItem> sortedScheduleItems,
            ProgramScheduleItem scheduleItem,
            ILogger logger);
    }
}
