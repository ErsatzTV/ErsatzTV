﻿using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Domain.Scheduling;

namespace ErsatzTV.Core.Scheduling;

public class CollectionKey : Record<CollectionKey>
{
    public CollectionType CollectionType { get; set; }
    public int? CollectionId { get; set; }
    public int? MultiCollectionId { get; set; }
    public int? SmartCollectionId { get; set; }
    public int? MediaItemId { get; set; }
    public int? PlaylistId { get; set; }
    public string FakeCollectionKey { get; set; }

    public static CollectionKey ForPlaylistItem(PlaylistItem item) =>
        item.CollectionType switch
        {
            CollectionType.Collection => new CollectionKey
            {
                CollectionType = item.CollectionType,
                CollectionId = item.CollectionId
            },
            CollectionType.TelevisionShow => new CollectionKey
            {
                CollectionType = item.CollectionType,
                MediaItemId = item.MediaItemId
            },
            CollectionType.TelevisionSeason => new CollectionKey
            {
                CollectionType = item.CollectionType,
                MediaItemId = item.MediaItemId
            },
            CollectionType.Artist => new CollectionKey
            {
                CollectionType = item.CollectionType,
                MediaItemId = item.MediaItemId
            },
            CollectionType.MultiCollection => new CollectionKey
            {
                CollectionType = item.CollectionType,
                MultiCollectionId = item.MultiCollectionId
            },
            CollectionType.SmartCollection => new CollectionKey
            {
                CollectionType = item.CollectionType,
                SmartCollectionId = item.SmartCollectionId
            },
            CollectionType.FakeCollection => new CollectionKey
            {
                CollectionType = item.CollectionType
            },
            CollectionType.FakePlaylistItem => new CollectionKey
            {
                CollectionType = item.CollectionType,
                CollectionId = item.CollectionId
            },
            CollectionType.Movie => new CollectionKey
            {
                CollectionType = item.CollectionType,
                MediaItemId = item.MediaItemId
            },
            CollectionType.Episode => new CollectionKey
            {
                CollectionType = item.CollectionType,
                MediaItemId = item.MediaItemId
            },
            CollectionType.MusicVideo => new CollectionKey
            {
                CollectionType = item.CollectionType,
                MediaItemId = item.MediaItemId
            },
            CollectionType.OtherVideo => new CollectionKey
            {
                CollectionType = item.CollectionType,
                MediaItemId = item.MediaItemId
            },
            CollectionType.Song => new CollectionKey
            {
                CollectionType = item.CollectionType,
                MediaItemId = item.MediaItemId
            },
            CollectionType.Image => new CollectionKey
            {
                CollectionType = item.CollectionType,
                MediaItemId = item.MediaItemId
            },
            _ => throw new ArgumentOutOfRangeException(nameof(item))
        };

    public static CollectionKey ForBlockItem(BlockItem item) =>
        item.CollectionType switch
        {
            CollectionType.Collection => new CollectionKey
            {
                CollectionType = item.CollectionType,
                CollectionId = item.CollectionId
            },
            CollectionType.TelevisionShow => new CollectionKey
            {
                CollectionType = item.CollectionType,
                MediaItemId = item.MediaItemId
            },
            CollectionType.TelevisionSeason => new CollectionKey
            {
                CollectionType = item.CollectionType,
                MediaItemId = item.MediaItemId
            },
            CollectionType.Artist => new CollectionKey
            {
                CollectionType = item.CollectionType,
                MediaItemId = item.MediaItemId
            },
            CollectionType.MultiCollection => new CollectionKey
            {
                CollectionType = item.CollectionType,
                MultiCollectionId = item.MultiCollectionId
            },
            CollectionType.SmartCollection => new CollectionKey
            {
                CollectionType = item.CollectionType,
                SmartCollectionId = item.SmartCollectionId
            },
            CollectionType.FakeCollection => new CollectionKey
            {
                CollectionType = item.CollectionType
            },
            CollectionType.Movie => new CollectionKey
            {
                CollectionType = item.CollectionType,
                MediaItemId = item.MediaItemId
            },
            CollectionType.Episode => new CollectionKey
            {
                CollectionType = item.CollectionType,
                MediaItemId = item.MediaItemId
            },
            CollectionType.MusicVideo => new CollectionKey
            {
                CollectionType = item.CollectionType,
                MediaItemId = item.MediaItemId
            },
            CollectionType.OtherVideo => new CollectionKey
            {
                CollectionType = item.CollectionType,
                MediaItemId = item.MediaItemId
            },
            CollectionType.Song => new CollectionKey
            {
                CollectionType = item.CollectionType,
                MediaItemId = item.MediaItemId
            },
            CollectionType.Image => new CollectionKey
            {
                CollectionType = item.CollectionType,
                MediaItemId = item.MediaItemId
            },
            _ => throw new ArgumentOutOfRangeException(nameof(item))
        };

