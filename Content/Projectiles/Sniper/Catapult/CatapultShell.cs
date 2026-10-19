using ArknightsMod.Content.Projectiles.Sniper.Fiammetta;
using ArknightsMod.Content.Projectiles.Sniper.Artillery;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Sniper.Catapult;

/// <summary>与后续炮手共用高空跨射；ai[2]仅快照空爆的技能爆炸半径。</summary>
public sealed class CatapultShell : FiammettaMortarShell
{
    protected override int FlightMode => 0;
    protected override Color TrailOuter => new(244, 132, 30);
    protected override Color TrailInner => new(255, 237, 160);
    protected override float TrailWidth => 17f;

    protected override void SpawnExplosion(int damage)
    {
        int blast = Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Center, Vector2.Zero,
            ModContent.ProjectileType<CatapultExplosion>(), damage, Projectile.knockBack, Projectile.owner,
            MathHelper.Clamp(Projectile.ai[2], ArtilleryBlast.Radius, ArtilleryBlast.Radius * 2f));
        if (Main.projectile.IndexInRange(blast)) Main.projectile[blast].CritChance = Projectile.CritChance;
    }
}
