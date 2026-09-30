using ArknightsMod.Content.Buffs;
using ArknightsMod.Content.Projectiles.Caster.Goldenglow;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Players;

public class GoldenglowBeaconPlayer : ModPlayer
{
    public override void PostUpdateBuffs()
    {
        if (Player.ownedProjectileCounts[ModContent.ProjectileType<GoldenglowBeacon>()] > 0)
            Player.AddBuff(ModContent.BuffType<GoldenglowBeaconBuff>(), 2);
    }
}