    public static CollectionKey ForDecoDefaultFiller(Deco deco) =>
        deco.DefaultFillerCollectionType switch
        {
            CollectionType.Collection => new CollectionKey
            {
                CollectionType = deco.DefaultFillerCollectionType,
                CollectionId = deco.DefaultFillerCollectionId
            },
            CollectionType.TelevisionShow => new CollectionKey
            {
                CollectionType = deco.DefaultFillerCollectionType,
                MediaItemId = deco.DefaultFillerMediaItemId
            },
            CollectionType.TelevisionSeason => new CollectionKey
            {
                CollectionType = deco.DefaultFillerCollectionType,
                MediaItemId = deco.DefaultFillerMediaItemId
            },
            CollectionType.Artist => new CollectionKey
            {
                CollectionType = deco.DefaultFillerCollectionType,
                MediaItemId = deco.DefaultFillerMediaItemId
            },
            CollectionType.MultiCollection => new CollectionKey
            {
                CollectionType = deco.DefaultFillerCollectionType,
                MultiCollectionId = deco.DefaultFillerMultiCollectionId
            },
            CollectionType.SmartCollection => new CollectionKey
            {
                CollectionType = deco.DefaultFillerCollectionType,
                SmartCollectionId = deco.DefaultFillerSmartCollectionId
            },
            CollectionType.FakeCollection => new CollectionKey
            {
                CollectionType = deco.DefaultFillerCollectionType
            },
            CollectionType.Movie => new CollectionKey
            {
                CollectionType = deco.DefaultFillerCollectionType,
                MediaItemId = deco.DefaultFillerMediaItemId
            },
            CollectionType.Episode => new CollectionKey
            {
                CollectionType = deco.DefaultFillerCollectionType,
                MediaItemId = deco.DefaultFillerMediaItemId
            },
            CollectionType.MusicVideo => new CollectionKey
            {
                CollectionType = deco.DefaultFillerCollectionType,
                MediaItemId = deco.DefaultFillerMediaItemId
            },
            CollectionType.OtherVideo => new CollectionKey
            {
                CollectionType = deco.DefaultFillerCollectionType,
                MediaItemId = deco.DefaultFillerMediaItemId
            },
            CollectionType.Song => new CollectionKey
            {
                CollectionType = deco.DefaultFillerCollectionType,
                MediaItemId = deco.DefaultFillerMediaItemId
            },
            CollectionType.Image => new CollectionKey
            {
                CollectionType = deco.DefaultFillerCollectionType,
                MediaItemId = deco.DefaultFillerMediaItemId
            },
            _ => throw new ArgumentOutOfRangeException(nameof(deco))
        };

    public static CollectionKey ForScheduleItem(ProgramScheduleItem item) =>
        item.CollectionType switch
        {
            CollectionType.Collection => new CollectionKey
            {
                CollectionType = item.CollectionType,
                CollectionId = item.CollectionId,
                FakeCollectionKey = item.FakeCollectionKey
            },
            CollectionType.TelevisionShow => new CollectionKey
            {
                CollectionType = item.CollectionType,
                MediaItemId = item.MediaItemId,
                FakeCollectionKey = item.FakeCollectionKey
            },
            CollectionType.TelevisionSeason => new CollectionKey
            {
                CollectionType = item.CollectionType,
                MediaItemId = item.MediaItemId,
                FakeCollectionKey = item.FakeCollectionKey
            },
            CollectionType.Artist => new CollectionKey
            {
                CollectionType = item.CollectionType,
                MediaItemId = item.MediaItemId,
                FakeCollectionKey = item.FakeCollectionKey
            },
            CollectionType.MultiCollection => new CollectionKey
            {
                CollectionType = item.CollectionType,
                MultiCollectionId = item.MultiCollectionId,
                FakeCollectionKey = item.FakeCollectionKey
            },
            CollectionType.SmartCollection => new CollectionKey
            {
                CollectionType = item.CollectionType,
                SmartCollectionId = item.SmartCollectionId,
                FakeCollectionKey = item.FakeCollectionKey
            },
            CollectionType.Playlist => new CollectionKey
            {
                CollectionType = item.CollectionType,
                PlaylistId = item.PlaylistId,
                FakeCollectionKey = item.FakeCollectionKey
            },
            CollectionType.FakeCollection => new CollectionKey
            {
                CollectionType = item.CollectionType,
                FakeCollectionKey = item.FakeCollectionKey
            },
            _ => throw new ArgumentOutOfRangeException(nameof(item))
        };

    public static CollectionKey ForFillerPreset(FillerPreset filler) =>
        filler.CollectionType switch
        {
            CollectionType.Collection => new CollectionKey
            {
                CollectionType = filler.CollectionType,
                CollectionId = filler.CollectionId
            },
            CollectionType.TelevisionShow => new CollectionKey
            {
                CollectionType = filler.CollectionType,
                MediaItemId = filler.MediaItemId
            },
            CollectionType.TelevisionSeason => new CollectionKey
            {
                CollectionType = filler.CollectionType,
                MediaItemId = filler.MediaItemId
            },
            CollectionType.Artist => new CollectionKey
            {
                CollectionType = filler.CollectionType,
                MediaItemId = filler.MediaItemId
            },
            CollectionType.MultiCollection => new CollectionKey
            {
                CollectionType = filler.CollectionType,
                MultiCollectionId = filler.MultiCollectionId
            },
            CollectionType.SmartCollection => new CollectionKey
            {
                CollectionType = filler.CollectionType,
                SmartCollectionId = filler.SmartCollectionId
            },
            CollectionType.Playlist => new CollectionKey
            {
                CollectionType = filler.CollectionType,
                PlaylistId = filler.PlaylistId
            },
            _ => throw new ArgumentOutOfRangeException(nameof(filler))
        };
}
