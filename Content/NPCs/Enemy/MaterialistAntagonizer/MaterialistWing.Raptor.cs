using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

public sealed partial class MaterialistWing
{
    private enum RaptorLeg : byte { Waiting, Depart, Scan, Strafe, Egress, Support }
    private const int RaptorScanTicks = 84, RaptorStrafeTicks = 210, RaptorEgressTicks = 90;
    private RaptorLeg raptorLeg;
    private int raptorLegAge, raptorSortie, raptorHeading = 1;
    private int raptorRearCountdown = -1;
    private float raptorCruiseY;
    private Vector2 raptorRunStart, raptorGround, raptorScanSize;
    private readonly Vector2[] raptorGroundProfile = new Vector2[7];
    private readonly float[] raptorTerrain = new float[3];
    private ulong raptorLastShot = ulong.MaxValue;
    private static readonly Color RaptorGold = new(255, 196, 66);
    private bool RaptorFinal => MaterialistAntagonizer.Owner(OwnerIndex, Encounter)?.Phase == 3;
    private bool RaptorHorizontal => RaptorFinal && Slot == 9;
    private float RaptorEntryPitch => RaptorHorizontal ? 0 : RaptorFinal ? MathHelper.ToRadians(65) : MathHelper.PiOver4;

    private bool RaptorWindow(MaterialistAntagonizer boss) => boss != null && !Retreating && Age >= MaterialistRules.EscortGrace &&
        boss.EscortFire && boss.Order != FlightOrder.Deploy && !boss.Paused &&
        raptorLeg == RaptorLeg.Strafe && (boss.Phase == 3 ? raptorRearCountdown != 0 : raptorLegAge < RaptorStrafeTicks);
    private bool RaptorShot(MaterialistAntagonizer boss) => RaptorWindow(boss) &&
        (boss.Phase == 3 || raptorLegAge % (raptorLegAge < 24 ? 4 : 2) == 0);
    private float RaptorPitch => RaptorHorizontal ? 0 : RaptorFinal ? MathHelper.ToRadians(65) : MathHelper.ToRadians(MathHelper.Lerp(45, 70,
        MaterialistFormation.Smooth(raptorLegAge / (float)RaptorStrafeTicks)));
    // Terraria's Y axis points down: (+cos65,+sin65) is the user's right/-65 degrees;
    // (-cos65,+sin65) is left/245 degrees in the usual Y-up angle convention.
    private Vector2 RaptorDirection(float pitch) => new(raptorHeading * MathF.Cos(pitch), MathF.Sin(pitch));

    private bool CanBeginRaptorPass(MaterialistAntagonizer boss)
    {
        if (!boss.EscortsReleased || boss.Paused) return false;
        bool nextAlive = false;
        foreach (NPC escort in Main.ActiveNPCs)
        {
            if (!escort.TryGetGlobalNPC<MaterialistWing>(out var wing) || !wing.BelongsTo(boss) ||
                wing.Retreating || Role(escort) != WingRole.Raptor) continue;
            // The relief aircraft prepares during the other's two-second tail; actual fire still waits for handoff.
            if (wing.raptorLeg == RaptorLeg.Strafe) return false;
            if (wing.Slot == boss.RaptorNextSlot) nextAlive = true;
        }
        return Slot == boss.RaptorNextSlot || !nextAlive;
    }

