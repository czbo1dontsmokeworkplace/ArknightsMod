using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.BossArsenal;

public sealed class BasaltRollingMeteor : ModProjectile
{
    public override string Texture => "ArknightsMod/Content/Projectiles/PmpSlugEggFly";
    public override void SetDefaults() {
        Projectile.width = Projectile.height = 20; Projectile.friendly = true; Projectile.DamageType = DamageClass.Melee;
        Projectile.penetrate = -1; Projectile.timeLeft = 180; Projectile.tileCollide = true; Projectile.ignoreWater = true;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = 15;
    }
    public override void AI() {
        Projectile.velocity.Y = Math.Min(12, Projectile.velocity.Y + .25f);
        Projectile.rotation += Projectile.velocity.X * .045f;
        Projectile.Opacity = Math.Min(1, Projectile.timeLeft / 25f);
        if (!Main.dedServ) {
            Lighting.AddLight(Projectile.Center, new Vector3(.7f, .2f, .03f) * Projectile.Opacity);
            if (Projectile.timeLeft % 2 == 0) {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Torch, -Projectile.velocity * .12f, 80, default, 1.1f); d.noGravity = true;
            }
        }
    }
    public override bool OnTileCollide(Vector2 oldVelocity) {
        Projectile.ai[0] = 1;
        if (oldVelocity.Y > 0 && Projectile.velocity.Y != oldVelocity.Y) Projectile.velocity.Y = 0;
        if (Projectile.velocity.X != oldVelocity.X) Projectile.velocity.X = -oldVelocity.X * .8f;
        else Projectile.velocity.X = oldVelocity.X * .998f;
        Projectile.netUpdate = true; return false;
    }
    public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
    { fallThrough = false; return true; }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
        target.AddBuff(BuffID.OnFire, 180);
        if (Projectile.owner == Main.myPlayer) Projectile.NewProjectile(Projectile.GetSource_OnHit(target), target.Center, Vector2.Zero,
            ModContent.ProjectileType<BasaltImpactFlash>(), 0, 0, Projectile.owner);
    }
    public override bool PreDraw(ref Color lightColor) {
        string art = Projectile.ai[0] == 0 ? Texture : "ArknightsMod/Content/Projectiles/PmpSlugEggLand";
        Texture2D body = ModContent.Request<Texture2D>(art).Value;
        Main.EntitySpriteDraw(body, Projectile.Center - Main.screenPosition, null, Color.White * Projectile.Opacity,
            Projectile.rotation, body.Size() * .5f, 1, SpriteEffects.None); return false;
    }
}

public sealed class BasaltImpactFlash : ModProjectile
{
    public override string Texture => "Terraria/Images/MagicPixel";
    public override void SetDefaults() { Projectile.width = Projectile.height = 2; Projectile.timeLeft = 18; Projectile.tileCollide = false; }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;
    public override void AI() {
        if (Projectile.timeLeft != 18 || Main.dedServ) return;
        for (int i = 0; i < 20; i++) {
            var dust = Dust.NewDustPerfect(Projectile.Center, DustID.FlameBurst, Main.rand.NextVector2Circular(5, 5), 50, default, 1.4f);
            dust.noGravity = true;
        }
    }
    public override bool PreDraw(ref Color lightColor) {
        float t = 1 - Projectile.timeLeft / 18f;
        BossArsenalFx.Ring(Projectile.Center, 55 * MathF.Sqrt(t), Color.Orange, 1 - t); return false;
    }
}
