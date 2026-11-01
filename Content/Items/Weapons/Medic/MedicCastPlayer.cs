using System;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Medic;

// One fixed real-time cooldown shared by every healing branch and automatic casts.
public sealed class MedicCastPlayer : ModPlayer
{
    internal const int CooldownTicks = 180;
    internal int Cooldown { get; private set; }

    internal bool TryStartCast()
    {
        if (Cooldown > 0) return false;
        Cooldown = CooldownTicks;
        return true;
    }

    public override void PostUpdate() => TickCooldown();
    public override void UpdateDead() => TickCooldown();

    private void TickCooldown()
    {
        if (Cooldown > 0) Cooldown--;
    }
}
