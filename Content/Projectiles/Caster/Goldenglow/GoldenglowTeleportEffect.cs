using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.Goldenglow;

public sealed class GoldenglowTeleportEffect : ModProjectile
{
    public override string Texture => "ArknightsMod/Assets/Effects/Goldenglow/LightningImpact";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2;
        Projectile.timeLeft = 18;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.netImportant = true;
    }
    public override bool? CanDamage() => false;
    public override bool ShouldUpdatePosition() => false;
    public override void AI()
    {
        if (Main.dedServ) return;
        Lighting.AddLight(Projectile.Center, 0.2f, 0.45f, 0.8f);
        if (Projectile.localAI[0]++ != 0) return;
        bool arrival = Projectile.ai[0] > 0;
        for (int i = 0; i < 18; i++)
        {
            Vector2 radial = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * i / 18f);
            Dust dust = Dust.NewDustPerfect(Projectile.Center + radial * (arrival ? 5f : 24f),
                DustID.Electric, radial * (arrival ? 3f : -2.5f), 80, Color.LightSkyBlue, 1.1f);
            dust.noGravity = true;
        }
    }
    public override bool PreDraw(ref Color lightColor)
    {
        float life = Projectile.timeLeft / 18f;
        GoldenglowLightningRenderer.DrawFlare(Projectile.Center, new Color(100, 185, 255),
            0.18f + (1f - life) * 0.45f, life);
        return false;
    }
}
