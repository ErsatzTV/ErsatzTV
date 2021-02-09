using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Scheduling;
using LanguageExt;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.Scheduling
{
    public class RandomizedMediaCollectionEnumerator : IMediaCollectionEnumerator
    {
        private readonly IList<MediaItem> _mediaItems;
        private readonly Random _random;
        private int _index;
        private Option<int> _peekNext;

        public RandomizedMediaCollectionEnumerator(IList<MediaItem> mediaItems, MediaCollectionEnumeratorState state)
        {
            _mediaItems = mediaItems;
            _random = new Random(state.Seed);
            _peekNext = None;

            State = new MediaCollectionEnumeratorState { Seed = state.Seed };
            // we want to move at least once so we start with a random item and not the first
            // because _index defaults to 0
            while (State.Index <= state.Index)
            {
                MoveNext();
            }
        }

        public MediaCollectionEnumeratorState State { get; }

        public Option<MediaItem> Current => _mediaItems.Any() ? _mediaItems[_index] : None;

        public Option<MediaItem> Peek
        {
            get
            {
                if (_mediaItems.Any())
                {
                    return _peekNext.Match(
                        peek =>
                        {
                            Debug.WriteLine("returning existing peek");
                            return _mediaItems[peek];
                        },
                        () =>
                        {
                            Debug.WriteLine("setting peek");
                            // gen a random index but save it so we can use it again when
                            // we actually move next
                            int index = _random.Next() % _mediaItems.Count;
                            _peekNext = index;
                            return _mediaItems[index];
                        });
                }

                return None;
            }
        }

        public void MoveNext()
        {
            // TODO: reset seed at some predictable point so we don't overflow the index
            Debug.WriteLine("resetting peek");

            _index = _peekNext.IfNone(() => _random.Next() % _mediaItems.Count);
            _peekNext = None;

            State.Index++;
        }
    }
}
