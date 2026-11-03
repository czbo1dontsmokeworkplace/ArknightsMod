using System;
using ArknightsMod.Content.Items.Weapons.Charged;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Charged;

public sealed class DreadnoughtMomentFlash : ModProjectile
{
    public override string Texture => "Terraria/Images/MagicPixel";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2;
        Projectile.timeLeft = 24;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;
    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];
        if (!owner.active || owner.dead) { Projectile.Kill(); return; }
        Projectile.Center = owner.Top + new Vector2(0, -12);
        if (Projectile.localAI[0]++ == 0 && !Main.dedServ) {
            SoundEngine.PlaySound(SoundID.MaxMana with { Volume = .85f, Pitch = .12f, MaxInstances = 3 }, Projectile.Center);
            Color tint = ChargedOperatorRules.ColorFor((ChargedOperatorKind)(int)Projectile.ai[0]);
            for (int i = 0; i < 8; i++) {
                Vector2 ray = (i * MathHelper.PiOver4).ToRotationVector2();
                ChargedOperatorVfx.Sparkle(Projectile.Center + ray * 8, ray * 1.2f,
                    Color.Lerp(tint, Color.White, .6f), ray.ToRotation(), .85f);
            }
        }
    }
    public override bool PreDraw(ref Color lightColor)
    {
        float t = 1 - Projectile.timeLeft / 24f;
        float flare = MathF.Sin(MathHelper.Pi * MathF.Pow(t, .4f));
        Color color = ChargedOperatorRules.ColorFor((ChargedOperatorKind)(int)Projectile.ai[0]);
        using var pass = new ChargedOperatorVfx.Pass(true);
        ChargedOperatorVfx.Sprite("LinearSpark", Projectile.Center, new Vector2(15, 90) * flare, Color.White * flare);
        ChargedOperatorVfx.Sprite("LinearSpark", Projectile.Center, new Vector2(10, 70) * flare,
            Color.Lerp(color, Color.White, .5f) * flare, MathHelper.PiOver2);
        ChargedOperatorVfx.Sprite("RuptureRing", Projectile.Center, new Vector2(20 + 60 * t), color * ((1 - t) * .6f));
        return false;
    }
}
