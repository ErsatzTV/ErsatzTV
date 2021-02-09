﻿using System;
using System.Collections.Generic;
using System.Linq;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Scheduling;
using LanguageExt;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.Scheduling
{
    public sealed class ChronologicalMediaCollectionEnumerator : IMediaCollectionEnumerator
    {
        private readonly IList<MediaItem> _sortedMediaItems;

        public ChronologicalMediaCollectionEnumerator(
            IEnumerable<MediaItem> mediaItems,
            MediaCollectionEnumeratorState state)
        {
            _sortedMediaItems = mediaItems.OrderBy(c => c.Metadata.Aired ?? DateTime.MaxValue)
                .ThenBy(c => c.Metadata.SeasonNumber)
                .ThenBy(c => c.Metadata.EpisodeNumber)
                .ToList();

            State = new MediaCollectionEnumeratorState { Seed = state.Seed };
            while (State.Index < state.Index)
            {
                MoveNext();
            }
        }

        public MediaCollectionEnumeratorState State { get; }

        public Option<MediaItem> Current => _sortedMediaItems.Any() ? _sortedMediaItems[State.Index] : None;

        public Option<MediaItem> Peek => _sortedMediaItems.Any()
            ? _sortedMediaItems[(State.Index + 1) % _sortedMediaItems.Count]
            : None;

        public void MoveNext() => State.Index = (State.Index + 1) % _sortedMediaItems.Count;
    }
}
