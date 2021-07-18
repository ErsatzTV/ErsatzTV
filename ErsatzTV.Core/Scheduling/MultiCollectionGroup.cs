﻿using System;
using System.Linq;
using ErsatzTV.Core.Domain;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.Scheduling
{
    public class MultiCollectionGroup : GroupedMediaItem
    {
        public MultiCollectionGroup(CollectionWithItems collectionWithItems)
        {
            if (collectionWithItems.UseCustomOrder)
            {
                if (collectionWithItems.MediaItems.Count > 0)
                {
                    First = collectionWithItems.MediaItems.Head();
                    Additional = collectionWithItems.MediaItems.Tail().ToList();
                }
                else
                {
                    throw new InvalidOperationException("Collection has no items");
                }
            }
            else
            {
                switch (collectionWithItems.PlaybackOrder)
                {
                    case PlaybackOrder.Chronological:
                        var sortedItems = collectionWithItems.MediaItems.OrderBy(identity, new ChronologicalMediaComparer())
                            .ToList();
                        First = sortedItems.Head();
                        Additional = sortedItems.Tail().ToList();
                        break;
                    default:
                        throw new NotSupportedException(
                            $"Unsupported MultiCollection PlaybackOrder: {collectionWithItems.PlaybackOrder}");
                }
            }
        }
    }
}
