using ArknightsMod.Common.VisualEffects;
using ArknightsMod.Systems.Gameplay.Damage;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Defender;

// Shalem's blood-price volley. Each shard owns one hit and never changes its source after firing.
public sealed class DefenderArtsShard : ModProjectile
{
    public override string Texture => "Terraria/Images/MagicPixel";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 8; Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Magic; Projectile.penetrate = 1; Projectile.timeLeft = 90;
    }
    public override void AI()
    {
        Projectile.GetGlobalProjectile<ArtsProjectileMarker>().IsArtsDamage = true;
        int index = (int)Projectile.ai[0];
        if (Main.npc.IndexInRange(index) && Main.npc[index].CanBeChasedBy(Projectile))
        {
            NPC target = Main.npc[index];
            if (Collision.CanHitLine(Projectile.Center, 1, 1, target.position, target.width, target.height))
                Projectile.velocity = Vector2.Lerp(Projectile.velocity,
                    (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX) * 14f, .12f);
        }
        if (Projectile.timeLeft % 3 == 0 && !Main.dedServ)
            OperatorTextureEffects.Mote(Projectile.Center, -Projectile.velocity * .1f, new Color(185, 90, 145), 7f, 18);
    }
    public override void OnKill(int timeLeft) => DefenderSkillVisuals.Burst(Projectile.Center, new Color(185, 90, 145), 4);
    public override bool PreDraw(ref Color lightColor)
    {
        OperatorTextureEffects.Glow(Projectile.Center, 16f, new Color(230, 130, 190), .7f);
        return false;
    }
}
