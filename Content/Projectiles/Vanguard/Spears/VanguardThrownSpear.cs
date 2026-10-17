using System;
using ArknightsMod.Content.Items.Weapons.Vanguard;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Vanguard.Spears;

// Exactly one physical javelin per right-click, for every operator.
public sealed class VanguardThrownSpear : ModProjectile
{
    public override string Texture => "Terraria/Images/MagicPixel";
    public VanguardSpearKind Kind => (VanguardSpearKind)(int)Projectile.ai[0];
    private bool impactSpent;
    private Vector2 start, previousFrame;
    private readonly Vector2[] history = new Vector2[24];
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 14; Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Melee; Projectile.ignoreWater = true; Projectile.tileCollide = true;
        Projectile.extraUpdates = VanguardSpearRules.FlightUpdates - 1;
        Projectile.timeLeft = 75 * VanguardSpearRules.FlightUpdates;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override void OnSpawn(IEntitySource source)
    {
        start = previousFrame = Projectile.Center;
        Projectile.penetrate = VanguardSpearRules.Penetration(Kind);
        VanguardSpearVfx.Throw(start, Projectile.velocity.SafeNormalize(Vector2.UnitX), Kind);
        if (Projectile.owner == Main.myPlayer) VanguardThrowPulse.Spawn(Projectile, start, false);
    }
    public override void AI()
    {
        if (!VanguardSpearRules.ValidKind((int)Kind) || !float.IsFinite(Projectile.velocity.X) || !float.IsFinite(Projectile.velocity.Y))
        { Projectile.Kill(); return; }
        Projectile.rotation = Projectile.velocity.ToRotation();
        // Check the range on every substep; extra updates cannot overshoot by a full frame.
        if (Vector2.DistanceSquared(start, Projectile.Center) > VanguardSpearRules.FlightRange * VanguardSpearRules.FlightRange)
        { Projectile.Kill(); return; }
        if (Projectile.numUpdates != 0) return;
        if (++Projectile.ai[2] >= 75) { Projectile.Kill(); return; }
        // Real-frame tip history, independent of extraUpdates and frame rate.
        Array.Copy(history, 0, history, 1, history.Length - 1); history[0] = Projectile.Center;
        Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
        // Three bounded samples along the distance actually travelled, not three effects at one point.
        for (int i = 1; i <= 3; i++)
            VanguardSpearVfx.Flight(Vector2.Lerp(previousFrame, Projectile.Center, i / 3f), direction, Kind, (int)Projectile.ai[2] * 3 + i, false);
        previousFrame = Projectile.Center;
    }
    public override bool? CanHitNPC(NPC target) => target.friendly ? false : null;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float point = 0;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
            Projectile.Center - Projectile.velocity, Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitX) * 7, 14, ref point);
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        Vector2 point = target.Hitbox.ClosestPointInRect(Projectile.Center);
        VanguardSpearVfx.Impact(point, Projectile.velocity.SafeNormalize(Vector2.UnitX), Kind, true);
        if (Projectile.owner != Main.myPlayer) return;
        if (!impactSpent)
        {
            impactSpent = true;
            VanguardSpearSpark.Release(Projectile, point, Kind, true);
            VanguardThrowPulse.Spawn(Projectile, point, true);
            float radius = Kind == VanguardSpearKind.Bagpipe ? 76 : Kind == VanguardSpearKind.Vigna ? 40 : 52;
            int blastDamage = Kind == VanguardSpearKind.Bagpipe ? Projectile.damage : Kind == VanguardSpearKind.Vigna ? Projectile.damage / 2 : 0;
            VanguardSpearImpact.Spawn(Projectile, point, Kind, blastDamage, radius);
        }
    }
    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        VanguardSpearVfx.Impact(Projectile.Center, oldVelocity.SafeNormalize(Vector2.UnitX), Kind, false);
        if (Projectile.owner == Main.myPlayer) VanguardThrowPulse.Spawn(Projectile, Projectile.Center, false);
        return true;
    }
    public override void OnKill(int timeLeft) => VanguardSpearVfx.Fizzle(Projectile.Center, Kind);
    public override bool PreDraw(ref Color lightColor)
    {
        VanguardSpearVfx.FlightTrails(Projectile.Center, history, Kind, Projectile.ai[2]);
        Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
        float length = 98 + (int)Kind * 10;
        VanguardSpearVfx.DrawSpear(Projectile.Center - direction * length * .78f, Projectile.Center, Kind, lightColor, true, true, Projectile.ai[2]);
        return false;
    }
}
