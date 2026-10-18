using System;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Medic;

// Shared by the player's field staves, so switching items or using another copy cannot skip the cooldown.
public sealed class MedicCastPlayer : ModPlayer
{
    internal int GroupCooldown { get; private set; }

    internal bool TryStartGroupCast(int interval)
    {
        if (GroupCooldown > 0) return false;
        GroupCooldown = Math.Max(1, interval);
        return true;
    }

    public override void PostUpdate() => TickCooldown();
    public override void UpdateDead() => TickCooldown();

    private void TickCooldown()
    {
        if (GroupCooldown > 0) GroupCooldown--;
    }
}
