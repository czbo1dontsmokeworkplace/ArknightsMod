using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

public sealed partial class EvolutionHazard
{
    internal Vector2 CrashFanDirection(int ray) => EvolutionBroodfallRules.FanDirection(ray) * (Projectile.velocity.Y < 0 ? -1 : 1);
    internal bool IsBroodArsenal => Kind is EvolutionShot.CrashFan or EvolutionShot.Flame or
        EvolutionShot.BroodRocket or EvolutionShot.GravityFlare or EvolutionShot.Serpent;
    private int RocketStop => Delay + (int)Parameter;
    private int RocketBlast => RocketStop + 30;
    private bool ArsenalMoves => (Kind is EvolutionShot.Flame or EvolutionShot.GravityFlare) && Age >= Delay ||
        Kind == EvolutionShot.BroodRocket && Age >= Delay && Age < RocketStop;
    internal bool ArsenalCanDamage() => Kind switch
    {
        EvolutionShot.CrashFan => Age >= Delay && Age < Delay + EvolutionBroodfallRules.FanActive &&
            EvolutionRules.BeamEnvelope(Age-Delay,EvolutionBroodfallRules.FanActive,EvolutionBroodfallRules.FanFade)>=.45f,
        EvolutionShot.BroodRocket => Age >= Delay && Age < RocketStop || Age >= RocketBlast && Age < RocketBlast + 8,
        _ => Age >= Delay && Age < Lifetime - 18
    };
    internal Vector2 SerpentPoint(int age)
    {
        int t = Math.Max(0, age - Delay);
        Vector2 normal = Projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.PiOver2);
        return ArsenalOrigin + Projectile.velocity * t + normal * (MathF.Sin(t * .29f) * Parameter * Math.Min(1, t / 14f));
    }
    private void UpdateBroodArsenal(Evolution boss)
    {
        int t = Age - Delay;
        if (Kind == EvolutionShot.Serpent)
        {
            Projectile.Center = SerpentPoint(Age);
            Projectile.rotation = (SerpentPoint(Age + 1) - Projectile.Center).ToRotation();
        }
        if (Kind == EvolutionShot.GravityFlare && t >= 0)
        {
            Projectile.velocity.X *= MathF.Pow(.995f, KineticScale);
            Projectile.velocity.Y = Math.Min(17 * KineticScale, Projectile.velocity.Y + EvolutionBroodfallRules.Gravity * KineticScale * KineticScale);
            Projectile.tileCollide = t > 7 && !Collision.SolidCollision(Projectile.position, Projectile.width, Projectile.height);
        }
        if (Kind == EvolutionShot.BroodRocket && t >= 0 && Age < RocketStop)
        {
            float speed = MathHelper.Lerp(2, 8, Math.Clamp((t + 1f) * KineticScale / 10, 0, 1)) * KineticScale;
            Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * speed;
        }
        if (Kind != EvolutionShot.CrashFan && Kind != EvolutionShot.Serpent)
            Projectile.rotation = Projectile.velocity.ToRotation();
        if (Main.dedServ) return;
        if (t == 0)
        {
            SoundEngine.PlaySound((Kind == EvolutionShot.CrashFan ? SoundID.Zombie104 : SoundID.Item34) with
                { Volume = Kind == EvolutionShot.Flame ? .14f : .48f, Pitch = -.35f, MaxInstances = 3 }, Projectile.Center);
            if (Kind == EvolutionShot.CrashFan) EvolutionImpactSystem.Emit(Projectile.Center, 450, 3);
        }
        if (Kind == EvolutionShot.BroodRocket && Age == RocketBlast)
        {
            EvolutionArsenalVisuals.Burst(Projectile.Center, 1.1f);
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = .55f, MaxInstances = 2 }, Projectile.Center);
        }
        if (t >= 0 && Kind is EvolutionShot.Flame or EvolutionShot.BroodRocket or EvolutionShot.GravityFlare && Age % 3 == 0 &&
            (Kind != EvolutionShot.BroodRocket || Age < RocketStop))
            EvolutionArsenalVisuals.Smoke(Projectile.Center, -Projectile.velocity * .15f,
                Kind == EvolutionShot.Flame ? 40 : 26, 20, Age % 6 == 0);
        Lighting.AddLight(Projectile.Center, EvolutionVisuals.Blood.ToVector3() * .5f);
    }
    internal bool ArsenalCollides(Rectangle box)
    {
        if (Kind == EvolutionShot.CrashFan)
        {
            for (int i = 0; i < EvolutionBroodfallRules.FanRayCount; i++)
            {
                Vector2 direction = CrashFanDirection(i);
                // 近端75像素是亮核；释放玩家时不让交汇点变成六束共同命中的陷阱。
                if (RayPath(Projectile.Center + direction * 75, direction, EvolutionBroodfallRules.FanReach - 75)
                    .Collides(box, EvolutionBroodfallRules.FanWidth)) return true;
            }
            return false;
        }
        if (Kind == EvolutionShot.Serpent)
        {
            for (int i = 0; i < 15; i++)
            {
                if (Age - i <= Delay) break;
                float hit = 0;
                if (Collision.CheckAABBvLineCollision(box.TopLeft(), box.Size(), SerpentPoint(Age - i),
                    SerpentPoint(Age - i - 1), 14 * (1 - i / 18f), ref hit)) return true;
            }
            return false;
        }
        float radius = Kind == EvolutionShot.BroodRocket && Age >= RocketStop ? 125 :
            Kind == EvolutionShot.Flame ? 18 : 12;
        Vector2 nearest = new(Math.Clamp(Projectile.Center.X, box.Left, box.Right), Math.Clamp(Projectile.Center.Y, box.Top, box.Bottom));
        return Vector2.DistanceSquared(nearest, Projectile.Center) <= radius * radius;
    }
    private void DrawBroodArsenal()
    {
        float fade = Math.Clamp((Lifetime - Age) / 18f, 0, 1);
        Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitY);
        if (Kind == EvolutionShot.CrashFan)
        {
            int fired = Age - Delay;
            float opacity = EvolutionRules.BeamEnvelope(fired,EvolutionBroodfallRules.FanActive,EvolutionBroodfallRules.FanFade);
            for (int ray = 0; ray < EvolutionBroodfallRules.FanRayCount; ray++)
            {
                Vector2 d = CrashFanDirection(ray);
                Vector2 start = Projectile.Center + d * 75, end = Projectile.Center + d * EvolutionBroodfallRules.FanReach;
                if (fired >= 0 && BeamStyle == EvolutionBeamStyle.Refracted)
                    DrawRayContacts(RayPath(start, d, EvolutionBroodfallRules.FanReach - 75), opacity, EvolutionBroodfallRules.FanWidth);
                if (fired < 0)
                {
                    if (BeamStyle == EvolutionBeamStyle.Refracted)
                    {
                        DrawRayWarning(RayPath(start, d, EvolutionBroodfallRules.FanReach - 75), true);
                        continue;
                    }
                    float warning = Math.Clamp(Age / 16f, 0, 1) * Math.Clamp((Delay - Age) / 8f, 0, 1);
                    Vector2 edge = d.RotatedBy(MathHelper.PiOver2) * (EvolutionBroodfallRules.FanWidth * .5f);
                    EvolutionVisuals.Line(start, end, EvolutionVisuals.Core * (.7f * warning), 2);
                    EvolutionVisuals.Line(start + edge, end + edge, EvolutionVisuals.Blood * (.35f * warning), 1, false);
                    EvolutionVisuals.Line(start - edge, end - edge, EvolutionVisuals.Blood * (.35f * warning), 1, false);
                }
            }
            if(!EvolutionLaserVisuals.WorldMaterialReady) DrawContinuousBodies();
            EvolutionLaserVisuals.DrawMuzzle(Projectile.Center,-Vector2.UnitY,opacity,70,fired,EvolutionVisuals.Core);
            return;
        }
        if (Age < Delay)
        {
            float warning = Math.Clamp(Age / 12f, 0, 1);
            EvolutionVisuals.Glow(Projectile.Center, EvolutionVisuals.Core * (.4f * warning), new Vector2(30));
            if (Kind == EvolutionShot.GravityFlare) DrawGravityPreview(warning);
            else if (Kind == EvolutionShot.Serpent)
            {
                for (int i = 0; i < 75; i += 3)
                    EvolutionVisuals.Line(SerpentPoint(Delay + i), SerpentPoint(Delay + i + 2), EvolutionVisuals.Blood * (.5f * warning), 1.2f, false);
            }
            else EvolutionVisuals.Line(Projectile.Center, Projectile.Center + direction * (Kind == EvolutionShot.Flame ? 380 : 500),
                EvolutionVisuals.Core * (.45f * warning), 1.4f);
            return;
        }
        switch (Kind)
        {
            case EvolutionShot.Flame:
                EvolutionArsenalVisuals.Flame(Projectile.Center, direction, Age, fade, 55);
                break;
            case EvolutionShot.GravityFlare:
                for (int i = 1; i < Math.Min(7, Age - Delay); i++)
                    if (Projectile.oldPos[i] != Vector2.Zero)
                        EvolutionArsenalVisuals.GravityFlare(Projectile.oldPos[i] + Projectile.Size * .5f,
                            Projectile.oldRot[i], fade * (1 - i / 7f) * .32f);
                EvolutionArsenalVisuals.GravityFlare(Projectile.Center, Projectile.rotation, fade);
                break;
            case EvolutionShot.BroodRocket:
                if (Age < RocketStop)
                {
                    EvolutionArsenalVisuals.Flame(Projectile.Center - direction * 10, direction, Age, fade, 70);
                    EvolutionProjectileVisuals.DrawBlade(Projectile.Center, Projectile.rotation, fade);
                }
                else if (Age < RocketBlast)
                {
                    float charge = (Age - RocketStop) / 30f;
                    EvolutionVisuals.Ring(Projectile.Center, 125, 0, EvolutionVisuals.Blood * (.45f + charge * .4f), 2, false);
                    EvolutionProjectileVisuals.DrawBomb(Projectile.Center, Age * .05f, 42 + charge * 14, charge);
                }
                else EvolutionArsenalVisuals.Blast(Projectile.Center, Age - RocketBlast, 125);
                break;
            case EvolutionShot.Serpent:
                for (int i = 14; i >= 0; i--)
                {
                    if (Age - i <= Delay) continue;
                    float opacity = (1 - i / 16f) * fade;
                    EvolutionArsenalVisuals.Spark(SerpentPoint(Age - i - 1), SerpentPoint(Age - i),
                        14 * (1 - i / 18f), EvolutionVisuals.Blood * opacity);
                    EvolutionVisuals.Line(SerpentPoint(Age - i - 1), SerpentPoint(Age - i),
                        EvolutionVisuals.Core * (.9f * opacity), 3 * (1 - i / 18f), false);
                }
                EvolutionVisuals.Glow(Projectile.Center, EvolutionVisuals.Core * fade, new Vector2(17));
                break;
        }
    }
    private void DrawGravityPreview(float opacity)
    {
        Vector2 point = ArsenalOrigin, velocity = Projectile.velocity;
        for (int i = 0; i < 75; i++)
        {
            velocity.X *= MathF.Pow(.995f, KineticScale); velocity.Y = Math.Min(17 * KineticScale, velocity.Y + EvolutionBroodfallRules.Gravity * KineticScale * KineticScale);
            Vector2 next = point + velocity;
            if (i % 8 < 3) EvolutionVisuals.Line(point, next, EvolutionVisuals.Core * (.36f * opacity), 1, false);
            point = next;
        }
    }
}
