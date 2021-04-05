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
            CollectionEnumeratorState state)
        {
            _sortedMediaItems = mediaItems.OrderBy(identity, new ChronologicalComparer()).ToList();

            State = new CollectionEnumeratorState { Seed = state.Seed };
            while (State.Index < state.Index)
            {
                MoveNext();
            }
        }

        public CollectionEnumeratorState State { get; }

        public Option<MediaItem> Current => _sortedMediaItems.Any() ? _sortedMediaItems[State.Index] : None;

        public void MoveNext() => State.Index = (State.Index + 1) % _sortedMediaItems.Count;

        private class ChronologicalComparer : IComparer<MediaItem>
        {
            public int Compare(MediaItem x, MediaItem y)
            {
                if (x == null || y == null)
                {
                    return 0;
                }

                DateTime date1 = x switch
                {
                    Episode e => e.EpisodeMetadata.HeadOrNone().Match(
                        em => em.ReleaseDate ?? DateTime.MaxValue,
                        () => DateTime.MaxValue),
                    Movie m => m.MovieMetadata.HeadOrNone().Match(
                        mm => mm.ReleaseDate ?? DateTime.MaxValue,
                        () => DateTime.MaxValue),
                    MusicVideo mv => mv.MusicVideoMetadata.HeadOrNone().Match(
                        mvm => mvm.ReleaseDate ?? DateTime.MaxValue,
                        () => DateTime.MaxValue),
                    _ => DateTime.MaxValue
                };

                DateTime date2 = y switch
                {
                    Episode e => e.EpisodeMetadata.HeadOrNone().Match(
                        em => em.ReleaseDate ?? DateTime.MaxValue,
                        () => DateTime.MaxValue),
                    Movie m => m.MovieMetadata.HeadOrNone().Match(
                        mm => mm.ReleaseDate ?? DateTime.MaxValue,
                        () => DateTime.MaxValue),
                    MusicVideo mv => mv.MusicVideoMetadata.HeadOrNone().Match(
                        mvm => mvm.ReleaseDate ?? DateTime.MaxValue,
                        () => DateTime.MaxValue),
                    _ => DateTime.MaxValue
                };

                if (date1 != date2)
                {
                    return date1.CompareTo(date2);
                }

                int season1 = x switch
                {
                    Episode e => e.Season?.SeasonNumber ?? int.MaxValue,
                    _ => int.MaxValue
                };

                int season2 = y switch
                {
                    Episode e => e.Season?.SeasonNumber ?? int.MaxValue,
                    _ => int.MaxValue
                };

                if (season1 != season2)
                {
                    return season1.CompareTo(season2);
                }

                int episode1 = x switch
                {
                    Episode e => e.EpisodeNumber,
                    _ => int.MaxValue
                };

                int episode2 = y switch
                {
                    Episode e => e.EpisodeNumber,
                    _ => int.MaxValue
                };

                if (episode1 != episode2)
                {
                    return episode1.CompareTo(episode2);
                }

                return x.Id.CompareTo(y.Id);
            }
        }
    }
}
