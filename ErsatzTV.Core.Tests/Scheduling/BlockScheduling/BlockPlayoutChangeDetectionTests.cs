using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Core.Scheduling;
using ErsatzTV.Core.Scheduling.BlockScheduling;
using Newtonsoft.Json;
using NUnit.Framework;
using Shouldly;

namespace ErsatzTV.Core.Tests.Scheduling.BlockScheduling;

public static class BlockPlayoutChangeDetectionTests
{
    [TestFixture]
    public class FindUpdatedItems
    {
        // takes playout items, item block keys and effective blocks
        // returns blocks to schedule and playout items to remove

        // test case: nothing has changed

        // test case: block has moved from one time to another time, nothing after
        // test case: block has moved from one time to another time, same block after
        // test case: block has moved from one time to another time, different block with same collection after
        // test case: block was moved from one time to another time, different block with different collection after

        // test case: block was removed, nothing after
        // test case: block was removed, same block after
        // test case: block was removed, different block with same collection after
        // test case: block was removed, different block with different collection after

        // test case: block was added, nothing after
        // test case: block was added, same block after
        // test case: block was added, different block with same collection after
        // test case: block was added, different block with different collection after


        [Test]
        public void Should_Work_When_Nothing_Has_Changed()
        {
            DateTimeOffset dateUpdated = DateTimeOffset.Now;
            List<Block> blocks = Blocks(dateUpdated);
            var template = new Template { Id = 1, DateUpdated = dateUpdated.UtcDateTime };
            var playoutTemplate = new PlayoutTemplate { Id = 10, DateUpdated = dateUpdated.UtcDateTime };

            Block block1 = blocks[0];
            Block block2 = blocks[1];
            var blockKey1 = new BlockKey(block1, template, playoutTemplate);
            var blockKey2 = new BlockKey(block2, template, playoutTemplate);
            var collectionKey1 = CollectionKey.ForBlockItem(block1.Items.Head());
            var collectionKey2 = CollectionKey.ForBlockItem(block2.Items.Head());

            // 9am-9:20am
            PlayoutItem playoutItem1 = PlayoutItem(blockKey1, collectionKey1, GetLocalDate(2024, 1, 17).AddHours(9));
            // 1pm-1:20pm
            PlayoutItem playoutItem2 = PlayoutItem(blockKey2, collectionKey2, GetLocalDate(2024, 1, 17).AddHours(13));

            List<PlayoutItem> playoutItems = [playoutItem1, playoutItem2];
            Dictionary<PlayoutItem, BlockKey> itemBlockKeys =
                new()
                {
                    { playoutItem1, blockKey1 },
                    { playoutItem2, blockKey2 }
                };

            List<EffectiveBlock> effectiveBlocks =
            [
                new(block1, blockKey1, GetLocalDate(2024, 1, 17).AddHours(9), 1),
                new(block2, blockKey2, GetLocalDate(2024, 1, 17).AddHours(13), 2)
            ];

            Map<CollectionKey, string> collectionEtags = LanguageExt.Map<CollectionKey, string>.Empty;
            collectionEtags = collectionEtags.Add(collectionKey1, JsonConvert.SerializeObject(collectionKey1));
            collectionEtags = collectionEtags.Add(collectionKey2, JsonConvert.SerializeObject(collectionKey2));

            var buildStart = GetLocalDate(2024, 1, 17);
            Tuple<List<EffectiveBlock>, List<PlayoutItem>> result = BlockPlayoutChangeDetection.FindUpdatedItems(
                buildStart,
                playoutItems,
                itemBlockKeys,
                effectiveBlocks,
                collectionEtags);

            // nothing to schedule
            result.Item1.Count.ShouldBe(0);

            // do not need to remove any playout items or history
            result.Item2.Count.ShouldBe(0);
        }

