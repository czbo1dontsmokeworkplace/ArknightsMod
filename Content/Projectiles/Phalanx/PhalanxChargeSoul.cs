using System;
using ArknightsMod.Common.VisualEffects;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Phalanx;

// SHPC NewSHPS preset 4: corkscrew approach -> tight orbit -> inward collapse.
// This projectile is exclusively visual, even if spawned with a nonzero damage value.
public sealed class PhalanxChargeSoul : ModProjectile
{
    private int timer, state, stateTimer;
    private float orbitRadius, orbitAngle, angularVelocity, orbitTravel;
    private int parentIndex = -1;
    private float Seed => Projectile.identity * 1.618034f + Projectile.ai[0] * .37f;
    private float Spin => (Projectile.identity & 1) == 0 ? 1 : -1;
    internal Color Tint => PhalanxVisuals.Palette((int)Projectile.ai[1]);
    public override string Texture => "Terraria/Images/MagicPixel";
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Type] = 14;
        ProjectileID.Sets.TrailingMode[Type] = 2;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 24;
        Projectile.friendly = Projectile.hostile = false;
        Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.penetrate = -1; Projectile.timeLeft = 108; Projectile.extraUpdates = 1;
    }
    public override bool? CanDamage() => false;
    public override bool CanHitPlayer(Player target) => false;
    private PhalanxFocus Parent()
    {
        if (parentIndex >= 0)
        {
            Projectile p = Main.projectile[parentIndex];
            if (p.active && p.owner == Projectile.owner && p.identity == (int)Projectile.ai[0])
                return p.ModProjectile as PhalanxFocus;
        }
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.owner == Projectile.owner && p.identity == (int)Projectile.ai[0] && p.ModProjectile is PhalanxFocus focus)
            { parentIndex = p.whoAmI; return focus; }
        return null;
    }
    public override void AI()
    {
        if (Main.dedServ) return;
        PhalanxFocus parent = Parent();
        if (parent == null || !parent.ChargingForEffects || parent.ReleaseSequence != (int)Projectile.ai[2]
            || (int)parent.Projectile.ai[0] != (int)Projectile.ai[1])
        { Projectile.Kill(); return; }
        Advance(parent.FocusPosition(), new Vector2(0, -Main.player[Projectile.owner].gravDir));
        if (Projectile.active && Projectile.numUpdates == 0 && Main.rand.NextBool(5))
            PhalanxChargeEffects.MaterialMote(Projectile.Center, -Projectile.velocity * .08f, (int)Projectile.ai[1], 12);
    }
    internal void Advance(Vector2 tip, Vector2 forward)
    {
        timer++; stateTimer++;
        Projectile.scale = .92f + .12f * (.5f + .5f * MathF.Sin(timer * .22f + Seed));
        Projectile.Opacity = Math.Min(1, timer / 10f) * Math.Min(1, Projectile.timeLeft / 18f);
        Vector2 delta = tip - Projectile.Center;
        float distance = delta.Length();
        Vector2 inward = delta.SafeNormalize(forward), side = inward.RotatedBy(MathHelper.PiOver2 * Spin);
        if (distance > 420) { Projectile.Kill(); return; }
        switch (state)
        {
            case 0:
                float waveA = MathF.Sin(timer * .37f + Seed), waveB = MathF.Cos(timer * .18f + Seed * 1.6f);
                float approach = Utils.GetLerpValue(220, 30, distance, true);
                float twist = MathHelper.Lerp(13, 3.5f, approach);
                Vector2 desired = inward * MathHelper.Lerp(7, 22, approach)
                    + side * (waveA * twist + waveB * twist * .55f) + forward * (2.5f * waveB);
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, .14f);
                if (distance < 42 || (stateTimer > 18 && distance < 78))
                {
                    state = 1; stateTimer = 0; orbitRadius = Math.Clamp(distance, 18, 44);
                    orbitAngle = (-delta).ToRotation(); angularVelocity = .26f * Spin;
                }
                break;
            case 1:
                orbitRadius = MathHelper.Lerp(orbitRadius, 12, .055f);
                angularVelocity = MathHelper.Lerp(angularVelocity, (.44f + .06f * MathF.Sin(timer * .16f + Seed)) * Spin, .08f);
                orbitAngle += angularVelocity; orbitTravel += Math.Abs(angularVelocity);
                Vector2 offset = orbitAngle.ToRotationVector2() * orbitRadius;
                Vector2 tangent = offset.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2 * Spin);
                Vector2 desiredPosition = tip + offset + tangent * (MathF.Sin(timer * .33f + Seed) * 5)
                    + forward * (MathF.Cos(timer * .27f + Seed) * 3);
                Projectile.velocity = Vector2.Lerp(Projectile.velocity,
                    (desiredPosition - Projectile.Center) * .5f + tangent * (5.5f + orbitRadius * .08f), .18f);
                if (orbitTravel >= MathHelper.TwoPi * (2.25f + .575f * (1 + MathF.Sin(Seed))) || Projectile.timeLeft < 22)
                { state = 2; stateTimer = 0; }
                break;
            default:
                float collapse = Math.Clamp(stateTimer / 18f, 0, 1);
                Projectile.velocity = Vector2.Lerp(Projectile.velocity,
                    inward * MathHelper.Lerp(10, 24, collapse)
                    + side * MathF.Sin(timer * .6f + Seed) * MathHelper.Lerp(4.5f, 0, collapse), .22f);
                if (distance < 10) { Projectile.Kill(); return; }
                break;
        }
        Projectile.rotation = Projectile.velocity.ToRotation();
    }
    public override bool PreDraw(ref Color lightColor) => false;
}
