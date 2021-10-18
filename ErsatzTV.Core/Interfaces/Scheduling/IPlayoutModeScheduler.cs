using System;
using System.Collections.Generic;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Scheduling;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Interfaces.Scheduling
{
    public interface IPlayoutModeScheduler<in T> where T : ProgramScheduleItem
    {
        Tuple<PlayoutBuilderState, List<PlayoutItem>> Schedule(
            PlayoutBuilderState playoutBuilderState,
            Dictionary<CollectionKey, IMediaCollectionEnumerator> collectionEnumerators,
            T scheduleItem,
            ProgramScheduleItem nextScheduleItem,
            DateTimeOffset hardStop,
            ILogger logger);
        
        // Tuple<PlayoutBuilderState, List<PlayoutItem>> Schedule(
        //     PlayoutBuilderState playoutBuilderState,
        //     Dictionary<CollectionKey, IMediaCollectionEnumerator> collectionEnumerators,
        //     Map<CollectionKey, List<MediaItem>> collectionMediaItems,
        //     List<ProgramScheduleItem> sortedScheduleItems,
        //     ProgramScheduleItem scheduleItem,
        //     ILogger logger);
    }
}
