﻿using System.Text.RegularExpressions;
using ErsatzTV.Core.Domain;
using LanguageExt.UnsafeValueAccess;

namespace ErsatzTV.Core.Scheduling;

public static partial class MultiPartEpisodeGrouper
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
                                group = new GroupedMediaItem(group.First, additional);
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
        foreach (EpisodeMetadata metadata in e.EpisodeMetadata.HeadOrNone())
        {
            Match match = Pattern1Regex().Match(metadata.Title ?? string.Empty);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int value1))
            {
                return value1;
            }

            Match match2 = Pattern2Regex().Match(metadata.Title ?? string.Empty);
            if (match2.Success && int.TryParse(match2.Groups[1].Value, out int value2))
            {
                return value2;
            }

            Match match3 = Pattern3Regex().Match(metadata.Title ?? string.Empty);
            if (match3.Success && TryParseRoman(match3.Groups[1].Value, out int value3))
            {
                return value3;
            }

            Match match4 = Pattern4Regex().Match(metadata.Title ?? string.Empty);
            if (match4.Success && TryParseEnglish(match4.Groups[1].Value, out int value4))
            {
                return value4;
            }
        }

        return None;
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

    [GeneratedRegex(@"^.*\((\d+)\)( - .*)?$")]
    private static partial Regex Pattern1Regex();

    [GeneratedRegex(@"^.*\(?Part (\d+)\)?$")]
    private static partial Regex Pattern2Regex();

    [GeneratedRegex(@"^.*\(([MDCLXVI]+)\)( - .*)?$")]
    private static partial Regex Pattern3Regex();

    [GeneratedRegex(@"^.*\(?Part (\w+)\)?$")]
    private static partial Regex Pattern4Regex();
}
