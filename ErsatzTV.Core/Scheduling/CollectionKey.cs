﻿using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Domain.Scheduling;

namespace ErsatzTV.Core.Scheduling;

public class CollectionKey : Record<CollectionKey>
{
    public ProgramScheduleItemCollectionType CollectionType { get; set; }
    public int? CollectionId { get; set; }
    public int? MultiCollectionId { get; set; }
    public int? SmartCollectionId { get; set; }
    public int? MediaItemId { get; set; }
    public int? PlaylistId { get; set; }
    public string FakeCollectionKey { get; set; }

    public static CollectionKey ForPlaylistItem(PlaylistItem item) =>
        item.CollectionType switch
        {
            ProgramScheduleItemCollectionType.Collection => new CollectionKey
            {
                CollectionType = item.CollectionType,
                CollectionId = item.CollectionId
            },
            ProgramScheduleItemCollectionType.TelevisionShow => new CollectionKey
            {
                CollectionType = item.CollectionType,
                MediaItemId = item.MediaItemId
            },
            ProgramScheduleItemCollectionType.TelevisionSeason => new CollectionKey
            {
                CollectionType = item.CollectionType,
                MediaItemId = item.MediaItemId
            },
            ProgramScheduleItemCollectionType.Artist => new CollectionKey
            {
                CollectionType = item.CollectionType,
                MediaItemId = item.MediaItemId
            },
            ProgramScheduleItemCollectionType.MultiCollection => new CollectionKey
            {
                CollectionType = item.CollectionType,
                MultiCollectionId = item.MultiCollectionId
            },
            ProgramScheduleItemCollectionType.SmartCollection => new CollectionKey
            {
                CollectionType = item.CollectionType,
                SmartCollectionId = item.SmartCollectionId
            },
            ProgramScheduleItemCollectionType.FakeCollection => new CollectionKey
            {
                CollectionType = item.CollectionType
            },
            ProgramScheduleItemCollectionType.Movie => new CollectionKey
            {
                CollectionType = item.CollectionType,
                MediaItemId = item.MediaItemId
            },
            ProgramScheduleItemCollectionType.Episode => new CollectionKey
            {
                CollectionType = item.CollectionType,
                MediaItemId = item.MediaItemId
            },
            ProgramScheduleItemCollectionType.MusicVideo => new CollectionKey
            {
                CollectionType = item.CollectionType,
                MediaItemId = item.MediaItemId
            },
            ProgramScheduleItemCollectionType.OtherVideo => new CollectionKey
            {
                CollectionType = item.CollectionType,
                MediaItemId = item.MediaItemId
            },
            ProgramScheduleItemCollectionType.Song => new CollectionKey
            {
                CollectionType = item.CollectionType,
                MediaItemId = item.MediaItemId
            },
            ProgramScheduleItemCollectionType.Image => new CollectionKey
            {
                CollectionType = item.CollectionType,
                MediaItemId = item.MediaItemId
            },
            _ => throw new ArgumentOutOfRangeException(nameof(item))
        };

