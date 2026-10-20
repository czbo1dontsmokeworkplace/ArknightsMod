using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

public sealed partial class MaterialistWing
{
    private int scoutSeenRevision = -1;
    private Vector2 scoutAim;
    private void PlanScout(NPC npc, MaterialistAntagonizer boss, ref Vector2 desired)
    {
        int beat = MaterialistScoutPlanner.LocalBeat(boss, Slot);
        ScoutMove move = boss.ScoutAttack(Slot);
        if (scoutSeenRevision != boss.ScoutRevision)
        {
            scoutSeenRevision = boss.ScoutRevision;
            planStart = npc.Center;
            if (MaterialistAntagonizer.Authority)
            {
                scoutAim = Main.player[boss.NPC.target].Center;
                npc.netUpdate = true;
            }
        }
        if (MaterialistAntagonizer.Authority && (beat == 12 || beat == 70 && move is ScoutMove.LockBurst or ScoutMove.SplitOrb))
        {
            Player target = Main.player[boss.NPC.target];
            scoutAim = target.Center + Vector2.Clamp(target.velocity * 6, new Vector2(-65), new Vector2(65));
            npc.netUpdate = true;
        }
        desired = MaterialistScoutPlanner.Position(move, beat, Slot, FlightCenter(boss), FlightExit(boss));
        Vector2 aim = move is ScoutMove.LaneBeam or ScoutMove.PulseGate ? boss.FormationCenter + boss.ExitAngle.ToRotationVector2() * 160 : scoutAim;
        aimAngle = (aim - Muzzle(npc)).ToRotation();
    }
    private void FireScout(NPC npc, MaterialistAntagonizer boss)
    {
        int beat = MaterialistScoutPlanner.LocalBeat(boss, Slot);
        if (beat < 0 || beat >= 154) return;
        ScoutMove move = boss.ScoutAttack(Slot);
        Vector2 muzzle = Muzzle(npc), f = boss.ExitAngle.ToRotationVector2(), n = f.RotatedBy(-MathHelper.PiOver2);
        float side = Slot % 2 == 0 ? -1 : 1;
        void Fan(DroneShot kind, int count, float spread, float speed, int delay = 0, float bend = 0)
        {
            for (int i = 0; i < count; i++)
                boss.Shoot(kind, muzzle, (aimAngle + (i - (count - 1) * .5f) * spread * MaterialistRules.SpreadSpacing).ToRotationVector2() * speed,
                    delay, 110, npc.whoAmI, style: move is ScoutMove.LockBurst or ScoutMove.OrbitVolley ? FireStyle.Precision : FireStyle.Suppression, bend: bend);
        }
        switch (move)
        {
            case ScoutMove.LaneBeam:
                if (beat == 28) boss.Shoot(DroneShot.Beam, muzzle, f, 40, 78, npc.whoAmI, length: 800, style: FireStyle.Containment);
                break;
            case ScoutMove.PulseGate:
                if (beat is 26 or 76)
                {
                    Vector2 point = boss.FormationCenter + n * side * 225 + f * (beat == 26 ? -80 : 115);
                    boss.Shoot(DroneShot.FluxMine, point, Vector2.Zero, 46, 24, npc.whoAmI, style: FireStyle.Containment);
                }
                if (beat == 102) Fan(DroneShot.ArcSpark, 3, .25f, 7.1f, bend: -side * .013f);
                break;
            case ScoutMove.ChaseLance:
                if (beat == 20) boss.Shoot(DroneShot.ChaseBeam, muzzle, (aimAngle - side * .42f).ToRotationVector2(),
                    54, 92, npc.whoAmI, length: 1900, style: FireStyle.Containment);
                break;
            case ScoutMove.LockBurst:
                if (beat is 34 or 82) Fan(DroneShot.Needle, 3, .10f, 8.2f, 28);
                break;
            case ScoutMove.SplitOrb:
                if (beat is 30 or 84) Fan(DroneShot.ClusterOrb, 1, 0, 5.8f, 12);
                break;
            case ScoutMove.OrbitVolley:
                if (beat == 24)
                    for (int i = 0; i < 4; i++)
                        boss.Shoot(DroneShot.OrbitSpark, muzzle, (aimAngle + (i - 1.5f) * .19f * MaterialistRules.SpreadSpacing).ToRotationVector2() * 7.4f,
                            38 + i * 7, 110, npc.whoAmI, style: FireStyle.Precision);
                break;
            case ScoutMove.Fan:
                if (beat is 56 or 85 or 116) Fan(DroneShot.Bolt, 4, .19f, 6);
                break;
            case ScoutMove.SpinRing:
                if (beat is >= 58 and < 100 && (beat - 58) % 9 == 0)
                    for (int i = 0; i < 2; i++)
                        boss.Shoot(DroneShot.Bolt, muzzle, ((beat - 58) / 42f * MathHelper.TwoPi + i * MathHelper.Pi + Slot * .2f).ToRotationVector2() * 5.6f,
                            duration: 110, shooter: npc.whoAmI, style: FireStyle.Suppression);
                break;
            case ScoutMove.Crescent:
                if (beat is 50 or 82 or 114) Fan(DroneShot.ArcSpark, 3, .24f, 7.3f, bend: -side * .018f);
                break;
            case ScoutMove.BombDash:
                if (beat is 92 or 98 or 104 or 110)
                    boss.Shoot(DroneShot.TrailBomb, npc.Center, Vector2.Zero, 42, 24, npc.whoAmI, style: FireStyle.Suppression);
                break;
            case ScoutMove.Strafe:
                if (beat is >= 54 and <= 130 && (beat - 54) % 18 == 0) Fan(DroneShot.Bolt, 2, .16f, 6.6f);
                break;
            case ScoutMove.CurveDive:
                if (beat is 64 or 80 or 96) Fan(DroneShot.Prism, 3, .18f, 7.2f);
                break;
        }
    }
    private void DrawScoutWarning(NPC npc, MaterialistAntagonizer boss)
    {
        if (!MaterialistTactics.Active(boss.Timer)) return;
        int beat = MaterialistScoutPlanner.LocalBeat(boss, Slot);
        ScoutMove move = boss.ScoutAttack(Slot);
        Color color = move switch {
            ScoutMove.BombDash => new Color(255, 77, 52),
            ScoutMove.ChaseLance or ScoutMove.OrbitVolley => MaterialistVisuals.Violet,
            ScoutMove.LaneBeam or ScoutMove.PulseGate or ScoutMove.Crescent => MaterialistVisuals.Cyan,
            _ => MaterialistVisuals.Amber
        };
        MaterialistVisuals.Brackets(npc.Center, new Vector2(npc.width + 20, npc.height + 16), color, .5f);
        if (move == ScoutMove.BombDash && beat is >= 41 and < 86)
        {
            Vector2 end = MaterialistScoutPlanner.Position(move, 111, Slot, FlightCenter(boss), FlightExit(boss));
            Vector2 normal = (end - npc.Center).SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2) * Math.Max(16, npc.height * .5f);
            MaterialistVisuals.Line(npc.Center - normal, end - normal, color * .65f, 2);
            MaterialistVisuals.Line(npc.Center + normal, end + normal, color * .65f, 2);
            for (int i = 1; i <= 4; i++) MaterialistVisuals.Ring(Vector2.Lerp(npc.Center, end, i / 5f), 9, color * .6f, 0, 1, .7f, 6);
        }
        if (move == ScoutMove.CurveDive && beat is >= 20 and < 64)
        {
            Vector2 last = MaterialistScoutPlanner.Position(move, 42, Slot, FlightCenter(boss), FlightExit(boss));
            for (int t = 50; t <= 142; t += 8)
            {
                Vector2 next = MaterialistScoutPlanner.Position(move, t, Slot, FlightCenter(boss), FlightExit(boss));
                MaterialistVisuals.Line(last, next, color * .3f, 1); last = next;
            }
        }
        if (beat is >= 8 and < 36 && move is not (ScoutMove.BombDash or ScoutMove.LaneBeam or ScoutMove.ChaseLance))
        {
            Vector2 muzzle = Muzzle(npc);
            int points = move == ScoutMove.SpinRing ? 12 : move is ScoutMove.SplitOrb or ScoutMove.OrbitVolley ? 6 : 4;
            MaterialistVisuals.Ring(muzzle, 18 - Math.Max(0, beat - 8) * .2f, color, beat * .035f, 2, 1, points);
            if (move is ScoutMove.LockBurst or ScoutMove.Strafe)
                MaterialistVisuals.Line(muzzle, muzzle + aimAngle.ToRotationVector2() * 400, color * .35f, 1);
        }
    }
}
