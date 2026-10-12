using System;

namespace ArknightsMod.Systems;

public static class EndfieldGatheringRules
{
    public const int RequiredAmount = 3;

    // 先核对整份库存，再分配各叠扣除量。不足时完全不扣料，避免只扣第一叠就发奖。
    public static bool TryPlanConsumption(ReadOnlySpan<int> stacks, int required, Span<int> consumed)
    {
        if (consumed.Length != stacks.Length || required <= 0)
            return false;
        consumed.Clear();
        long available = 0;
        foreach (int stack in stacks)
            available += Math.Max(0, stack);
        if (available < required)
            return false;
        int remaining = required;
        for (int i = 0; i < stacks.Length && remaining > 0; i++)
        {
            consumed[i] = Math.Min(Math.Max(0, stacks[i]), remaining);
            remaining -= consumed[i];
        }
        return remaining == 0;
    }
}
