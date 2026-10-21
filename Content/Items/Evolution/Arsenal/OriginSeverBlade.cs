using System;
using ArknightsMod.Common.VisualEffects;
using ArknightsMod.Content.NPCs.Enemy.Evolution;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.Items.Evolution.Arsenal;

internal static class OriginSeverBladeRules
{
    internal const float MaximumSpeed = 44, SearchRange = 1300, MaximumTrail = 260 * EvolutionArsenalRules.OriginScale;
    internal static Vector2 Pursue(Vector2 center, Vector2 velocity, Vector2 target, Vector2 targetVelocity, float age, float lane)
    {
        float speed = Math.Min(MaximumSpeed, Math.Max(32, velocity.Length()) + 1.6f);
        Vector2 delta = target - center;
        float distance = delta.Length();
        // Close the final step exactly, rather than orbiting past a small target at high speed.
        if (distance <= speed) return delta;
        float prediction = Math.Clamp(distance / speed * .55f, 0, 12) * Math.Clamp((distance - 140) / 240, 0, 1);
        Vector2 aim = target + targetVelocity * prediction - center;
        float bend = MathF.Sin(age * .52f + lane * 1.7f) * .07f * Math.Clamp((10 - age) / 7, 0, 1);
        float turn = MathHelper.Lerp(.62f, .17f, Math.Clamp((distance - 150) / 850, 0, 1));
        float angle = velocity.SafeNormalize(Vector2.UnitX).ToRotation().AngleTowards(aim.ToRotation() + bend, turn);
        return angle.ToRotationVector2() * speed;
    }
}

public partial class OriginShard
{
    private Vector2 sweepStart;
    private bool hasSweep;
    private void UpdateSeverBlade()
    {
        Player owner = Main.player[Projectile.owner];
        if (!owner.active || owner.dead) { Projectile.Kill(); return; }
        sweepStart = Projectile.Center; hasSweep = true;
        Projectile.ai[0]++;
        NPC target = null;
        if (Projectile.ai[1] > 0 && Projectile.ai[1] <= Main.maxNPCs)
        {
            NPC locked = Main.npc[(int)Projectile.ai[1] - 1];
            if (locked.CanBeChasedBy() && Vector2.DistanceSquared(locked.Center, Projectile.Center) <= OriginSeverBladeRules.SearchRange * OriginSeverBladeRules.SearchRange)
                target = locked;
        }
        if (target == null && Projectile.owner == Main.myPlayer && (Projectile.ai[1] > 0 || (int)Projectile.ai[0] % 8 == 1))
        {
            target = EvolutionArsenalRules.Target(Projectile.Center, OriginSeverBladeRules.SearchRange, owner);
            int slot = target == null ? 0 : target.whoAmI + 1;
            if (Projectile.ai[1] != slot) { Projectile.ai[1] = slot; Projectile.netUpdate = true; }
        }
        Projectile.velocity = target == null ? Projectile.velocity.SafeNormalize(Vector2.UnitX) *
            Math.Min(OriginSeverBladeRules.MaximumSpeed, Math.Max(32, Projectile.velocity.Length()) + 1.6f) :
            OriginSeverBladeRules.Pursue(Projectile.Center, Projectile.velocity, target.Center, target.velocity, Projectile.ai[0], Projectile.ai[2]);
        Projectile.rotation = Projectile.velocity.SafeNormalize(Vector2.UnitX).ToRotation();
        if (!Main.dedServ)
        {
            Lighting.AddLight(Projectile.Center, new Vector3(.38f, .018f, .04f));
            if ((int)Projectile.ai[0] % 4 == 0)
            {
                Vector2 normal = Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
                float side = (int)Projectile.ai[0] % 8 == 0 ? 1 : -1;
                OperatorTextureEffects.Mote(Projectile.Center + normal * side * 8, -Projectile.velocity * .035f + normal * side * .7f,
                    EvolutionWeaponVisuals.Blood, 4, 14);
            }
        }
    }
}

internal static class OriginSeverBladeVisuals
{
    internal static int Trail(Projectile projectile, Span<Vector2> points)
    {
        points[0] = projectile.Center;
        int count = 1;
        float travelled = 0;
        for (int i = 0; i < projectile.oldPos.Length && count < points.Length; i++)
        {
            if (projectile.oldPos[i] == Vector2.Zero) break;
            Vector2 next = projectile.oldPos[i] + projectile.Size * .5f;
            float distance = Vector2.Distance(points[count - 1], next);
            if (distance > 80) break; // No streaks across a spawn correction or teleport.
            if (distance < .5f) continue;
            float remaining = OriginSeverBladeRules.MaximumTrail - travelled;
            points[count] = Vector2.Lerp(points[count - 1], next, Math.Min(1, remaining / distance));
            travelled += Math.Min(remaining, distance); count++;
            if (travelled >= OriginSeverBladeRules.MaximumTrail - .01f) break;
        }
        return count;
    }
    internal static void Draw(Projectile projectile)
    {
        Span<Vector2> points = stackalloc Vector2[22];
        int count = Trail(projectile, points);
        float length = 0;
        for (int i = 1; i < count; i++) length += Vector2.Distance(points[i - 1], points[i]);
        float travelled = 0;
        for (int i = 1; i < count; i++)
        {
            Vector2 start = points[i - 1];
            // Two subsegments per history point soften tight turns without long, bright ribbons.
            for (int step = 1; step <= 2; step++)
            {
                Vector2 end = Vector2.CatmullRom(points[Math.Max(0, i - 2)], points[i - 1], points[i], points[Math.Min(count - 1, i + 1)], step / 2f);
                float distance = Vector2.Distance(start, end), offset = Math.Clamp(travelled / Math.Max(1, length), 0, 1);
                float span = Math.Min(1 - offset, distance / Math.Max(1, length));
                float taper = MathF.Pow(Math.Clamp(1 - offset, 0, 1), .65f);
                EvolutionWeaponVisuals.SeverRibbon(start, end, 26 * EvolutionArsenalRules.OriginScale * taper, .32f * taper, projectile.identity * .71f, offset, span);
                travelled += distance; start = end;
            }
        }

    }
    internal static void Details(Projectile projectile)
    {
        float opacity = Math.Clamp(projectile.ai[0] / 4, 0, 1) * Math.Clamp(projectile.timeLeft / 14f, 0, 1);
        EvolutionBloodglowVisuals.Draw(projectile, opacity, EvolutionArsenalRules.OriginScale);
    }
}