    public static CollectionKey ForBlockItem(BlockItem item) =>
        item.CollectionType switch
        {
            ProgramScheduleItemCollectionType.Collection => new CollectionKey
            {
                CollectionType = item.CollectionType,
                CollectionId = item.CollectionId
            },
            ProgramScheduleItemCollectionType.TelevisionShow => new CollectionKey
            {
                CollectionType = item.CollectionType,
                MediaItemId = item.MediaItemId
            },
            ProgramScheduleItemCollectionType.TelevisionSeason => new CollectionKey
            {
                CollectionType = item.CollectionType,
                MediaItemId = item.MediaItemId
            },
            ProgramScheduleItemCollectionType.Artist => new CollectionKey
            {
                CollectionType = item.CollectionType,
                MediaItemId = item.MediaItemId
            },
            ProgramScheduleItemCollectionType.MultiCollection => new CollectionKey
            {
                CollectionType = item.CollectionType,
                MultiCollectionId = item.MultiCollectionId
            },
            ProgramScheduleItemCollectionType.SmartCollection => new CollectionKey
            {
                CollectionType = item.CollectionType,
                SmartCollectionId = item.SmartCollectionId
            },
            ProgramScheduleItemCollectionType.FakeCollection => new CollectionKey
            {
                CollectionType = item.CollectionType
            },
            ProgramScheduleItemCollectionType.Movie => new CollectionKey
            {
                CollectionType = item.CollectionType,
                MediaItemId = item.MediaItemId
            },
            ProgramScheduleItemCollectionType.Episode => new CollectionKey
            {
                CollectionType = item.CollectionType,
                MediaItemId = item.MediaItemId
            },
            ProgramScheduleItemCollectionType.MusicVideo => new CollectionKey
            {
                CollectionType = item.CollectionType,
                MediaItemId = item.MediaItemId
            },
            ProgramScheduleItemCollectionType.OtherVideo => new CollectionKey
            {
                CollectionType = item.CollectionType,
                MediaItemId = item.MediaItemId
            },
            ProgramScheduleItemCollectionType.Song => new CollectionKey
            {
                CollectionType = item.CollectionType,
                MediaItemId = item.MediaItemId
            },
            ProgramScheduleItemCollectionType.Image => new CollectionKey
            {
                CollectionType = item.CollectionType,
                MediaItemId = item.MediaItemId
            },
            _ => throw new ArgumentOutOfRangeException(nameof(item))
        };

    public static CollectionKey ForDecoDefaultFiller(Deco deco) =>
        deco.DefaultFillerCollectionType switch
        {
            ProgramScheduleItemCollectionType.Collection => new CollectionKey
            {
                CollectionType = deco.DefaultFillerCollectionType,
                CollectionId = deco.DefaultFillerCollectionId
            },
            ProgramScheduleItemCollectionType.TelevisionShow => new CollectionKey
            {
                CollectionType = deco.DefaultFillerCollectionType,
                MediaItemId = deco.DefaultFillerMediaItemId
            },
            ProgramScheduleItemCollectionType.TelevisionSeason => new CollectionKey
            {
                CollectionType = deco.DefaultFillerCollectionType,
                MediaItemId = deco.DefaultFillerMediaItemId
            },
            ProgramScheduleItemCollectionType.Artist => new CollectionKey
            {
                CollectionType = deco.DefaultFillerCollectionType,
                MediaItemId = deco.DefaultFillerMediaItemId
            },
            ProgramScheduleItemCollectionType.MultiCollection => new CollectionKey
            {
                CollectionType = deco.DefaultFillerCollectionType,
                MultiCollectionId = deco.DefaultFillerMultiCollectionId
            },
            ProgramScheduleItemCollectionType.SmartCollection => new CollectionKey
            {
                CollectionType = deco.DefaultFillerCollectionType,
                SmartCollectionId = deco.DefaultFillerSmartCollectionId
            },
            ProgramScheduleItemCollectionType.FakeCollection => new CollectionKey
            {
                CollectionType = deco.DefaultFillerCollectionType
            },
            ProgramScheduleItemCollectionType.Movie => new CollectionKey
            {
                CollectionType = deco.DefaultFillerCollectionType,
                MediaItemId = deco.DefaultFillerMediaItemId
            },
            ProgramScheduleItemCollectionType.Episode => new CollectionKey
            {
                CollectionType = deco.DefaultFillerCollectionType,
                MediaItemId = deco.DefaultFillerMediaItemId
            },
            ProgramScheduleItemCollectionType.MusicVideo => new CollectionKey
            {
                CollectionType = deco.DefaultFillerCollectionType,
                MediaItemId = deco.DefaultFillerMediaItemId
            },
            ProgramScheduleItemCollectionType.OtherVideo => new CollectionKey
            {
                CollectionType = deco.DefaultFillerCollectionType,
                MediaItemId = deco.DefaultFillerMediaItemId
            },
            ProgramScheduleItemCollectionType.Song => new CollectionKey
            {
                CollectionType = deco.DefaultFillerCollectionType,
                MediaItemId = deco.DefaultFillerMediaItemId
            },
            ProgramScheduleItemCollectionType.Image => new CollectionKey
            {
                CollectionType = deco.DefaultFillerCollectionType,
                MediaItemId = deco.DefaultFillerMediaItemId
            },
            _ => throw new ArgumentOutOfRangeException(nameof(deco))
        };

