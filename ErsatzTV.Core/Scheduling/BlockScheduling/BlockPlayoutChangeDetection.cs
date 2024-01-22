using System.Collections.Immutable;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Scheduling;
using Newtonsoft.Json;

namespace ErsatzTV.Core.Scheduling.BlockScheduling;

internal static class BlockPlayoutChangeDetection
{
    public static Dictionary<PlayoutItem, BlockKey> GetPlayoutItemToBlockKeyMap(Playout playout)
    {
        var itemBlockKeys = new Dictionary<PlayoutItem, BlockKey>();
        foreach (PlayoutItem item in playout.Items)
        {
            if (!string.IsNullOrWhiteSpace(item.BlockKey))
            {
                BlockKey blockKey = JsonConvert.DeserializeObject<BlockKey>(item.BlockKey);
                itemBlockKeys.Add(item, blockKey);
            }
        }

        return itemBlockKeys.ToDictionary();
    }

    public static Tuple<List<EffectiveBlock>, List<PlayoutItem>> FindUpdatedItems(
        List<PlayoutItem> playoutItems,
        Dictionary<PlayoutItem, BlockKey> itemBlockKeys,
        List<EffectiveBlock> blocksToSchedule,
        Map<CollectionKey, string> collectionEtags)
    {
        DateTimeOffset lastScheduledItem = playoutItems.Count == 0
            ? SystemTime.MinValueUtc
            : playoutItems.Max(i => i.StartOffset);

        var existingBlockKeys = itemBlockKeys.Values.ToImmutableHashSet();
        var blockKeysToSchedule = blocksToSchedule.Map(b => b.BlockKey).ToImmutableHashSet();
        var updatedBlocks = new System.Collections.Generic.HashSet<EffectiveBlock>();
        var updatedItemIds = new System.Collections.Generic.HashSet<int>();

        var earliestEffectiveBlocks = new Dictionary<BlockKey, DateTimeOffset>();
        var earliestBlocks = new Dictionary<int, DateTimeOffset>();
        
        // check for changed collections
        foreach (EffectiveBlock effectiveBlock in blocksToSchedule.OrderBy(b => b.Start))
        {
            foreach (PlayoutItem playoutItem in playoutItems)
            {
                int blockId = itemBlockKeys[playoutItem].b;
                if (effectiveBlock.Block.Id != blockId)
                {
                    continue;
                }

                bool isUpdated = string.IsNullOrWhiteSpace(playoutItem.CollectionKey);
                if (!isUpdated)
                {
                    CollectionKey collectionKey = JsonConvert.DeserializeObject<CollectionKey>(playoutItem.CollectionKey);

                    // collection is no longer present or collection has been modified
                    isUpdated = !collectionEtags.ContainsKey(collectionKey) ||
                                collectionEtags[collectionKey] != playoutItem.CollectionEtag;
                }
                
                if (isUpdated)
                {
                    // playout item needs to be removed/re-added
                    updatedItemIds.Add(playoutItem.Id);
                    
                    // block needs to be scheduled again
                    updatedBlocks.Add(effectiveBlock);

                    if (!earliestEffectiveBlocks.ContainsKey(effectiveBlock.BlockKey))
                    {
                        earliestEffectiveBlocks[effectiveBlock.BlockKey] = effectiveBlock.Start;
                    }

                    if (!earliestBlocks.ContainsKey(effectiveBlock.Block.Id))
                    {
                        earliestBlocks[effectiveBlock.Block.Id] = effectiveBlock.Start;
                    }
                }
            }
        }

        // process in sorted order to simplify checks
        foreach (EffectiveBlock effectiveBlock in blocksToSchedule.OrderBy(b => b.Start))
        {
            // future blocks always need to be scheduled
            if (effectiveBlock.Start > lastScheduledItem)
            {
                updatedBlocks.Add(effectiveBlock);
            }
            // if block key is not present in existingBlockKeys, the effective block is new or updated
            else if (!existingBlockKeys.Contains(effectiveBlock.BlockKey))
            {
                updatedBlocks.Add(effectiveBlock);

                if (!earliestEffectiveBlocks.ContainsKey(effectiveBlock.BlockKey))
                {
                    earliestEffectiveBlocks[effectiveBlock.BlockKey] = effectiveBlock.Start;
                }

                if (!earliestBlocks.ContainsKey(effectiveBlock.Block.Id))
                {
                    earliestBlocks[effectiveBlock.Block.Id] = effectiveBlock.Start;
                }
            }
            // if id is present, the block has been modified earlier, so this effective block also needs to update
            else if (earliestBlocks.ContainsKey(effectiveBlock.Block.Id))
            {
                updatedBlocks.Add(effectiveBlock);
            }
        }

        foreach ((BlockKey key, DateTimeOffset value) in earliestEffectiveBlocks)
        {
            Serilog.Log.Logger.Debug("Earliest effective block: {Key} => {Value}", key, value);
        }

        foreach ((int blockId, DateTimeOffset value) in earliestBlocks)
        {
            Serilog.Log.Logger.Debug("Earliest block id: {Id} => {Value}", blockId, value);
        }

        // find affected playout items
        foreach (PlayoutItem item in playoutItems)
        {
            BlockKey blockKey = itemBlockKeys[item];

            bool blockKeyIsAffected = earliestEffectiveBlocks.TryGetValue(blockKey, out DateTimeOffset value) &&
                                      value <= item.StartOffset;

            bool blockIdIsAffected = earliestBlocks.TryGetValue(blockKey.b, out DateTimeOffset value2) &&
                                     value2 <= item.StartOffset; 
            
            if (!blockKeysToSchedule.Contains(blockKey) || blockKeyIsAffected || blockIdIsAffected)
            {
                updatedItemIds.Add(item.Id);
            }
        }

        return Tuple(updatedBlocks.ToList(), playoutItems.Filter(i => updatedItemIds.Contains(i.Id)).ToList());
    }

    public static void RemoveItemAndHistory(Playout playout, PlayoutItem playoutItem)
    {
        playout.Items.Remove(playoutItem);

        Option<PlayoutHistory> historyToRemove = playout.PlayoutHistory
            .Find(h => h.When == playoutItem.Start);

        foreach (PlayoutHistory history in historyToRemove)
        {
            playout.PlayoutHistory.Remove(history);
        }
    }
}
