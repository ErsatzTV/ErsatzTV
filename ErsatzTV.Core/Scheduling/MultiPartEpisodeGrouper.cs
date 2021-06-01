using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ErsatzTV.Core.Domain;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.Scheduling
{
    public static class MultiPartEpisodeGrouper
    {
        public static List<GroupedMediaItem> GroupMediaItems(IList<MediaItem> mediaItems, bool treatCollectionsAsShows)
        {
            var episodes = mediaItems.OfType<Episode>().ToList();
            // var showIds = episodes.Map(e => e.Season.ShowId).Distinct().ToList();

            var groups = new List<GroupedMediaItem>();
            GroupedMediaItem group = null;

            var showIds = new List<Option<int>>();
            if (treatCollectionsAsShows)
            {
                showIds.Add(Option<int>.None);
            }
            else
            {
                showIds.AddRange(episodes.Map(e => Some(e.Season.ShowId)).Distinct());
            }

            foreach (Option<int> showId in showIds)
            {
                var lastNumber = 0;

                void AddUngrouped(MediaItem item)
                {
                    if (group != null && lastNumber != 0)
                    {
                        groups.Add(group);
                        group = null;
                        lastNumber = 0;
                    }

                    groups.Add(new GroupedMediaItem(item, null));
                }

                IEnumerable<Episode> sortedEpisodes = showId.Match(
                    id => episodes.Filter(e => e.Season.ShowId == id),
                    () => episodes).OrderBy(identity, new ChronologicalMediaComparer());

                foreach (Episode episode in sortedEpisodes)
                {
                    Option<int> maybeNumber = FindPartNumber(episode);
                    if (maybeNumber.IsSome)
                    {
                        int number = maybeNumber.ValueUnsafe();
                        if (number <= lastNumber && group != null)
                        {
                            groups.Add(group);
                            group = null;
                            lastNumber = 0;
                        }

                        if (number > lastNumber)
                        {
                            if (lastNumber == 0)
                            {
                                // start a new group
                                group = new GroupedMediaItem(episode, null);
                                lastNumber = number;
                            }
                            else if (number == lastNumber + 1)
                            {
                                if (group != null)
                                {
                                    // add to current group
                                    List<MediaItem> additional = group.Additional ?? new List<MediaItem>();
                                    additional.Add(episode);
                                    group = group with { Additional = additional };
                                }
                                else
                                {
                                    // this should never happen
                                    throw new InvalidOperationException(
                                        $"Bad shuffle state; unexpected number {number} after {lastNumber} with no existing group");
                                }

                                lastNumber = number;
                            }
                            else
                            {
                                AddUngrouped(episode);
                            }
                        }
                        else
                        {
                            AddUngrouped(episode);
                        }
                    }
                    else
                    {
                        AddUngrouped(episode);
                    }
                }

                if (group != null && lastNumber != 0)
                {
                    groups.Add(group);
                    group = null;
                }
            }

            foreach (MediaItem notEpisode in mediaItems.Filter(i => i is not Episode))
            {
                groups.Add(new GroupedMediaItem(notEpisode, null));
            }

            return groups;
        }

        private static Option<int> FindPartNumber(Episode e)
        {
            const string PATTERN = @"^.*\((\d+)\)( - .*)?$";
            Match match = Regex.Match(e.EpisodeMetadata.Head().Title, PATTERN);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int value1))
            {
                return value1;
            }

            const string PATTERN_2 = @"^.*\(?Part (\d+)\)?$";
            Match match2 = Regex.Match(e.EpisodeMetadata.Head().Title, PATTERN_2);
            if (match2.Success && int.TryParse(match2.Groups[1].Value, out int value2))
            {
                return value2;
            }

            const string PATTERN_3 = @"^.*\(([MDCLXVI]+)\)( - .*)?$";
            Match match3 = Regex.Match(e.EpisodeMetadata.Head().Title, PATTERN_3);
            if (match3.Success && TryParseRoman(match3.Groups[1].Value, out int value3))
            {
                return value3;
            }

            const string PATTERN_4 = @"^.*Part (\w+)$";
            Match match4 = Regex.Match(e.EpisodeMetadata.Head().Title, PATTERN_4);
            if (match4.Success && TryParseEnglish(match4.Groups[1].Value, out int value4))
            {
                return value4;
            }

            return None;
        }

        public static IList<MediaItem> FlattenGroups(GroupedMediaItem[] copy, int mediaItemCount)
        {
            var result = new MediaItem[mediaItemCount];
            var i = 0;
            foreach (GroupedMediaItem group in copy)
            {
                result[i++] = group.First;
                foreach (MediaItem additional in Optional(group.Additional).Flatten())
                {
                    result[i++] = additional;
                }
            }

            return result;
        }

        private static bool TryParseRoman(string input, out int output)
        {
            switch (input?.ToLowerInvariant())
            {
                case "i":
                    output = 1;
                    return true;
                case "ii":
                    output = 2;
                    return true;
                case "iii":
                    output = 3;
                    return true;
                case "iv":
                    output = 4;
                    return true;
                case "v":
                    output = 5;
                    return true;
                case "vi":
                    output = 6;
                    return true;
                case "vii":
                    output = 7;
                    return true;
                case "viii" or "iix":
                    output = 8;
                    return true;
                case "ix":
                    output = 9;
                    return true;
                case "x":
                    output = 10;
                    return true;
                default:
                    output = 0;
                    return false;
            }
        }

        private static bool TryParseEnglish(string input, out int output)
        {
            switch (input?.ToLowerInvariant())
            {
                case "one":
                    output = 1;
                    return true;
                case "two":
                    output = 2;
                    return true;
                case "three":
                    output = 3;
                    return true;
                case "four":
                    output = 4;
                    return true;
                case "five":
                    output = 5;
                    return true;
                case "six":
                    output = 6;
                    return true;
                case "seven":
                    output = 7;
                    return true;
                case "eight":
                    output = 8;
                    return true;
                case "nine":
                    output = 9;
                    return true;
                case "ten":
                    output = 10;
                    return true;
                default:
                    output = 0;
                    return false;
            }
        }
    }
}
