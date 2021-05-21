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
            var sortedMediaItems = mediaItems.OrderBy(identity, new ChronologicalMediaComparer()).ToList();
            var groups = new List<GroupedMediaItem>();
            GroupedMediaItem group = null;
            var lastNumber = 0;
            foreach (MediaItem item in sortedMediaItems)
            {
                if (item is Episode e)
                {
                    const string PATTERN = @"^.*\((\d+)\)( - .*)?$";
                    Match match = Regex.Match(e.EpisodeMetadata.Head().Title, PATTERN);
                    if (match.Success)
                    {
                        var number = int.Parse(match.Groups[1].Value);
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
                                group = new GroupedMediaItem(item, null);
                            }
                            else if (group != null)
                            {
                                // add to current group
                                List<MediaItem> additional = group.Additional ?? new List<MediaItem>();
                                additional.Add(item);
                                group = group with { Additional = additional };
                            }
                            else
                            {
                                // this should never happen
                                throw new InvalidOperationException("Bad shuffle state");
                            }

                            lastNumber = number;
                        }
                        else
                        {
                            // this should never happen
                            throw new InvalidOperationException(
                                $"Bad shuffle state; unexpected number {number} after {lastNumber}");
                        }
                    }
                    else
                    {
                        if (group != null && lastNumber != 0)
                        {
                            groups.Add(group);
                            group = null;
                            lastNumber = 0;
                        }

                        groups.Add(new GroupedMediaItem(item, null));
                    }
                }
                else
                {
                    groups.Add(new GroupedMediaItem(item, null));
                }
            }

            if (group != null && lastNumber != 0)
            {
                groups.Add(group);
            }

            return groups;
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
