using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

public sealed partial class MaterialistAntagonizer
{
    internal Vector2 CoreAim, CoreSpreadAim;
    internal CorePattern CorePlan => (CorePattern)Math.Clamp(CoreAttack, 0, 11);
    internal Vector2 CoreMuzzle => NPC.Center + new Vector2(4, 39).RotatedBy(NPC.rotation) * 1.45f;

    // Providence reference: alternating radial chains/rings with directed spears and an independent
    // guardian pressure window. These mechanical patterns and projectiles are implemented locally.
    private void RunCoreBarrage(int beat)
    {
        if (!Authority || CoreSuppressed) return;
        int round = CoreSerial;
        if (!CorePincerRound && PincerInProgress) return;
        if (RunPincerBattery(beat)) return;
        if (RunSaucerBattery(beat)) return;
        if (beat == 18 && (Cycle + round + Phase) % 3 == 0) FireAreaScan(Main.player[NPC.target]);
        if (RunArtilleryBattery(beat)) return;
        RunPayloadBattery(beat);
        Player player = Main.player[NPC.target];
        if (beat == 12) { CoreSpreadAim = player.Center; NPC.netUpdate = true; }
        if (beat == 82)
        {
            CoreAim = player.Center + Vector2.Clamp(player.velocity * 8, new Vector2(-90), new Vector2(90));
            NPC.netUpdate = true;
        }
        Vector2 origin = CoreMuzzle;
        float aim = (CoreSpreadAim - origin).ToRotation();
        float speed = 7.4f + Phase * .65f;
        int arms = MaterialistRules.SpreadRingCount(Phase == 3 ? 4 : 3);
        float gap = ExitAngle + MathF.Sin((int)CorePlan * .7f) * .42f;
        void Fire(DroneShot kind, Vector2 velocity, int life = 110, float bend = 0, bool fixedLeft = false, bool goldenFan = false) =>
            Shoot(kind, origin, velocity, duration: life, style: FireStyle.CoreSpread, bend: bend, fixedLeftTurn: fixedLeft, goldenFan: goldenFan);
        void Ring(int count, float offset, DroneShot kind, bool fixedLeft = false)
        {
            count = MaterialistRules.SpreadRingCount(count);
            for (int i = 0; i < count; i++)
            {
                float angle = offset + i * MathHelper.TwoPi / count;
                if (Math.Abs(MathHelper.WrapAngle(angle - gap)) < .28f) continue;
                Fire(kind, angle.ToRotationVector2() * speed, fixedLeft: fixedLeft);
            }
        }
        switch (CorePlan)
        {
            case CorePattern.TwinSpiral:
                if (beat is >= 24 and <= 64 && (beat - 24) % 10 == 0)
                    for (int i = 0; i < 2; i++)
                        Fire(DroneShot.ArcSpark, (aim + (beat - 24) * .055f + i * MathHelper.Pi).ToRotationVector2() * speed, bend: .009f);
                break;
            case CorePattern.BurstRings:
                if (beat is 24 or 54) Ring(Phase == 1 ? 16 : 20, beat == 24 ? .1f : .27f, DroneShot.Petal, fixedLeft: true);
                break;
            case CorePattern.SquareLattice:
                if (beat is 24 or 58)
                    for (int side = 0; side < 4; side++)
                        for (int j = 0; j < 2; j++)
                            Fire(DroneShot.Lattice, new Vector2(speed, MathF.Tan((j - .5f) * MathHelper.PiOver4) * speed).RotatedBy(side * MathHelper.PiOver2 + (beat == 58 ? .16f : 0)), 95);
                break;
            case CorePattern.Rose:
                if (beat is 24 or 56)
                    for (int i = 0; i < MaterialistRules.SpreadRingCount(16); i++)
                    {
                        float a = i * MathHelper.TwoPi / MaterialistRules.SpreadRingCount(16) + (beat == 56 ? .16f : 0);
                        if (Math.Abs(MathHelper.WrapAngle(a - gap)) < .28f) continue;
                        Fire(DroneShot.Petal, a.ToRotationVector2() * (speed + MathF.Cos(a * 4) * 2), bend: i % 2 == 0 ? .013f : -.013f);
                    }
                break;
            case CorePattern.AimedFan:
                if (beat is 24 or 46 or 66)
                    for (int i = -2; i <= 2; i++) Fire(DroneShot.Prism, (aim + i * .17f * MaterialistRules.SpreadSpacing * 1.25f + (beat == 46 ? .08f : 0)).ToRotationVector2() * (speed + 1), goldenFan: true);
                break;
            case CorePattern.NeedleVolley:
                if (beat is >= 24 and <= 64 && (beat - 24) % 10 == 0)
                    for (int i = -1; i <= 1; i++) Fire(DroneShot.Needle, (aim + i * .09f * MaterialistRules.SpreadSpacing + MathF.Sin(beat * .15f) * .16f).ToRotationVector2() * (speed + 1.6f));
                break;
            case CorePattern.ScissorArcs:
                if (beat is >= 24 and <= 66 && (beat - 24) % 14 == 0)
                    for (int sign = -1; sign <= 1; sign += 2)
                        Fire(DroneShot.ArcSpark, (aim + sign * .7f * MaterialistRules.SpreadSpacing).ToRotationVector2() * speed, bend: -sign * .018f);
                break;
            case CorePattern.SineRain:
                if (beat is 24 or 54)
                    for (int i = -3; i <= 3; i++)
                    {
                        Vector2 port = origin + new Vector2(i * 28, 0);
                        Shoot(DroneShot.Petal, port, (ExitAngle + i * .13f * MaterialistRules.SpreadSpacing).ToRotationVector2() * speed,
                            duration: 105, style: FireStyle.CoreSpread, bend: i % 2 == 0 ? .028f : -.028f);
                    }
                break;
            case CorePattern.OrbCascade:
                if (beat is 24 or 52)
                    for (int i = -1; i <= 1; i++) Fire(DroneShot.ClusterOrb, (aim + i * .32f * MaterialistRules.SpreadSpacing).ToRotationVector2() * 5.8f, 100);
                break;
            case CorePattern.Pinwheel:
                if (beat is >= 24 and <= 64 && (beat - 24) % 8 == 0)
                    for (int i = 0; i < arms; i++)
                        Fire(DroneShot.ArcSpark, (beat * .06f + i * MathHelper.TwoPi / arms).ToRotationVector2() * speed, 95, .006f);
                break;
            case CorePattern.Diamond:
                if (beat is 24 or 56)
                    for (int i = 0; i < MaterialistRules.SpreadRingCount(16); i++)
                    {
                        Vector2 d = (i * MathHelper.TwoPi / MaterialistRules.SpreadRingCount(16) + MathHelper.PiOver4).ToRotationVector2();
                        if (Math.Abs(MathHelper.WrapAngle(d.ToRotation() - gap)) < .28f) continue;
                        Fire(DroneShot.Lattice, d / (Math.Abs(d.X) + Math.Abs(d.Y)) * (speed * 1.25f), 100);
                    }
                break;
            case CorePattern.ConvergingLances:
                if (beat is 24 or 44 or 64)
                    for (int sign = -1; sign <= 1; sign += 2)
                    {
                        Vector2 port = origin + new Vector2(sign * 95, -18);
                        Shoot(DroneShot.CoreLance, port, (CoreSpreadAim - port).SafeNormalize(Vector2.UnitY) * (speed + 1.4f),
                            18, 105, style: FireStyle.CoreFocus);
                    }
                break;
        }
        // A second, focused instrument overlaps the escort pressure after the geometric opening.
        // Its target is sampled once at beat 82; the firing corridor is shown before both salvos.
        if (beat is 106 or 128)
        {
            float focus = (CoreAim - origin).ToRotation();
            int half = Phase == 1 ? 1 : 2;
            for (int i = -half; i <= half; i++)
                Shoot(beat == 106 ? DroneShot.CoreLance : DroneShot.Needle, origin,
                    (focus + i * .105f * MaterialistRules.SpreadSpacing).ToRotationVector2() * (9.5f + Phase * .6f), duration: 105, style: FireStyle.CoreFocus);
        }
    }
}
