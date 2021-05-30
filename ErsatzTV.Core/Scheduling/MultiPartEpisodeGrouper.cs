using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ErsatzTV.Core.Domain;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.Scheduling
{
    public static class MultiPartEpisodeGrouper
    {
        public static List<GroupedMediaItem> GroupMediaItems(IList<MediaItem> mediaItems)
        {
            var episodes = mediaItems.OfType<Episode>().ToList();
            var showIds = episodes.Map(e => e.Season.ShowId).Distinct().ToList();

            var groups = new List<GroupedMediaItem>();
            GroupedMediaItem group = null;

            foreach (int showId in showIds)
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

                foreach (Episode episode in episodes.Filter(e => e.Season.ShowId == showId)
                    .OrderBy(identity, new ChronologicalMediaComparer()))
                {
                    string numberString = FindPartNumber(episode);
                    if (numberString != null)
                    {
                        var number = int.Parse(numberString);
                        if (number <= lastNumber && group != null)
                        {
                            groups.Add(group);
                            group = null;
                            lastNumber = 0;
                        }

                        if (number == lastNumber + 1)
                        {
                            if (lastNumber == 0)
                            {
                                // start a new group
                                group = new GroupedMediaItem(episode, null);
                            }
                            else if (group != null)
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

        private static string FindPartNumber(Episode e)
        {
            const string PATTERN = @"^.*\((\d+)\)( - .*)?$";
            Match match = Regex.Match(e.EpisodeMetadata.Head().Title, PATTERN);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            const string PATTERN_2 = @"^.*Part (\d+)$";
            Match match2 = Regex.Match(e.EpisodeMetadata.Head().Title, PATTERN_2);
            return match2.Success ? match2.Groups[1].Value : null;
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
    }
}
