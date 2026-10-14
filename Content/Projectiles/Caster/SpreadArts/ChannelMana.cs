using Terraria;

namespace ArknightsMod.Content.Projectiles.Caster.SpreadArts;

internal static class ChannelMana
{
    // Initial item use pays its normal cost. A held cast then pays once per
    // useTime while channeling, through Terraria's Mana Flower aware API.
    internal static bool Sustain(Player owner, Projectile focus)
    {
        if (++focus.localAI[1] < System.Math.Max(1, owner.HeldItem.useTime)) return true;
        focus.localAI[1] = 0f;
        return owner.CheckMana(owner.HeldItem, -1, true, false);
    }
}