    private void FlyRaptorSortie(NPC npc, MaterialistAntagonizer boss)
    {
        npc.damage = 0;
        if (raptorRelayAge >= 0) raptorRelayAge++;
        var partner = FiringRaptorPartner(npc, boss);
        if (MaterialistAntagonizer.Authority && boss.Phase == 3 && !boss.Paused && boss.EscortsReleased &&
            raptorLeg != RaptorLeg.Strafe && partner != null && partner.raptorRearCountdown < 0 && raptorRelayAge < 0 &&
            raptorLeg != RaptorLeg.Support)
            SetRaptorLeg(npc, RaptorLeg.Support);
        if (raptorLeg == RaptorLeg.Support && !boss.Paused)
        {
            FlyRaptorSupport(npc, boss);
            if (MaterialistAntagonizer.Authority && (partner == null || partner.raptorRearCountdown >= 0))
            {
                BeginRaptorSortie(npc, boss);
                raptorRelayAge = 0;
                npc.netUpdate = true;
            }
            FireRaptorSupport(npc, boss, partner);
            raptorLegAge++;
            UpdateRaptorAirframe(npc);
            return;
        }
        if (raptorLeg == RaptorLeg.Waiting)
        {
            // Launch as two staggered aircraft, without inheriting the carrier's future formation goals.
            npc.velocity = MaterialistFlight.Approach(npc.velocity, new Vector2(Slot % 2 == 0 ? -1 : 1, -.3f), .10f);
            npc.direction = npc.spriteDirection = Slot % 2 == 0 ? 1 : -1;
            if (MaterialistAntagonizer.Authority && !boss.Paused && Age >= MaterialistRules.EscortGrace + (Slot - 8) * 120)
                BeginRaptorSortie(npc, boss);
            return;
        }
        if (boss.Paused)
        {
            if (MaterialistAntagonizer.Authority && raptorLeg != RaptorLeg.Egress) SetRaptorLeg(npc, RaptorLeg.Egress);
            CoastRaptor(npc, new Vector2(raptorHeading * 3.6f, -1.1f), .075f);
            raptorLegAge++;
            UpdateRaptorAirframe(npc);
            return;
        }
        switch (raptorLeg)
        {
            case RaptorLeg.Depart:
                // Relief preparation overlaps the outgoing stream, with a bounded smooth approach.
                MoveRaptorTo(npc, raptorRunStart, raptorRelayAge >= 0 ? 16 : 8.5f, raptorRelayAge >= 0 ? .32f : .17f);
                TurnRaptorGun(RaptorDirection(RaptorEntryPitch), .008f);
                if (MaterialistAntagonizer.Authority && npc.Distance(raptorRunStart) < 52 && npc.velocity.LengthSquared() < 5)
                    CommitRaptorScan(npc, boss);
                else if (MaterialistAntagonizer.Authority && boss.Phase == 3 && raptorLegAge >= 240)
                    CommitRaptorScan(npc, boss);
                // If the player left the selected area, still finish the old route. Only an unreachable approach is replanned.
                else if (MaterialistAntagonizer.Authority && raptorLegAge > 420) BeginRaptorSortie(npc, boss);
                break;
            case RaptorLeg.Scan:
                MoveRaptorTo(npc, raptorRunStart, 3.8f, .16f);
                TurnRaptorGun(RaptorDirection(RaptorEntryPitch), .006f);
                if (!Main.dedServ && raptorLegAge == 1)
                    SoundEngine.PlaySound(SoundID.Item93 with { Volume = .45f, Pitch = -.55f, MaxInstances = 2 }, npc.Center);
                if (MaterialistAntagonizer.Authority && RaptorScanReady && boss.Order != FlightOrder.Deploy &&
                    (boss.Phase == 3 || npc.Distance(raptorRunStart) < 70 &&
                    Math.Abs(MathHelper.WrapAngle(aimAngle - RaptorDirection(RaptorEntryPitch).ToRotation())) < .025f))
                {
                    if (CanBeginRaptorPass(boss))
                        SetRaptorLeg(npc, RaptorLeg.Strafe);
                }
                break;
            case RaptorLeg.Strafe:
                if (boss.Phase == 3)
                {
                    if (raptorRearCountdown < 0 && MaterialistAntagonizer.Authority)
                    {
                        foreach (Player player in Main.ActivePlayers)
                            if (!player.dead && !player.ghost && (player.Center.X - npc.Center.X) * raptorHeading < 0)
                            {
                                // First rear-half-plane detection commits the stop. No continuous occupancy requirement.
                                raptorRearCountdown = 120;
                                PrepareRaptorRelief(npc, boss);
                                npc.netUpdate = true;
                                break;
                            }
                    }
                    else if (raptorRearCountdown > 0 && --raptorRearCountdown == 0 && MaterialistAntagonizer.Authority)
                    {
                        SetRaptorLeg(npc, RaptorLeg.Egress);
                        break;
                    }
                    if (!RaptorHorizontal)
                    {
                        npc.Center = new Vector2(npc.Center.X, raptorCruiseY);
                        npc.velocity.Y = 0;
                    }
                }
                // A slow ground-attack pass in world coordinates: neither this velocity nor the gun reads the player.
                float progress = raptorLegAge / (float)RaptorStrafeTicks;
                float speed = boss.Phase == 3 ? 3.6f * (1 + MathHelper.Clamp(raptorLegAge / 150f, 0, 1)) :
                    3.6f + MathF.Sin(progress * MathHelper.Pi) * .65f;
                // Preserve the entire acceleration profile, with 1.5x speed and acceleration while strafing.
                float multiplier = boss.Phase == 3 ? 1.5f : 1;
                if (RaptorHorizontal)
                {
                    Player target = Main.player[boss.NPC.target];
                    // Horizontal gun axis stays fixed. The airframe can only rise slowly towards a higher target.
                    float rise = MathHelper.Clamp((target.Center.Y - npc.Center.Y) * .01f, -1.5f, 0);
                    CoastRaptor(npc, new Vector2(raptorHeading * .6f, rise), .055f);
                }
                else CoastRaptor(npc, new Vector2(raptorHeading * speed, boss.Phase == 3 ? 0 : .12f) * multiplier, .095f * multiplier);
                if (boss.Phase == 3) aimAngle = RaptorDirection(RaptorPitch).ToRotation();
                else TurnRaptorGun(RaptorDirection(RaptorPitch), .004f);
                if (MaterialistAntagonizer.Authority) FireRaptor(npc, boss);
                if (MaterialistAntagonizer.Authority && boss.Phase != 3 && raptorLegAge >= RaptorStrafeTicks) SetRaptorLeg(npc, RaptorLeg.Egress);
                break;
            case RaptorLeg.Egress:
                CoastRaptor(npc, new Vector2(raptorHeading * 5.2f, -2.1f), .08f);
                if (MaterialistAntagonizer.Authority && raptorLegAge >= RaptorEgressTicks) BeginRaptorSortie(npc, boss);
                break;
        }
        FireRaptorSupport(npc, boss, partner);
        raptorLegAge++;
        UpdateRaptorAirframe(npc);
    }
    private void BeginRaptorSortie(NPC npc, MaterialistAntagonizer boss)
    {
        // Ground attacks sample altitude only here. Horizontal attacks may slowly rise after firing, without turning.
        bool relief = raptorRelayAge >= 0;
        Player player = Main.player[boss.NPC.target];
        Vector2 sample = player.Center;
        raptorHeading = boss.Phase == 3 ? (npc.Center.X < sample.X ? 1 : -1) :
            (raptorSortie + Slot) % 2 == 0 ? 1 : -1;
        if (boss.Phase == 3 && (sample.X - raptorHeading * 520 < 64 || sample.X - raptorHeading * 520 > Main.maxTilesX * 16 - 64))
            raptorHeading *= -1;
        raptorSortie++;
        float centerX = MathHelper.Clamp(sample.X + (Slot == 8 ? -80 : 80), 900, Main.maxTilesX * 16 - 900);
        float top = float.MaxValue, bottom = 0;
        for (int i = 0; i < raptorGroundProfile.Length; i++)
        {
            float x = centerX + (i - 3) * 170;
            Vector2 scan = new(x, MathHelper.Clamp(sample.Y - 180, 80, Main.maxTilesY * 16 - 1300));
            float reach = 1200;
            if (Collision.SolidCollision(scan - Vector2.One * 2, 4, 4)) reach = 0;
            else
            {
                Collision.LaserScan(scan, Vector2.UnitY, 4, reach, raptorTerrain);
                reach = Math.Min(raptorTerrain[0], Math.Min(raptorTerrain[1], raptorTerrain[2]));
            }
            float y = reach < 1190 ? scan.Y + reach : sample.Y + 240;
            y = MathHelper.Clamp(y, 180, Main.maxTilesY * 16 - 160);
            raptorGroundProfile[i] = new Vector2(x, y);
            top = Math.Min(top, y); bottom = Math.Max(bottom, y);
        }
        raptorGround = new Vector2(centerX, (top + bottom) * .5f);
        raptorScanSize = new Vector2(1120, Math.Max(150, bottom - top + 100));
        float cruiseY = RaptorHorizontal ? sample.Y : boss.Phase == 3 ?
            sample.Y - Main.rand.NextFloat(10 * 16 + 2, 20 * 16 - 2) : top - 420 - (Slot - 8) * 55;
        float runX = boss.Phase == 3 ? sample.X - raptorHeading * 520 : centerX - raptorHeading * 520;
        raptorRunStart = new Vector2(MathHelper.Clamp(runX, 64, Main.maxTilesX * 16 - 64),
            MathHelper.Clamp(cruiseY, 64, Main.maxTilesY * 16 - 64));
        SetRaptorLeg(npc, RaptorLeg.Depart);
        if (relief) raptorRelayAge = 0;
    }
    private void SetRaptorLeg(NPC npc, RaptorLeg leg)
    {
        if (MaterialistAntagonizer.Authority && raptorLeg == RaptorLeg.Strafe && leg != RaptorLeg.Strafe)
        {
            var boss = MaterialistAntagonizer.Owner(OwnerIndex, Encounter);
            if (boss != null)
            {
                boss.RaptorNextSlot = Slot == 8 ? 9 : 8;
                boss.RaptorHandoffTicks = 0;
                boss.NPC.netUpdate = true;
            }
        }
        raptorLeg = leg; raptorLegAge = 0;
        if (leg == RaptorLeg.Strafe)
        {
            raptorCruiseY = npc.Center.Y;
            raptorRearCountdown = -1;
            raptorRelayAge = -1;
        }
        npc.netUpdate = true;
    }
    private static void CoastRaptor(NPC npc, Vector2 velocity, float acceleration) =>
        npc.velocity = MaterialistFlight.Approach(npc.velocity, velocity * MaterialistRules.Mobility, acceleration * MaterialistRules.Mobility);
    private static void MoveRaptorTo(NPC npc, Vector2 point, float maximum, float acceleration)
    {
        Vector2 desired = MaterialistFlight.Limit((point - npc.Center) * .055f, maximum);
        CoastRaptor(npc, desired, acceleration);
    }
    private void TurnRaptorGun(Vector2 direction, float maximumTurn) =>
        aimAngle += MathHelper.Clamp(MathHelper.WrapAngle(direction.ToRotation() - aimAngle), -maximumTurn, maximumTurn);
    private void UpdateRaptorAirframe(NPC npc)
    {
        if (raptorLeg is RaptorLeg.Scan or RaptorLeg.Strafe) npc.direction = npc.spriteDirection = raptorHeading;
        else if (Math.Abs(npc.velocity.X) > .4f) npc.direction = npc.spriteDirection = Math.Sign(npc.velocity.X);
        float bank = raptorLeg == RaptorLeg.Egress ? -raptorHeading * .13f : MathHelper.Clamp(npc.velocity.X * .012f, -.18f, .18f);
        npc.rotation = MathHelper.Lerp(npc.rotation, bank, .035f);
        if (!Main.dedServ && Age % (MaterialistVisuals.Reduced ? 12 : 5) == 0)
            MaterialistBallistics.AirWake(npc, RaptorWindow(MaterialistAntagonizer.Owner(OwnerIndex, Encounter)));
    }
    private void FireRaptor(NPC npc, MaterialistAntagonizer boss)
    {
        if (!RaptorShot(boss) || raptorLastShot == Main.GameUpdateCount) return;
        raptorLastShot = Main.GameUpdateCount;
        Vector2 direction = aimAngle.ToRotationVector2();
        Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
        Vector2 muzzle = Muzzle(npc) + normal * Main.rand.NextFloat(-4, 4);
        float pitch = boss.Phase == 3 ? RaptorPitch :
            MathHelper.Clamp(RaptorPitch + Main.rand.NextFloat(-.045f, .045f), MathHelper.PiOver4, MathHelper.ToRadians(70));
        boss.Shoot(DroneShot.MachineRound, muzzle, RaptorDirection(pitch) * 15,
            duration: 24, shooter: npc.whoAmI, style: FireStyle.Suppression);
    }
    private void DrawRaptorWarning(NPC npc, MaterialistAntagonizer boss)
    {
        if (raptorLeg is RaptorLeg.Waiting or RaptorLeg.Egress or RaptorLeg.Support) return;
        Vector2 muzzle = Muzzle(npc), direction = aimAngle.ToRotationVector2();
        if (raptorLeg == RaptorLeg.Depart) return;
        if (RaptorHorizontal)
        {
            if (raptorLeg == RaptorLeg.Scan)
            {
                Vector2 end = muzzle + new Vector2(raptorHeading * 1200, 0);
                for (float distance = 0; distance < 1200; distance += 52)
                    MaterialistVisuals.Line(muzzle + new Vector2(raptorHeading * distance, 0),
                        muzzle + new Vector2(raptorHeading * Math.Min(1200, distance + 22), 0), RaptorGold * .6f, 2);
                MaterialistVisuals.Line(muzzle - Vector2.UnitY * 15, end - Vector2.UnitY * 15, RaptorGold * .18f, 1);
                MaterialistVisuals.Line(muzzle + Vector2.UnitY * 15, end + Vector2.UnitY * 15, RaptorGold * .18f, 1);

            }
        }
        else
        {
            Vector2 half = raptorScanSize * .5f;
            float alpha = raptorLeg == RaptorLeg.Scan ? .8f : .35f;
            MaterialistVisuals.Brackets(raptorGround, raptorScanSize, RaptorGold, alpha);
            for (int i = 1; i < raptorGroundProfile.Length; i++)
                MaterialistVisuals.Line(raptorGroundProfile[i - 1], raptorGroundProfile[i], RaptorGold * (alpha * .65f), 2);
            if (raptorLeg == RaptorLeg.Scan)
            {
                float scan = MathHelper.Clamp(raptorLegAge / (float)RaptorScanTicks, 0, 1);
                float x = MathHelper.Lerp(-half.X, half.X, raptorHeading > 0 ? scan : 1 - scan);
                Vector2 stripe = raptorGround + new Vector2(x, 0);
                MaterialistVisuals.Line(stripe - Vector2.UnitY * half.Y, stripe + Vector2.UnitY * half.Y, MaterialistVisuals.White * .85f, 3);
                MaterialistVisuals.Glow(stripe, RaptorGold * .3f, new Vector2(30, raptorScanSize.Y));
                for (int edge = 0; edge < (boss.Phase == 3 ? 1 : 2); edge++)
                {
                    Vector2 ray = RaptorDirection(boss.Phase == 3 ? MathHelper.ToRadians(65) : MathHelper.ToRadians(edge == 0 ? 45 : 70));
                    float reach = MathHelper.Clamp((raptorGround.Y - muzzle.Y) / Math.Max(.1f, ray.Y), 160, 1500);
                    for (float d = 0; d < reach; d += 60)
                        MaterialistVisuals.Line(muzzle + ray * d, muzzle + ray * Math.Min(reach, d + 24), RaptorGold * .25f, 1);
                }

            }
        }
        if (RaptorWindow(boss))
        {
            float flash = RaptorShot(boss) ? 1 : .55f;
            MaterialistVisuals.Glow(muzzle + direction * 16, RaptorGold * flash, new Vector2(88, 37), aimAngle);
            MaterialistVisuals.Glow(muzzle + direction * 10, MaterialistVisuals.White * (.8f * flash), new Vector2(44, 17), aimAngle);
        }
    }
    private void WriteRaptorAI(BinaryWriter w)
    {
        w.Write((byte)raptorLeg); w.Write(raptorLegAge); w.Write(raptorSortie); w.Write(raptorHeading);
        w.Write(raptorRearCountdown); w.Write(raptorCruiseY);
        w.Write(raptorRelayAge);
        w.Write(raptorSupportClock);
        w.WriteVector2(raptorRunStart); w.WriteVector2(raptorGround); w.WriteVector2(raptorScanSize);
        foreach (Vector2 point in raptorGroundProfile) w.WriteVector2(point);
    }
    private void ReadRaptorAI(BinaryReader r)
    {
        raptorLeg = (RaptorLeg)r.ReadByte(); raptorLegAge = r.ReadInt32(); raptorSortie = r.ReadInt32(); raptorHeading = r.ReadInt32();
        raptorRearCountdown = r.ReadInt32(); raptorCruiseY = r.ReadSingle();
        raptorRelayAge = r.ReadInt32();
        raptorSupportClock = r.ReadInt32();
        raptorRunStart = r.ReadVector2(); raptorGround = r.ReadVector2(); raptorScanSize = r.ReadVector2();
        for (int i = 0; i < raptorGroundProfile.Length; i++) raptorGroundProfile[i] = r.ReadVector2();
    }
}
