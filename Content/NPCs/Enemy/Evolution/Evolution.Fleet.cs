using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

internal static class EvolutionFleetRules
{
    internal const int SlamLock = 0, SlamLaunch = 13, SlamImpact = 22, SlamFan = SlamImpact + EvolutionBroodfallRules.Escape;
    internal const int ZigStride = 68, ZigLock = 18, ZigLaunch = 38, ZigEnd = 60;
    internal static bool IsMotion(int attack) => attack is 14 or 15;
    internal static int Duration(int attack) => EvolutionRecitalRules.IsFigure(attack) ? EvolutionRecitalRules.Duration :
        EvolutionBloodSurgeRules.IsFigure(attack) ? EvolutionBloodSurgeRules.Duration :
        attack is >= 19 and <= 22 ? EvolutionRicochetRules.Duration : IsMotion(attack) ? 216 : EvolutionPianoRules.Duration;
    internal static int CurtainGap(int wave, int cycle) => cycle % 3 - 2 + (wave is 1 or 2 ? 1 : 0);
    internal static Vector2 ZigTarget(Vector2 center, int leg) => center + new Vector2(leg % 2 == 0 ? 420 : -420, leg % 2 == 0 ? -150 : 150);
    internal static bool DashEmission(int age) => age is 5 or 12 or 19;
}

public sealed partial class Evolution
{
    private void DoFleetAttack(Player player)
    {
        int t = Timer;
        if (EvolutionCorridorPressureRules.IsFigure(Attack)) { DoHorizontalPressure(); return; }
        if (Attack >= 19) { DoRicochetPattern(); return; }
        if (Attack == 14) { DoQuickSlam(player); return; }
        if (Attack == 15) { DoLowZigzag(player); return; }
        if (Attack == 16 && t is 16 or 74 or 132)
        {
            int wave = (t - 16) / 58, side = wave % 2 == 0 ? -1 : 1;
            Vector2 source = new(PatternOrigin.X + side * 570, Math.Clamp(PatternOrigin.Y + (wave - 1) * 140, CorridorTop + 100, CorridorBottom - 100));
            Vector2 direction = (PatternOrigin - source).SafeNormalize(-side * Vector2.UnitX);
            for (int i = 0; i < 8; i++)
                FleetBolt(source, direction.RotatedBy(EvolutionRules.FanSlot(i, 8) * .15f) * 9, 24, 100);
        }
        if (Attack == 17 && t >= 12 && t <= 120 && (t - 12) % 36 == 0)
        {
            int wave = (t - 12) / 36, side = wave % 2 == 0 ? -1 : 1;
            int gap = EvolutionFleetRules.CurtainGap(wave, (int)NPC.ai[2]);
            for (int row = -4; row <= 4; row++)
            {
                if (row == gap || row == gap + 1) continue;
                FleetBolt(new Vector2(PatternOrigin.X + side * 600, Arena.Y + row * 76), -side * Vector2.UnitX * 9, 24, 106);
            }
        }
        if (Attack == 18 && t is 18 or 76 or 134)
        {
            int wave = (t - 18) / 58;
            for (int i = 0; i < 12; i++)
            {
                if (i % 6 == 0) continue;
                Vector2 d = (i * MathHelper.TwoPi / 12 + wave * .22f).ToRotationVector2();
                FleetBolt(NPC.Center + d * 64, d * (5.5f + wave * .4f), 28, Math.Min(100, 230 - t));
            }
        }
    }
    private void FleetBolt(Vector2 source, Vector2 velocity, int delay, int lifetime) =>
        Shoot(EvolutionShot.Barrage, source, velocity, delay: delay, lifetime: lifetime, quietLaunch: true);

