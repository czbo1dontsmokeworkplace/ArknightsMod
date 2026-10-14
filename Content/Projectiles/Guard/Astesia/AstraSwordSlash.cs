using ArknightsMod.Content.Items.Weapons.Guard.Astesia;
using ArknightsMod.Content.Projectiles.Guard.ArtsFighter;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Astesia;

public class AstraSwordSlash : ArtsFighterSlash
{
    internal static readonly Color Sky = new(85, 185, 255);
    protected override int WeaponType => ModContent.ItemType<AstraSword>();
    protected override Color SlashColor => Sky;
    protected override bool StarDetails => true;
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        base.OnHitNPC(target, hit, damageDone);
        if (Projectile.numHits < 4)
            AstraImpactSparkles.Burst(target.Hitbox.ClosestPointInRect(Projectile.Center),
                Projectile.velocity.SafeNormalize(Vector2.UnitX), Sky,
                MathHelper.Clamp(System.Math.Max(target.width, target.height) * .5f, 12f, 48f));
    }
}

public class AstraSwordHoldout : ArtsFighterHoldout
{
    public override string Texture => "ArknightsMod/Content/Items/Weapons/Guard/Astesia/AstraSword";
    protected override int WeaponType => ModContent.ItemType<AstraSword>();
    protected override int SlashType => ModContent.ProjectileType<AstraSwordSlash>();
    protected override Color BladeColor => AstraSwordSlash.Sky;
    protected override bool PhysicalSword => true;
}
