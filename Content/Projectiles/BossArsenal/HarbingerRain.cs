using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Projectiles.Charged;

namespace ArknightsMod.Content.Projectiles.BossArsenal;

// Devastation's forward shot and descending rain, rendered entirely as optical particles.
public sealed class HarbingerRain : ModProjectile
{
    public override string Texture => "Terraria/Images/MagicPixel";
    public override void SetDefaults() {
        Projectile.width = Projectile.height = 14; Projectile.friendly = true; Projectile.DamageType = DamageClass.Melee;
        Projectile.penetrate = 1; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.extraUpdates = 1; Projectile.timeLeft = 140;
    }
    public override void AI() {
        Projectile.localAI[0]++;
        Projectile.velocity.Y += .07f;
        Projectile.rotation = Projectile.velocity.ToRotation();
        if (Main.dedServ) return;
        Vector2 axis = Projectile.velocity.SafeNormalize(Vector2.UnitY);
        Vector2 normal = axis.RotatedBy(MathHelper.PiOver2);
        for (int i = 0; i < 4; i++) {
            Vector2 at = Projectile.Center - axis * (i * 7);
            Dust dust = Dust.NewDustPerfect(at, DustID.ShadowbeamStaff, -axis * Main.rand.NextFloat(.3f, 1.1f),
                30, new Color(235, 55, 95), 1.3f - i * .13f);
            dust.noGravity = true;
            if (i < 2) ChargedOperatorVfx.Sparkle(at, -axis * .5f,
                new Color(255, 100, 140), axis.ToRotation(), .7f - i * .15f);
        }
        float phase = Projectile.localAI[0] * .34f;
        for (int side = -1; side <= 1; side += 2) {
            Vector2 offset = normal * (MathF.Sin(phase) * 10 * side) + axis * (MathF.Cos(phase) * 5 * side);
            ChargedOperatorVfx.Sparkle(Projectile.Center + offset, -axis * .35f,
                side < 0 ? Color.LightPink : new Color(255, 70, 110), axis.ToRotation(), .45f);
        }
        Lighting.AddLight(Projectile.Center, .45f, .04f, .12f);
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
        float at = 0;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center - Projectile.velocity,
            Projectile.Center, 14, ref at);
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
        if (Projectile.owner == Main.myPlayer)
            HarbingerEmber.Seed(Projectile.GetSource_OnHit(target), target.Center, target.whoAmI,
                Math.Max(1, Projectile.damage / 2), Projectile.owner, 1);
    }
    public override bool PreDraw(ref Color lightColor) => false;
}

// Enforcer-inspired afterflame: linger, acquire and pursue, then detonate.
public sealed class HarbingerEmber : ModProjectile
{
    private int Age => (int)Projectile.ai[1];
    public override string Texture => "Terraria/Images/MagicPixel";
    public override void SetDefaults() {
        Projectile.width = Projectile.height = 20; Projectile.friendly = true; Projectile.DamageType = DamageClass.Melee;
        Projectile.penetrate = 1; Projectile.tileCollide = false; Projectile.ignoreWater = true; Projectile.timeLeft = 180;
        Projectile.netImportant = true;
    }
    internal static void Seed(IEntitySource source, Vector2 at, int target, int damage, int owner, int count) {
        for (int i = 0; i < count; i++)
            Projectile.NewProjectile(source, at + Main.rand.NextVector2Circular(14, 14), Main.rand.NextVector2Circular(1.8f, 1.8f),
                ModContent.ProjectileType<HarbingerEmber>(), Math.Max(1, damage), 1, owner, target + 1);
    }
    public override bool? CanDamage() => Age < 24 ? false : null;
    public override void AI() {
        Projectile.ai[1]++;
        if (Age < 24) Projectile.velocity *= .9f;
        else {
            NPC target = Main.npc.IndexInRange((int)Projectile.ai[0] - 1) ? Main.npc[(int)Projectile.ai[0] - 1] : null;
            if (target == null || !target.CanBeChasedBy(Projectile)) {
                target = Projectile.FindTargetWithinRange(900);
                if (target != null && Projectile.owner == Main.myPlayer) { Projectile.ai[0] = target.whoAmI + 1; Projectile.netUpdate = true; }
            }
            if (target != null && target.CanBeChasedBy(Projectile)) {
                Vector2 delta = target.Center - Projectile.Center;
                float speed = Math.Min(16, Math.Max(2, delta.Length() * .2f));
                float inertia = MathHelper.Lerp(8, 1, Math.Clamp((Age - 24) / 45f, 0, 1));
                Vector2 current = Projectile.velocity.SafeNormalize(Vector2.UnitY) * Math.Min(Projectile.velocity.Length(), speed);
                Projectile.velocity = (current * inertia + delta.SafeNormalize(Vector2.UnitY) * speed) / (inertia + 1);
            }
        }
        if (Projectile.owner == Main.myPlayer && Age % 12 == 0) Projectile.netUpdate = true;
        if (Main.dedServ) return;
        float pulse = .65f + MathF.Sin(Age * .2f) * .15f;
        Vector2 axis = Projectile.velocity.SafeNormalize(Vector2.UnitY);
        ChargedOperatorVfx.Sparkle(Projectile.Center + Main.rand.NextVector2Circular(5, 5), -axis * .4f,
            new Color(255, 65, 105), axis.ToRotation(), pulse);
        Lighting.AddLight(Projectile.Center, .5f, .03f, .1f);
    }
    public override void OnKill(int timeLeft) {
        if (Projectile.owner == Main.myPlayer && Age >= 24)
            Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Center, Vector2.Zero,
                ModContent.ProjectileType<HarbingerOpticalBlast>(), Projectile.damage, 1, Projectile.owner);
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
        if (Projectile.owner == Main.myPlayer && Age >= 24) Projectile.Kill();
    }
    public override bool PreDraw(ref Color lightColor) => false;
}

public sealed class HarbingerOpticalBlast : ModProjectile
{
    public override string Texture => "Terraria/Images/MagicPixel";
    public override void SetDefaults() {
        Projectile.width = Projectile.height = 128; Projectile.friendly = true; Projectile.DamageType = DamageClass.Melee;
        Projectile.penetrate = -1; Projectile.tileCollide = false; Projectile.timeLeft = 20;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => Projectile.timeLeft >= 18 ? null : false;
    public override void AI() {
        if (Main.dedServ || Projectile.timeLeft != 20) return;
        SoundEngine.PlaySound(SoundID.Item14 with { Volume = .5f, Pitch = -.2f, MaxInstances = 4 }, Projectile.Center);
        for (int i = 0; i < 16; i++) {
            Vector2 ray = (i * MathHelper.TwoPi / 16).ToRotationVector2();
            ChargedOperatorVfx.Sparkle(Projectile.Center, ray * Main.rand.NextFloat(4, 10),
                i % 3 == 0 ? Color.White : new Color(255, 65, 105), ray.ToRotation(), .8f);
        }
    }
    public override bool PreDraw(ref Color lightColor) => false;
}