    private void DoQuickSlam(Player player)
    {
        int t = Timer;
        if (t == EvolutionFleetRules.SlamLock && Main.netMode != NetmodeID.MultiplayerClient)
            BeginCorridorSlam(player);
        if (t >= EvolutionFleetRules.SlamLock && t < EvolutionFleetRules.SlamLaunch) NPC.velocity = Vector2.Zero;
        if (t >= EvolutionFleetRules.SlamLaunch && t < EvolutionFleetRules.SlamImpact)
        {
            float u = (t - EvolutionFleetRules.SlamLaunch + 1f) / (EvolutionFleetRules.SlamImpact - EvolutionFleetRules.SlamLaunch);
            NPC.velocity = Vector2.Lerp(CrashStart, CrashGround, u * u) - NPC.Center;
            if (!Main.dedServ && t % 3 == 0) EvolutionArsenalVisuals.Smoke(NPC.Center, -NPC.velocity * .12f, 42, 20, true);
        }
        if (t >= EvolutionFleetRules.SlamImpact && t < 148) { NPC.Center = CrashGround; NPC.velocity = Vector2.Zero; }
        if (t == EvolutionFleetRules.SlamImpact && !Main.dedServ)
        {
            EvolutionImpactSystem.Emit(CrashGround, 430, 5, false, 24);
            EvolutionArsenalVisuals.Burst(CrashGround, 1.5f);
            SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode with { Volume = .55f, Pitch = -.4f }, CrashGround);
        }
        if (t == EvolutionFleetRules.SlamFan)
            Shoot(EvolutionShot.CrashFan, CorridorSlamMuzzle, Vector2.Zero,
                delay: 48, lifetime: 128, beamStyle: EvolutionBeamStyle.Refracted);
        if (t >= 148) HoverLow(player);
    }
    private void DoLowZigzag(Player player)
    {
        int leg = Timer / EvolutionFleetRules.ZigStride, t = Timer % EvolutionFleetRules.ZigStride;
        if (leg >= 3) { NPC.velocity *= .84f; return; }
        float side = leg % 2 == 0 ? -1 : 1;
        if (t < EvolutionFleetRules.ZigLock)
            MoveTo(new Vector2(PatternOrigin.X + side * 560,
                Math.Clamp(PatternOrigin.Y - side * 150, CorridorTop + 160, CorridorBottom - 160)), 18, .18f);
        if (t == EvolutionFleetRules.ZigLock && Main.netMode != NetmodeID.MultiplayerClient)
        {
            Aim = EvolutionFleetRules.ZigTarget(PatternOrigin, leg);
            Aim.Y = Math.Clamp(Aim.Y, CorridorTop + 160, CorridorBottom - 160);
            DashDirection = (Aim - NPC.Center).SafeNormalize(-side * Vector2.UnitX); NPC.netUpdate = true;
        }
        if (t >= EvolutionFleetRules.ZigLock && t < EvolutionFleetRules.ZigLaunch) NPC.velocity *= .35f;
        if (t >= EvolutionFleetRules.ZigLaunch && t < EvolutionFleetRules.ZigEnd)
        {
            Charging = true; NPC.velocity = DashDirection * (44 * EvolutionCorridorPressureRules.Speed);
            int age = t - EvolutionFleetRules.ZigLaunch;
            if (EvolutionFleetRules.DashEmission(age))
                for (int flank = -1; flank <= 1; flank += 2)
                {
                    Vector2 normal = DashDirection.RotatedBy(MathHelper.PiOver2) * flank;
                    FleetBolt(NPC.Center + normal * 56, normal * 6 - DashDirection * 1.5f, 18, Math.Min(96, 230 - Timer));
                }
        }
        if (t >= EvolutionFleetRules.ZigEnd) NPC.velocity *= .72f;
    }
    private void DrawFleetWarnings()
    {
        if (Phase != 3 || CrashOpeningActive) return;
        if (Attack == 14 && Timer >= EvolutionFleetRules.SlamLock && Timer < EvolutionFleetRules.SlamLaunch)
        {
            float fade = Math.Clamp((Timer - EvolutionFleetRules.SlamLock + 1) / 10f, 0, 1);
            EvolutionVisuals.Line(CrashStart, CrashGround, EvolutionVisuals.Core * (.65f * fade), 2, false);
            EvolutionVisuals.Ring(CrashGround, 95, 0, EvolutionVisuals.Blood * (.7f * fade), 2, false);
        }
        int t = Timer % EvolutionFleetRules.ZigStride;
        if (Attack == 15 && Timer / EvolutionFleetRules.ZigStride < 3 && t >= EvolutionFleetRules.ZigLock && t < EvolutionFleetRules.ZigLaunch)
            EvolutionVisuals.AimLine(NPC.Center, NPC.Center + DashDirection * (44 * EvolutionCorridorPressureRules.Speed * (EvolutionFleetRules.ZigEnd - EvolutionFleetRules.ZigLaunch)),
                (t - EvolutionFleetRules.ZigLock + 1f) / (EvolutionFleetRules.ZigLaunch - EvolutionFleetRules.ZigLock));
    }
}

public sealed partial class EvolutionHazard
{
    private void DrawFleetBolt()
    {
        float opacity = Math.Clamp((Lifetime - Age) / 12f, 0, 1);
        Vector2 d = Projectile.velocity.SafeNormalize(Vector2.UnitX);
        if (Age < Delay)
        {
            float warning = Math.Clamp(Age / 8f, 0, 1);
            EvolutionVisuals.Glow(Projectile.Center, EvolutionVisuals.Blood * (.35f * warning), new Vector2(18));
            EvolutionVisuals.Line(Projectile.Center, Projectile.Center + d * (Parameter > 0 ? Projectile.velocity.Length() * (Lifetime - Delay - 12) : 90),
                (Parameter > 0 ? Color.Lerp(EvolutionVisuals.Blood, Color.White, .3f) : EvolutionVisuals.Core) * ((Parameter > 0 ? .24f : .45f) * warning), 1, false);
            return;
        }
        if (Parameter > 0)
        {
            Vector2 normal = d.RotatedBy(MathHelper.PiOver2), tip = Projectile.Center + d * 8;
            EvolutionVisuals.Glow(Projectile.Center, EvolutionVisuals.Blood * (.65f * opacity), new Vector2(30, 12), d.ToRotation());
            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 shoulder = Projectile.Center - d * 4 + normal * (side * 4);
                EvolutionVisuals.Line(tip, shoulder, EvolutionVisuals.Blood * opacity, 1.5f, false);
                EvolutionVisuals.Line(shoulder, Projectile.Center - d * 20, EvolutionVisuals.Blood * (.35f * opacity), 1, false);
            }
            EvolutionVisuals.Line(Projectile.Center - d * 3, tip, new Color(255, 215, 223) * opacity, 1.6f, false);
            return;
        }
        EvolutionVisuals.Glow(Projectile.Center, EvolutionVisuals.Blood * (.8f * opacity), new Vector2(24, 12), d.ToRotation());
        EvolutionVisuals.Line(Projectile.Center - d * 6, Projectile.Center + d * 6, EvolutionVisuals.Core * opacity, 3, false);
    }
}
