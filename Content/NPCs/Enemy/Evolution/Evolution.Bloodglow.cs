using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

// Dreadnautilus BloodBolt reference: red-tinted greyscale gleam, dense afterimages and a cross flare.
// Uses our existing flare_01 texture; movement and rendering are independently implemented here.
internal static class EvolutionBloodglowVisuals
{
    internal static void Draw(Projectile projectile, float opacity, float scale = 1, bool decoy = false)
    {
        if (opacity <= 0) return;
        Texture2D gleam = EvolutionVisuals.Asset("BloodGleam");
        Vector2 origin = gleam.Size() * .5f;
        Color red = new Color(255, 12, 42, 0) * opacity;
        float angle = projectile.velocity.SafeNormalize(Vector2.UnitX).ToRotation();
        void Flare(Vector2 point, Vector2 size, float rotation, Color color)
            => Main.EntitySpriteDraw(gleam, point - Main.screenPosition, null, color, rotation, origin,
                size * scale / gleam.Size(), SpriteEffects.None);
        Vector2 previous = projectile.Center;
        float travelled = 0;
        for (int i = 0; i < Math.Min(decoy ? 3 : 7, projectile.oldPos.Length); i++)
        {
            if (projectile.oldPos[i] == Vector2.Zero) break;
            Vector2 next = projectile.oldPos[i] + projectile.Size * .5f;
            float length = Vector2.Distance(previous, next);
            if (length > 90 || travelled + length > 260 * scale) break;
            float fade = (1 - i / 7f) * .5f;
            for (int step = 1; step <= (decoy ? 1 : 3); step++)
                Flare(Vector2.Lerp(previous, next, step / (decoy ? 1f : 3f)), new Vector2(60, 32) * (1 - i * .075f), angle, red * fade);
            travelled += length; previous = next;
        }
        float pulse = .85f + .15f * MathF.Sin(projectile.ai[0] * .35f);
        Flare(projectile.Center, new Vector2(92, 42), angle, red * .9f);
        Flare(projectile.Center, new Vector2(126, 38) * pulse, MathHelper.PiOver2, red * .72f);
        Flare(projectile.Center, new Vector2(82, 28) * pulse, 0, red * .75f);
        if (!decoy) Flare(projectile.Center, new Vector2(35, 21), angle, new Color(255, 210, 220, 0) * opacity);
    }
}

internal static class EvolutionBloodglowRules
{
    internal const float CompanionSpacing = 92;
    internal static Vector2 AccelerateStraight(Vector2 velocity, float power) =>
        velocity.SafeNormalize(Vector2.UnitX) * Math.Min(14 * power, velocity.Length() + .22f * power);
    internal static Vector2 Steer(Vector2 center, Vector2 velocity, Vector2 target, int flyingAge, float power)
    {
        float speed = Math.Min(11 * power, Math.Max(4 * power, velocity.Length()) + .14f * power);
        Vector2 delta = target - center;
        float angle = velocity.SafeNormalize(Vector2.UnitY).ToRotation();
        if (flyingAge >= 5 && flyingAge < 55)
            angle = angle.AngleTowards(delta.ToRotation(), .026f);
        return angle.ToRotationVector2() * speed;
    }
}

public sealed partial class Evolution
{
    private void ReleaseBloodglow(Player player, int count)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        for (int i = 0; i < count; i++)
        {
            int side = i % 2 == 0 ? -1 : 1;
            Vector2 point = player.Center + new Vector2(side * Main.rand.NextFloat(420, 560), Main.rand.NextFloat(-220, 220));
            if (CorridorActive) point.Y = Math.Clamp(point.Y, CorridorTop + 60, CorridorBottom - 60);
            Vector2 direction = (player.Center - point).SafeNormalize(Vector2.UnitY);
            Shoot(EvolutionShot.Bloodglow, point, direction * 4,
                delay: 36, lifetime: 170, quietLaunch: true);
            if (Phase != 5) continue;
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
            // Spawn independently at the original heading. Only the central projectile homes.
            for (int lane = -4; lane <= 4; lane++)
                if (lane != 0) Shoot(EvolutionShot.Bloodglow, point + normal * (lane * EvolutionBloodglowRules.CompanionSpacing),
                    direction * 4, parameter: -1, delay: 36, lifetime: 170, peripheral: true, quietLaunch: true);
        }
    }
}

public sealed partial class EvolutionHazard
{
    private Vector2 bloodglowPrevious;
    private bool bloodglowSweep;
    internal bool IsBloodglowCompanion => Kind == EvolutionShot.Bloodglow && Parameter < 0;
    private void UpdateBloodglow()
    {
        bloodglowPrevious = Projectile.Center; bloodglowSweep = true;
        if (IsBloodglowCompanion) Projectile.hostile = false;
        if (Age < Delay) return;
        if (IsBloodglowCompanion)
        {
            Projectile.velocity = EvolutionBloodglowRules.AccelerateStraight(Projectile.velocity, PowerScale);
            Projectile.rotation = Projectile.velocity.ToRotation();
            return;
        }
        if (!HomingFinished && Main.player.IndexInRange(Target) && Main.player[Target].active && !Main.player[Target].dead)
        {
            Vector2 target = Main.player[Target].Center;
            if (Age - Delay >= 55 || Vector2.DistanceSquared(Projectile.Center, target) < 100 * 100)
            { HomingFinished = true; Projectile.netUpdate = true; }
            Projectile.velocity = EvolutionBloodglowRules.Steer(Projectile.Center, Projectile.velocity, target,
                HomingFinished ? 55 : Age - Delay, PowerScale);
        }
        Projectile.rotation = Projectile.velocity.ToRotation();
        if (!Main.dedServ) Lighting.AddLight(Projectile.Center, new Vector3(.35f, .01f, .04f));
    }
}
