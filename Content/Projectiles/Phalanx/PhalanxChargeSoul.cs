using System;
using ArknightsMod.Common.VisualEffects;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Phalanx;

// Tip-relative elliptical spiral: approach, orbit, then collapse at half playback speed.
// This projectile is exclusively visual, even if spawned with a nonzero damage value.
public sealed class PhalanxChargeSoul : ModProjectile
{
    private float motionTime;
    private bool initialized;
    private float initialRadius, initialAngle;
    private Vector2 previousTip, previousOffset;
    private int parentIndex = -1;
    internal const float MotionSpeed = .5f;

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
        Projectile.penetrate = -1; Projectile.timeLeft = 216; Projectile.extraUpdates = 1;
    }
    public override bool ShouldUpdatePosition() => false;
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
    }
    internal void Advance(Vector2 tip, Vector2 forward)
    {
        Vector2 up = forward.SafeNormalize(-Vector2.UnitY), right = up.RotatedBy(MathHelper.PiOver2);
        if (!initialized)
        {
            initialized = true; previousTip = tip;
            previousOffset = Projectile.Center - tip;
            initialRadius = Math.Clamp(new Vector2(Vector2.Dot(previousOffset, right), Vector2.Dot(previousOffset, up) / .72f).Length(), 24, 160);
            initialAngle = MathF.Atan2(Vector2.Dot(previousOffset, up) / .72f, Vector2.Dot(previousOffset, right));
        }
        // Translate the entire cached ribbon with its polar origin. Player
        // translation never enters the soul's local velocity or its momentum.
        Vector2 translation = tip - previousTip;
        for (int i = 0; i < Projectile.oldPos.Length; i++)
            if (Projectile.oldPos[i] != Vector2.Zero) Projectile.oldPos[i] += translation;
        previousTip = tip;
        motionTime += MotionSpeed;
        float radius;
        if (motionTime <= 24)
            radius = MathHelper.SmoothStep(initialRadius, 32, motionTime / 24);
        else if (motionTime <= 84)
            radius = MathHelper.SmoothStep(32, 18, (motionTime - 24) / 60);
        else
            radius = MathHelper.SmoothStep(18, 0, Math.Clamp((motionTime - 84) / 24, 0, 1));
        float angle = initialAngle + motionTime * .26f;
        Vector2 offset = right * (MathF.Cos(angle) * radius) + up * (MathF.Sin(angle) * radius * .72f);
        Projectile.velocity = offset - previousOffset;
        previousOffset = offset;
        Projectile.Center = tip + offset;
        Projectile.rotation = Projectile.velocity.SafeNormalize(right).ToRotation();
        Projectile.scale = .98f + .06f * MathF.Sin(motionTime * .11f);
        Projectile.Opacity = Math.Min(1, motionTime / 5) * Math.Min(1, Projectile.timeLeft / 36f);
        if (motionTime >= 108) Projectile.Kill();
    }
    public override bool PreDraw(ref Color lightColor) => false;
}
