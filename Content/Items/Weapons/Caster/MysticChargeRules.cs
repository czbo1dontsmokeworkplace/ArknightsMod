using System;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Caster;

// One shared contract for all three Mystic staves and their coordinated-release path.
internal static class MysticChargeRules
{
    internal const int Capacity = 3;
    internal static int ChargeTicks(Player player) => Math.Max(1,
        CombinedHooks.TotalUseTime(player.HeldItem.useTime, player, player.HeldItem));
    internal static float DamageMultiplier(int ordinal) => 2f + Math.Clamp(ordinal, 0, 2);
    internal static float VisualMultiplier(int ordinal) => 1.5f + .5f * Math.Clamp(ordinal, 0, 2);
    // ai[0] 0 = normal, 1 = legacy stored, 2 = echo; 3/4/5 preserve the new stored ordinal over the network.
    internal static int StoredMode(int ordinal) => 3 + Math.Clamp(ordinal, 0, 2);
    internal static bool IsStored(Projectile projectile) => projectile.ai[0] == 1 || projectile.ai[0] is >= 3 and <= 5;
    internal static float VisualScale(Projectile projectile) => projectile.ai[0] is >= 3 and <= 5
        ? VisualMultiplier((int)projectile.ai[0] - 3) : 1f;
    internal static float BurstScale(Projectile projectile) => projectile.ai[2] > 0 ? Math.Clamp(projectile.ai[2], 1, 2.5f) : 1f;
    internal static bool Accumulate(Player player, ref int charges, ref int clock)
    {
        if (player.itemAnimation > 0 || player.controlUseItem || charges >= Capacity) { clock = 0; return false; }
        if (++clock < ChargeTicks(player)) return false;
        clock = 0; charges++; return true;
    }
    internal static float Progress(Player player, int charges, int clock, bool justCharged)
        => charges >= Capacity || justCharged ? 1 : Math.Clamp(clock / (float)ChargeTicks(player), 0, 1);
}
