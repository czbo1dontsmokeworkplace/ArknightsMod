using ArknightsMod.Content.Items.Weapons.Guard.Crusher;
using ArknightsMod.Content.Projectiles.Charged;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.Graphics.CameraModifiers;

namespace ArknightsMod.Content.Projectiles.Guard.Crusher;

public sealed class CrusherImpact : ModProjectile
{
    public override string Texture => "Terraria/Images/Projectile_0";
    public override void SetDefaults() { Projectile.width = Projectile.height = 2; Projectile.tileCollide = false; Projectile.ignoreWater = true; Projectile.timeLeft = 32; }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;
    public override void AI()
    {
        if (Projectile.localAI[0]++ == 0) {
            CrusherVisuals.Impact(Projectile.Center, Projectile.velocity.SafeNormalize(Vector2.UnitX), (CrusherKind)(int)Projectile.ai[0], Projectile.ai[1]);
            if (!Main.dedServ && Projectile.owner == Main.myPlayer && Projectile.ai[1] >= 1.3f)
                Main.instance?.CameraModifiers?.Add(new PunchCameraModifier(Projectile.Center,
                    Projectile.velocity.SafeNormalize(-Vector2.UnitY), 4 * Projectile.ai[1], 7, 9, 850, "CrusherContact"));
        }
    }
    public override bool PreDraw(ref Color lightColor)
    {
        var kind = (CrusherKind)(int)Projectile.ai[0];
        float progress = 1 - Projectile.timeLeft / 32f, strength = Projectile.ai[1];
        float fade = (1 - progress) * (1 - progress);
        Vector2 point = Projectile.Center;
        float rotation = Projectile.velocity.SafeNormalize(Vector2.UnitX).ToRotation();
        CrusherMaterial.DrawImpact(point, Projectile.velocity.SafeNormalize(Vector2.UnitX), kind, progress, strength);
        if (!CrusherVisuals.Visible(point)) return false;
        using (new ChargedOperatorVfx.Pass(true)) {
            float flash = System.Math.Clamp(1 - progress * 5, 0, 1);
            ChargedOperatorVfx.Sprite("RuptureRing", point, new Vector2(38 + progress * 205, 28 + progress * 140) * strength,
                CrusherRules.Edge(kind) * (fade * .95f), rotation);
            ChargedOperatorVfx.Sprite("LinearSpark", point, new Vector2(165, 24) * strength,
                Color.White * flash, rotation + MathHelper.PiOver2);
            ChargedOperatorVfx.Sprite("StarCore", point, new Vector2(88, 55) * strength,
                Color.White * flash, rotation);
            // A short dense impact face opens into a colored tear; the later ring remains soft.
            ChargedOperatorVfx.Sprite("ImpactPetals", point, new Vector2(110, 88) * strength * (.65f + progress),
                CrusherRules.Tint(kind) * (fade * .8f), rotation + progress * .4f);
            ChargedOperatorVfx.Sprite("ForwardFlame", point, new Vector2(125, 43) * strength,
                CrusherRules.Edge(kind) * (fade * .7f), rotation);
            ChargedOperatorVfx.Sprite("StarCore", point, new Vector2(58) * strength * (1 - progress * .55f),
                CrusherRules.Edge(kind) * (fade * .8f), rotation);
        }
        return false;
    }
}
