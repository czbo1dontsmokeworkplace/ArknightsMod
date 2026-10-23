using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

public sealed partial class MaterialistWing
{
    private void PlanArtilleryEscort(NPC npc, MaterialistAntagonizer boss, ref Vector2 desired)
    {
        if (!boss.ArtilleryFormation || Role(npc) == WingRole.Raptor) return;
        int beat = Math.Max(0, boss.CoreBeat);
        WingDuty duty = MaterialistRules.Duty(boss.Phase, Slot);
        float side = Slot % 2 == 0 ? -1 : 1;
        Vector2 f = boss.ExitAngle.ToRotationVector2(), n = f.RotatedBy(-MathHelper.PiOver2);
        float travel = MaterialistFormation.Smooth((beat - 25) / 105f);
        Vector2 local = duty switch {
            // Broad outside rails frame the rocket approaches and shell landing bands.
            WingDuty.Containment => new Vector2(side * (350 + MathF.Sin(beat * .025f) * 30), -235),
            WingDuty.Precision => new Vector2(side * (140 + MathF.Sin(beat * .025f + Slot) * 35), -250 + travel * 85),
            // Curved flank advance instead of standing beside the carrier and waiting for a salvo.
            WingDuty.Scatter => new Vector2(side * (300 - MathF.Sin(travel * MathHelper.Pi) * 65), -80 + travel * 170),
            _ => new Vector2(side * (205 + MathF.Sin(beat * .045f + Slot) * 60), -90 + MathF.Sin(beat * .06f + Slot) * 80)
        };
        if (boss.ArtilleryPlan == ArtilleryPattern.WalkingBarrage)
            local.X += boss.ArtilleryDirection * MathF.Sin(travel * MathHelper.Pi) * 65;
        desired = boss.FormationCenter + n * local.X + f * local.Y;
        aimAngle = (Main.player[boss.NPC.target].Center - Muzzle(npc)).ToRotation();
    }
    private bool FireArtilleryEscort(NPC npc, MaterialistAntagonizer boss)
    {
        if (!boss.ArtilleryActive) return false;
        // The two physical gunships retain their independent, alternating ground/aimed gun streams.
        if (Role(npc) == WingRole.Raptor) { FireRaptor(npc, boss); return true; }
        int beat = boss.CoreBeat;
        WingDuty duty = MaterialistRules.Duty(boss.Phase, Slot);
        Vector2 muzzle = Muzzle(npc), f = boss.ExitAngle.ToRotationVector2();
        Player player = Main.player[boss.NPC.target];
        int offset = Slot % 2 * 12;
        if (duty == WingDuty.Containment)
        {
            if (boss.ArtilleryPlan == ArtilleryPattern.WalkingBarrage)
            {
                // Staggered side-lane packets accompany the walking impact points.
                if (beat >= 28 + offset && beat <= 118 + offset && (beat - 28 - offset) % 18 == 0)
                    for (int i = -1; i <= 1; i++)
                    {
                        Vector2 velocity = f.RotatedBy(i * .13f * MaterialistRules.SpreadSpacing) * 8.2f;
                        if (MaterialistFormation.CrossesExit(muzzle, velocity, boss.FormationCenter, boss.ExitAngle, 80)) continue;
                        boss.Shoot(DroneShot.ArcSpark, muzzle, velocity, 14, 96, npc.whoAmI,
                            style: FireStyle.Containment, bend: -Math.Sign(Slot % 2 - .5f) * .003f);
                    }
            }
            else if (beat == 38 + offset || beat == 98 + offset)
                for (int i = -1; i <= 1; i += 2)
                    boss.Shoot(DroneShot.ScoutPulse, muzzle, (player.Center - muzzle).SafeNormalize(Vector2.UnitY).RotatedBy(i * .17f * MaterialistRules.SpreadSpacing) * 6.4f,
                        20, MaterialistRules.ScoutLifetime, npc.whoAmI, style: FireStyle.Suppression);
            return true;
        }
        if (duty == WingDuty.Precision)
        {
            // Both shots carry their own fixed warning; the aim is not continually dragged after firing.
            if (beat == 52 + offset || beat == 94 + offset)
            {
                Vector2 target = player.Center + Vector2.Clamp(player.velocity * 9, new Vector2(-95), new Vector2(95));
                Vector2 direction = (target - muzzle).SafeNormalize(Vector2.UnitY);
                for (int i = -1; i <= 1; i++)
                    boss.Shoot(boss.Phase == 1 ? DroneShot.Prism : DroneShot.CoreLance, muzzle, direction.RotatedBy(i * .13f * MaterialistRules.SpreadSpacing) * 9.4f,
                        30, 102, npc.whoAmI, style: FireStyle.Precision);
            }
            return true;
        }
        if (duty == WingDuty.Scatter)
        {
            int start = boss.ArtilleryPlan == ArtilleryPattern.RocketRelay ? 82 : 62;
            int step = beat - start - Slot % 2 * 7;
            if (step >= 0 && step <= 48 && step % 12 == 0)
            {
                // One escort sweeps clockwise, the other counterclockwise as the heavy shots arrive.
                float sweep = (Slot % 2 == 0 ? 1 : -1) * MathHelper.Lerp(-.65f, .65f, step / 48f);
                Vector2 direction = (player.Center - muzzle).SafeNormalize(Vector2.UnitY).RotatedBy(sweep);
                for (int i = -1; i <= 1; i++)
                    boss.Shoot(boss.Phase == 1 ? DroneShot.Bolt : DroneShot.Petal, muzzle, direction.RotatedBy(i * .22f * MaterialistRules.SpreadSpacing) * 7.6f,
                        duration: 102, shooter: npc.whoAmI, style: FireStyle.Suppression, bend: boss.Phase == 1 ? 0 : .012f);
            }
            return true;
        }
        // Interceptors escort the salvo along curved paths, firing diversion bursts before and after impact.
        if (beat == 40 + offset || beat == 126 + offset)
        {
            float axis = (player.Center - muzzle).ToRotation();
            for (int i = -1; i <= 1; i++)
                boss.Shoot(DroneShot.ArcSpark, muzzle, (axis + i * .21f * MaterialistRules.SpreadSpacing).ToRotationVector2() * 8.4f,
                    18, 96, npc.whoAmI, style: FireStyle.Suppression, bend: Slot % 2 == 0 ? .008f : -.008f);
        }
        return true;
    }
    private void DrawArtilleryEscort(NPC npc, MaterialistAntagonizer boss)
    {
        int beat = boss.CoreBeat;
        WingDuty duty = MaterialistRules.Duty(boss.Phase, Slot);
        Color color = DutyColor(duty);
        MaterialistVisuals.Brackets(npc.Center, new Vector2(npc.width + 18, npc.height + 14), color, .55f);
        if (MaterialistVisuals.Reduced || beat >= 45 || Vector2.DistanceSquared(npc.Center, boss.NPC.Center) > 1000 * 1000) return;
        Vector2 port = boss.CannonPort(Slot % 2 == 0 ? -1 : 1);
        // A travelling packet on each assignment link makes the coordinated launch visible without labels.
        MaterialistVisuals.Line(port, npc.Center, color * .12f, 1);
        Vector2 packet = Vector2.Lerp(port, npc.Center, (beat * .035f + Slot * .13f) % 1);
        MaterialistVisuals.Brackets(packet, new Vector2(8), color, .65f);
    }
}