    public static CollectionKey ForScheduleItem(ProgramScheduleItem item) =>
        item.CollectionType switch
        {
            ProgramScheduleItemCollectionType.Collection => new CollectionKey
            {
                CollectionType = item.CollectionType,
                CollectionId = item.CollectionId,
                FakeCollectionKey = item.FakeCollectionKey
            },
            ProgramScheduleItemCollectionType.TelevisionShow => new CollectionKey
            {
                CollectionType = item.CollectionType,
                MediaItemId = item.MediaItemId,
                FakeCollectionKey = item.FakeCollectionKey
            },
            ProgramScheduleItemCollectionType.TelevisionSeason => new CollectionKey
            {
                CollectionType = item.CollectionType,
                MediaItemId = item.MediaItemId,
                FakeCollectionKey = item.FakeCollectionKey
            },
            ProgramScheduleItemCollectionType.Artist => new CollectionKey
            {
                CollectionType = item.CollectionType,
                MediaItemId = item.MediaItemId,
                FakeCollectionKey = item.FakeCollectionKey
            },
            ProgramScheduleItemCollectionType.MultiCollection => new CollectionKey
            {
                CollectionType = item.CollectionType,
                MultiCollectionId = item.MultiCollectionId,
                FakeCollectionKey = item.FakeCollectionKey
            },
            ProgramScheduleItemCollectionType.SmartCollection => new CollectionKey
            {
                CollectionType = item.CollectionType,
                SmartCollectionId = item.SmartCollectionId,
                FakeCollectionKey = item.FakeCollectionKey
            },
            ProgramScheduleItemCollectionType.Playlist => new CollectionKey
            {
                CollectionType = item.CollectionType,
                PlaylistId = item.PlaylistId,
                FakeCollectionKey = item.FakeCollectionKey
            },
            ProgramScheduleItemCollectionType.FakeCollection => new CollectionKey
            {
                CollectionType = item.CollectionType,
                FakeCollectionKey = item.FakeCollectionKey
            },
            _ => throw new ArgumentOutOfRangeException(nameof(item))
        };

    public static CollectionKey ForFillerPreset(FillerPreset filler) =>
        filler.CollectionType switch
        {
            ProgramScheduleItemCollectionType.Collection => new CollectionKey
            {
                CollectionType = filler.CollectionType,
                CollectionId = filler.CollectionId
            },
            ProgramScheduleItemCollectionType.TelevisionShow => new CollectionKey
            {
                CollectionType = filler.CollectionType,
                MediaItemId = filler.MediaItemId
            },
            ProgramScheduleItemCollectionType.TelevisionSeason => new CollectionKey
            {
                CollectionType = filler.CollectionType,
                MediaItemId = filler.MediaItemId
            },
            ProgramScheduleItemCollectionType.Artist => new CollectionKey
            {
                CollectionType = filler.CollectionType,
                MediaItemId = filler.MediaItemId
            },
            ProgramScheduleItemCollectionType.MultiCollection => new CollectionKey
            {
                CollectionType = filler.CollectionType,
                MultiCollectionId = filler.MultiCollectionId
            },
            ProgramScheduleItemCollectionType.SmartCollection => new CollectionKey
            {
                CollectionType = filler.CollectionType,
                SmartCollectionId = filler.SmartCollectionId
            },
            _ => throw new ArgumentOutOfRangeException(nameof(filler))
        };
}
