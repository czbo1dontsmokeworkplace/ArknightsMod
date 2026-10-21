using System;
using Microsoft.Xna.Framework;
using Terraria;
using ArknightsMod.Content.NPCs.Enemy.ThroughChapter4;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

public sealed partial class MaterialistWing
{
    private int planRevision = -1;
    private Vector2 planStart, launchOrigin;
    private int lastSalvoTimer = -1;
    internal static Color DutyColor(WingDuty duty) => duty switch {
        WingDuty.Containment => MaterialistVisuals.Cyan,
        WingDuty.Precision => MaterialistVisuals.Violet,
        WingDuty.Interceptor => new Color(255, 78, 62),
        WingDuty.Heavy => new Color(255, 195, 65), _ => MaterialistVisuals.Amber
    };
    private void FlyFormation(NPC npc, MaterialistAntagonizer boss)
    {
        int t = boss.Timer, cap = MaterialistRules.WingCap(boss.Phase);
        int beat = MaterialistTactics.Beat(t);
        bool scout = boss.ScoutReady && Slot < 6 && !boss.ArtilleryFormation;
        WingDuty duty = MaterialistRules.Duty(boss.Phase, Slot);
        float side = Slot % 2 == 0 ? -1 : 1;
        if (Age == 1) launchOrigin = npc.Center - boss.NPC.Center;
        if (npc.ModNPC is ArtsMasterDrone)
            npc.frame.Y = Age / 6 % 8 * (npc.ModNPC is ArtsMasterA2 ? 46 : 38);
        if (Role(npc) == WingRole.Raptor && Age >= MaterialistRules.EscortGrace)
        {
            FlyRaptorSortie(npc, boss);
            return;
        }
        if (planRevision != boss.Revision)
        {
            planRevision = boss.Revision; planStart = npc.Center; lastSalvoTimer = -1;
            if (MaterialistAntagonizer.Authority) npc.netUpdate = true;
        }
        PrepareFlight(npc, boss);
        Vector2 desired = MaterialistTactics.Position(boss.Pattern, t, Slot, cap, FlightCenter(boss), FlightExit(boss));
        if (scout) PlanScout(npc, boss, ref desired);
        PlanArtilleryEscort(npc, boss, ref desired);
        bool executing = boss.EscortFire && boss.Order != FlightOrder.Deploy && MaterialistTactics.Active(t) && !(boss.Order == FlightOrder.Ram && t >= 114);
        bool raptor = Role(npc) == WingRole.Raptor;
        if (!boss.EscortFire || boss.Order == FlightOrder.Deploy || boss.Order == FlightOrder.Ram && t >= 114)
            desired = boss.NPC.Center + new Vector2(side * (145 + Slot / 2 * 24), 75 + Slot / 2 * 24);
        desired = BlendFlightGoal(npc, boss, desired);
        if (Age < MaterialistRules.EscortGrace)
        {
            Vector2 rack = new(side * (165 + Slot / 2 * 20), 75 + Slot / 2 * 22);
            desired = boss.NPC.Center + MaterialistFormation.Bezier(launchOrigin, launchOrigin + new Vector2(0, 65), rack + new Vector2(-side * 60, 55), rack,
                MaterialistFormation.Smooth(Age / 72f));
        }
        desired = Vector2.Clamp(desired, new Vector2(100), new Vector2(Main.maxTilesX * 16 - 100, Main.maxTilesY * 16 - 100));
        bool dive = executing && !boss.ArtilleryActive && Age >= MaterialistRules.EscortGrace && (scout ?
            MaterialistScoutPlanner.Diving(boss.ScoutAttack(Slot), MaterialistScoutPlanner.LocalBeat(boss, Slot)) : MaterialistTactics.Diving(boss.Pattern, t, Slot, cap));
        float speed = dive ? scout || boss.Pattern == WingPattern.BombRun ? 38 : 34 : scout && boss.ScoutAttack(Slot) == ScoutMove.CurveDive ? 25 : 18;
        MoveEscort(npc, boss, desired, speed);
        npc.direction = npc.spriteDirection = boss.FormationCenter.X < npc.Center.X ? -1 : 1;
        bool spinning = executing && !boss.ArtilleryActive && (scout ? MaterialistScoutPlanner.Spinning(boss.ScoutAttack(Slot), MaterialistScoutPlanner.LocalBeat(boss, Slot)) :
            MaterialistTactics.Spinning(boss.Pattern, t, Slot, cap));
        npc.rotation = spinning ? ((scout ? MaterialistScoutPlanner.LocalBeat(boss, Slot) - 58 : beat - 66) / 42f * MathHelper.TwoPi) :
            MathHelper.Lerp(MathHelper.WrapAngle(npc.rotation), MathHelper.Clamp(npc.velocity.X * .018f, -.45f, .45f), .2f);
        npc.damage = dive ? 80 : 0;
        if (!scout && !raptor && !boss.ArtilleryActive && (duty != WingDuty.Precision || beat <= 52 + Slot % 2 * 12))
        {
            Player target = Main.player[boss.NPC.target];
            Vector2 aim = duty == WingDuty.Precision ? target.Center + Vector2.Clamp(target.velocity * 8, new Vector2(-80), new Vector2(80)) :
                boss.FormationCenter + boss.ExitAngle.ToRotationVector2() * (boss.Pattern == WingPattern.Gate ? -90 : 110);
            aimAngle = (aim - Muzzle(npc)).ToRotation();
        }
        if (executing && Age >= MaterialistRules.EscortGrace && MaterialistAntagonizer.Authority && lastSalvoTimer != t)
        {
            lastSalvoTimer = t;
            FireFormation(npc, boss);
        }
        FireTransitCover(npc, boss, executing, desired);
        if (!Main.dedServ && Age % (MaterialistVisuals.Reduced ? 12 : 4) == 0 && (npc.velocity.LengthSquared() > 16 || Age < 72))
            MaterialistVisuals.Nano(npc.Center - npc.velocity, -npc.velocity * .3f, DutyColor(duty), dive ? 1.3f : .7f, 22);
    }
    private void FireFormation(NPC npc, MaterialistAntagonizer boss)
    {
        if (FireArtsProtocol(npc, boss, MaterialistRules.Duty(boss.Phase, Slot), MaterialistTactics.Beat(boss.Timer))) return;
        if (FireArtilleryEscort(npc, boss)) return;
        if (boss.ScoutReady && Slot < 6) { FireScout(npc, boss); return; }
        if (Role(npc) == WingRole.Raptor) { FireRaptor(npc, boss); return; }
        int beat = MaterialistTactics.Beat(boss.Timer);
        WingDuty duty = MaterialistRules.Duty(boss.Phase, Slot);
        if (boss.Pattern == WingPattern.BombRun && MaterialistTactics.Bomber(duty, Slot))
        {
            int run = beat - MaterialistTactics.BombDiveBeat(Slot);
            if (run is >= 5 and < 26 && (run - 5) % 6 == 0)
                boss.Shoot(DroneShot.TrailBomb, npc.Center, Vector2.Zero, 42, 24, npc.whoAmI, style: FireStyle.Suppression);
            return;
        }
        TacticalShot shot = MaterialistTactics.Shot(duty, boss.Pattern, beat, Slot);
        Vector2 f = boss.ExitAngle.ToRotationVector2();
        if (boss.Pattern == WingPattern.TrackingLance && duty == WingDuty.Containment && Slot == 0)
        {
            if (beat == 20)
            {
                Vector2 target = Main.player[boss.NPC.target].Center;
                Vector2 direction = (target - Muzzle(npc)).SafeNormalize(Vector2.UnitY).RotatedBy(-.42f);
                boss.Shoot(DroneShot.ChaseBeam, Muzzle(npc), direction, 54, 100, npc.whoAmI, length: 1900, style: FireStyle.Containment);
            }
            return;
        }
        if (shot == TacticalShot.Rail)
        {
            // World-locked side lanes; never derive these high-speed shots from player aim.
            Vector2 n = f.RotatedBy(-MathHelper.PiOver2);
            Vector2 origin = boss.FormationCenter + n * (Slot % 2 == 0 ? -285 : 285) - f * 260;
            for (int i = 0; i < 3; i++)
                boss.Shoot(DroneShot.RailSlug, origin, f * 26, 60 + i * 8, 32, npc.whoAmI, style: FireStyle.Containment);
            return;
        }
        if (shot == TacticalShot.Mine)
        {
            // Anchored side pockets, with a 120-pixel central escape strip kept empty.
            Vector2 n = f.RotatedBy(-MathHelper.PiOver2);
            Vector2 point = boss.FormationCenter + n * (Slot % 2 == 0 ? -185 : 185) + f * (beat == 44 ? -65 : 90);
            boss.Shoot(DroneShot.FluxMine, point, Vector2.Zero, 60, 24, npc.whoAmI, style: FireStyle.Suppression);
            return;
        }
        if (shot == TacticalShot.Pursuit)
        {
            // Slow, single-target curved light; guidance ends before the shot expires.
            boss.Shoot(DroneShot.Seeker, Muzzle(npc), (aimAngle + (Slot % 2 == 0 ? -.45f : .45f)).ToRotationVector2() * 4.4f,
                18, 118, npc.whoAmI, style: FireStyle.Precision);
            return;
        }
        if (shot == TacticalShot.Boundary)
        {
            boss.Shoot(DroneShot.Beam, Muzzle(npc), f, 40, 78, npc.whoAmI, length: MaterialistTactics.BoundaryLength, style: FireStyle.Containment);
            return;
        }
        if (shot == TacticalShot.Snipe)
        {
            if (boss.Pattern is WingPattern.PrismCross or WingPattern.PursuitNet)
                boss.Shoot(DroneShot.Prism, Muzzle(npc), aimAngle.ToRotationVector2() * 7.5f, 42, 112, npc.whoAmI, style: FireStyle.Precision);
            else boss.Shoot(DroneShot.Beam, Muzzle(npc), aimAngle.ToRotationVector2(), 40, 14, npc.whoAmI, length: 1050, style: FireStyle.Precision);
            npc.netUpdate = true;
            return;
        }
        if (shot == TacticalShot.Circle)
        {
            float a = (beat - 66) / 42f * MathHelper.TwoPi;
            // Seven pairs from both scatterers form interleaved rings in the same pressure window.
            for (int i = 0; i < 2; i++)
            {
                Vector2 v = (a + i * MathHelper.Pi).ToRotationVector2() * 5.6f;
                if (boss.Pattern == WingPattern.Spiral && MaterialistFormation.CrossesExit(Muzzle(npc), v, boss.FormationCenter, boss.ExitAngle, 100)) continue;
                boss.Shoot(boss.Phase >= 2 ? DroneShot.Prism : DroneShot.Bolt, Muzzle(npc), v, duration: 110, shooter: npc.whoAmI, style: FireStyle.Suppression);
            }
        }
        if (shot != TacticalShot.Fan) return;
        int count = duty == WingDuty.Heavy ? 5 : 3;
        float angle = duty == WingDuty.Heavy ? f.ToRotation() + (Slot % 2 == 0 ? -.28f : .28f) : aimAngle;
        for (int i = 0; i < count; i++)
        {
            Vector2 v = (angle + (i - (count - 1) * .5f) * (duty == WingDuty.Heavy ? .19f : .23f) * MaterialistRules.SpreadSpacing).ToRotationVector2() * 6;
            int life = boss.Pattern == WingPattern.Gate ? 48 : 110;
            if (boss.Pattern == WingPattern.Gate && MaterialistFormation.CrossesExit(Muzzle(npc), v, boss.FormationCenter, boss.ExitAngle, life)) continue;
            DroneShot ammo = boss.Pattern is WingPattern.Battery or WingPattern.PrismCross || duty == WingDuty.Heavy ? DroneShot.Prism : DroneShot.Bolt;
            if (boss.Pattern == WingPattern.RailCorridor) ammo = DroneShot.Missile;
            boss.Shoot(ammo, Muzzle(npc), v, duration: life, shooter: npc.whoAmI, style: FireStyle.Suppression);
        }
    }
    private void DrawFlightWarning(NPC npc, MaterialistAntagonizer boss, Color unused)
    {
        if (boss == null || Retreating) return;
        WingDuty duty = MaterialistRules.Duty(boss.Phase, Slot);
        Color accent = DutyColor(duty);
        if (Role(npc) == WingRole.Raptor)
        {
            if (Age >= MaterialistRules.EscortGrace && boss.EscortFire) DrawRaptorWarning(npc, boss);
            return;
        }
        if (Age < 72)
        {
            MaterialistVisuals.Brackets(npc.Center, new Vector2(npc.width + 20, npc.height + 18), accent, .85f);
            return;
        }
        if (!boss.EscortFire) return;
        if (boss.ArtilleryActive && Role(npc) != WingRole.Raptor) { DrawArtilleryEscort(npc, boss); return; }
        if (boss.ScoutReady && Slot < 6) { DrawScoutWarning(npc, boss); return; }
        int beat = MaterialistTactics.Beat(boss.Timer), cap = MaterialistRules.WingCap(boss.Phase);
        if (boss.Order == FlightOrder.Ram && boss.Timer >= 114) return;
        bool bomber = boss.Pattern == WingPattern.BombRun && MaterialistTactics.Bomber(duty, Slot);
        if (MaterialistTactics.Active(boss.Timer) && (bomber || duty == WingDuty.Interceptor &&
            boss.Pattern is WingPattern.RelayDive or WingPattern.HeavyHunt or WingPattern.Scissor or WingPattern.FinalVolley))
        {
            int start = bomber ? MaterialistTactics.BombDiveBeat(Slot) : MaterialistTactics.DiveBeat(Slot);
            if (beat >= start - 45 && beat < start)
            {
                int endTime = boss.Timer - beat + start + (bomber ? 25 : 19);
                Vector2 end = MaterialistTactics.Position(boss.Pattern, endTime, Slot, cap, FlightCenter(boss), FlightExit(boss));
                Vector2 normal = (end - npc.Center).SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2) * Math.Max(16, npc.height * .5f);
                MaterialistVisuals.Line(npc.Center - normal, end - normal, accent * .65f, 2);
                MaterialistVisuals.Line(npc.Center + normal, end + normal, accent * .65f, 2);
            }
        }
        if (MaterialistTactics.Active(boss.Timer))
        {
            MaterialistVisuals.Brackets(npc.Center, new Vector2(npc.width + 14, npc.height + 12), accent, .45f);
            if (duty is WingDuty.Scatter or WingDuty.Heavy && beat is >= 40 and < 65)
                MaterialistVisuals.Line(Muzzle(npc), Muzzle(npc) + aimAngle.ToRotationVector2() * 230, accent * .4f, 1);
        }
    }
}
