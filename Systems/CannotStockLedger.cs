using System;
using System.Collections.Generic;
using System.Linq;

namespace ArknightsMod.Systems;

// Each boss owns its stock slots permanently. A rematch rerolls those slots only.
public sealed class CannotStockLedger
{
    public sealed class Slot(string bossKey, int tier, int itemType)
    {
        public string BossKey = bossKey;
        public int Tier = tier;
        public int ItemType = itemType;
    }

    public readonly List<Slot> Slots = [];
    public static readonly int[] Prices = [0, 2, 4, 8, 12, 16];
    public bool Unlocked => Slots.Count > 0;
    public int HighestTier => Unlocked ? Slots.Max(s => s.Tier) : 1;

    public void RecordVictory(string bossKey, int worldTier, Func<int, HashSet<int>, int, int> pick)
    {
        var owned = Slots.Where(s => s.BossKey == bossKey).ToList();
        if (owned.Count == 0) {
            int count = Unlocked ? 1 : 2;
            int tier = Unlocked ? Math.Clamp(worldTier, 2, 5) : 1;
            for (int i = 0; i < count; i++) {
                var slot = new Slot(bossKey, tier, 0);
                Slots.Add(slot);
                owned.Add(slot);
            }
        }
        RerollSlots(owned, pick);
    }

    public void RerollAll(Func<int, HashSet<int>, int, int> pick) => RerollSlots(Slots, pick);

    private void RerollSlots(IEnumerable<Slot> slots, Func<int, HashSet<int>, int, int> pick)
    {
        foreach (Slot slot in slots) {
            HashSet<int> used = Slots.Where(s => s != slot && s.ItemType > 0).Select(s => s.ItemType).ToHashSet();
            slot.ItemType = pick(slot.Tier, used, slot.ItemType);
        }
    }
}
