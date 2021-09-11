﻿using System.Linq;
using ErsatzTV.Core.Domain;
using static LanguageExt.Prelude;

namespace ErsatzTV.Application.MediaCollections
{
    internal static class Mapper
    {
        internal static MediaCollectionViewModel ProjectToViewModel(Collection collection) =>
            new(collection.Id, collection.Name, collection.UseCustomPlaybackOrder);

        internal static MultiCollectionViewModel ProjectToViewModel(MultiCollection multiCollection) =>
            new(
                multiCollection.Id,
                multiCollection.Name,
                Optional(multiCollection.MultiCollectionItems).Flatten().Map(ProjectToViewModel).ToList(),
                Optional(multiCollection.MultiCollectionSmartItems).Flatten().Map(ProjectToViewModel).ToList());

        internal static SmartCollectionViewModel ProjectToViewModel(SmartCollection collection) =>
            new(collection.Id, collection.Name, collection.Query);

        private static MultiCollectionItemViewModel ProjectToViewModel(MultiCollectionItem multiCollectionItem) =>
            new(
                multiCollectionItem.MultiCollectionId,
                ProjectToViewModel(multiCollectionItem.Collection),
                multiCollectionItem.ScheduleAsGroup,
                multiCollectionItem.PlaybackOrder);

        private static MultiCollectionSmartItemViewModel ProjectToViewModel(MultiCollectionSmartItem multiCollectionSmartItem) =>
            new(
                multiCollectionSmartItem.MultiCollectionId,
                ProjectToViewModel(multiCollectionSmartItem.SmartCollection),
                multiCollectionSmartItem.ScheduleAsGroup,
                multiCollectionSmartItem.PlaybackOrder);
    }
}