        [Test]
        public void Should_Not_Remove_Items_From_Outside_The_Scheduling_Window()
        {
            // This test demonstrates a bug where playout items from the past are removed
            // because their generating block is not in the current scheduling window.

            DateTimeOffset dateUpdated = DateTimeOffset.Now;
            List<Block> blocks = Blocks(dateUpdated);
            var template1 = new Template { Id = 1, DateUpdated = dateUpdated.UtcDateTime };
            var playoutTemplate1 = new PlayoutTemplate { Id = 10, DateUpdated = dateUpdated.UtcDateTime };

            var template2 = new Template { Id = 2, DateUpdated = dateUpdated.UtcDateTime };
            var playoutTemplate2 = new PlayoutTemplate { Id = 20, DateUpdated = dateUpdated.UtcDateTime };

            Block block1 = blocks[0]; // Yesterday's block
            Block block2 = blocks[1]; // Today's block

            var blockKey1 = new BlockKey(block1, template1, playoutTemplate1);
            var blockKey2 = new BlockKey(block2, template2, playoutTemplate2);

            var collectionKey1 = CollectionKey.ForBlockItem(block1.Items.Head());
            var collectionKey2 = CollectionKey.ForBlockItem(block2.Items.Head());

            // Playout item from yesterday
            PlayoutItem playoutItem1 = PlayoutItem(blockKey1, collectionKey1, GetLocalDate(2024, 1, 17).AddHours(9));

            List<PlayoutItem> playoutItems = [playoutItem1];
            Dictionary<PlayoutItem, BlockKey> itemBlockKeys =
                new()
                {
                    { playoutItem1, blockKey1 }
                };

            // Effective blocks for today - does not include yesterday's block
            List<EffectiveBlock> effectiveBlocks =
            [
                new(block2, blockKey2, GetLocalDate(2024, 1, 18).AddHours(9), 1)
            ];

            Map<CollectionKey, string> collectionEtags = LanguageExt.Map<CollectionKey, string>.Empty;
            collectionEtags = collectionEtags.Add(collectionKey1, JsonConvert.SerializeObject(collectionKey1));
            collectionEtags = collectionEtags.Add(collectionKey2, JsonConvert.SerializeObject(collectionKey2));

            var buildStart = GetLocalDate(2024, 1, 18);
            Tuple<List<EffectiveBlock>, List<PlayoutItem>> result = BlockPlayoutChangeDetection.FindUpdatedItems(
                buildStart,
                playoutItems,
                itemBlockKeys,
                effectiveBlocks,
                collectionEtags);

            // should schedule today's block
            result.Item1.Count.ShouldBe(1);
            result.Item1.Head().Block.ShouldBe(block2);

            // should NOT remove yesterday's item
            result.Item2.Count.ShouldBe(0);
        }

        private static List<Block> Blocks(DateTimeOffset dateUpdated)
        {
            List<Block> blocks =
            [
                // SHOW A
                new()
                {
                    Id = 1,
                    Items =
                    [
                        new BlockItem
                        {
                            Id = 1,
                            Index = 1,
                            BlockId = 1,
                            CollectionType = ProgramScheduleItemCollectionType.TelevisionShow,
                            MediaItemId = 1,
                            PlaybackOrder = PlaybackOrder.Chronological
                        }
                    ],
                    DateUpdated = dateUpdated.UtcDateTime
                },
                // SHOW B
                new()
                {
                    Id = 2,
                    Items =
                    [
                        new BlockItem
                        {
                            Id = 2,
                            Index = 1,
                            BlockId = 2,
                            CollectionType = ProgramScheduleItemCollectionType.TelevisionShow,
                            MediaItemId = 2,
                            PlaybackOrder = PlaybackOrder.Chronological
                        }
                    ],
                    DateUpdated = dateUpdated.UtcDateTime
                },
                // SHOW C
                new()
                {
                    Id = 3,
                    Items =
                    [
                        new BlockItem
                        {
                            Id = 3,
                            Index = 1,
                            BlockId = 3,
                            CollectionType = ProgramScheduleItemCollectionType.TelevisionShow,
                            MediaItemId = 3,
                            PlaybackOrder = PlaybackOrder.Chronological
                        }
                    ],
                    DateUpdated = dateUpdated.UtcDateTime
                }
            ];

            foreach (Block block in blocks)
            {
                foreach (BlockItem blockItem in block.Items)
                {
                    blockItem.Block = block;
                }
            }

            return blocks;
        }

        private static PlayoutItem PlayoutItem(BlockKey blockKey, CollectionKey collectionKey, DateTimeOffset start) =>
            new()
            {
                Start = start.UtcDateTime,
                Finish = start.UtcDateTime.AddMinutes(20),
                BlockKey = JsonConvert.SerializeObject(blockKey),
                CollectionKey = JsonConvert.SerializeObject(collectionKey),
                CollectionEtag = JsonConvert.SerializeObject(collectionKey)
            };

        private static DateTimeOffset GetLocalDate(int year, int month, int day) =>
            new(year, month, day, 0, 0, 0, TimeSpan.FromHours(-6));
    }
}
