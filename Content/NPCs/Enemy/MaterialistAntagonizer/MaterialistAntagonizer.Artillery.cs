using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

public sealed partial class MaterialistAntagonizer
{
    // Shared by carrier and escorts, so a heavy salvo changes everyone's assignment in the same round.
    // The other two rounds retain the existing geometry/payload battery and independent scout plans.
    internal ArtilleryPattern ArtilleryPlan => !CoreSequenceActive ? ArtilleryPattern.None :
        CoreAttack switch {
            RocketAttack => ArtilleryPattern.RocketRelay, CannonAttack => ArtilleryPattern.WalkingBarrage,
            CrossfireAttack => ArtilleryPattern.Crossfire, _ => ArtilleryPattern.None
        };
    internal bool ArtilleryActive => ArtilleryPlan != ArtilleryPattern.None;
    internal bool ArtilleryFormation => ArtilleryActive;
    internal int ArtilleryDirection => (CoreSerial + Wave) % 2 == 0 ? -1 : 1;
    internal Vector2 RocketPort(int side) => NPC.Center + new Vector2(side * 62, -27).RotatedBy(NPC.rotation) * 1.45f;
    internal Vector2 CannonPort(int side) => NPC.Center + new Vector2(side * 34, 31).RotatedBy(NPC.rotation) * 1.45f;
    internal int RocketCount => ArtilleryPlan == ArtilleryPattern.Crossfire ? 2 + Phase * 2 : 4 + Phase * 2;
    internal int RocketCadence => ArtilleryPlan == ArtilleryPattern.Crossfire ? 10 : 10 - Phase;
    internal bool RocketBeat(int beat) => ArtilleryPlan is ArtilleryPattern.RocketRelay or ArtilleryPattern.Crossfire &&
        beat >= 10 && (beat - 10) % RocketCadence == 0 && (beat - 10) / RocketCadence < RocketCount;
    internal bool CannonBeat(int beat) => ArtilleryPlan == ArtilleryPattern.WalkingBarrage ?
        beat >= 16 && (beat - 16) % 22 == 0 && (beat - 16) / 22 < (Phase == 1 ? 3 : 4) :
        ArtilleryPlan == ArtilleryPattern.Crossfire && beat is 30 or 74;

    private bool RunArtilleryBattery(int beat)
    {
        if (!ArtilleryActive) return false;
        Player player = Main.player[NPC.target];
        if (RocketBeat(beat))
        {
            int index = (beat - 10) / RocketCadence;
            int side = (index % 2 == 0 ? -1 : 1) * ArtilleryDirection;
            // Cold launch from alternating dorsal racks; the motor and guidance start in free flight.
            Vector2 velocity = new(side * (4.6f + index % 3 * .35f), -3.8f);
            velocity += MaterialistFlight.Limit(NPC.velocity, 20) * .12f;
            Shoot(DroneShot.CommandRocket, RocketPort(side), velocity, duration: 180,
                style: FireStyle.CoreFocus, bend: side);
        }
        if (CannonBeat(beat))
        {
            int index = ArtilleryPlan == ArtilleryPattern.WalkingBarrage ? (beat - 16) / 22 : beat == 30 ? 0 : 1;
            Vector2 lead = Vector2.Clamp(player.velocity * 12, new Vector2(-130, -70), new Vector2(130, 70));
            float lateral = ArtilleryPlan == ArtilleryPattern.WalkingBarrage ?
                (index - (Phase == 1 ? 1 : 1.5f)) * 170 * ArtilleryDirection : (index == 0 ? -170 : 170) * ArtilleryDirection;
            Vector2 mark = player.Center + lead + new Vector2(lateral, 65);
            FireCannonShell(mark, index % 2 == 0 ? -1 : 1);
            // The final phase adds a separated second shell, leaving space between impact circles.
            if (Phase == 3 && ArtilleryPlan == ArtilleryPattern.Crossfire)
                FireCannonShell(mark + new Vector2(-ArtilleryDirection * 230, -35), index % 2 == 0 ? 1 : -1);
        }
        // Precision shots land between the heavy arrivals rather than a full geometric wall on top.
        if (beat == 104)
        {
            CoreAim = player.Center + Vector2.Clamp(player.velocity * 7, new Vector2(-85), new Vector2(85));
            NPC.netUpdate = true;
        }
        if (beat == 132)
            for (int i = -1; i <= 1; i++)
                Shoot(DroneShot.Needle, CoreMuzzle, (CoreAim - CoreMuzzle).SafeNormalize(Vector2.UnitY).RotatedBy(i * .16f * MaterialistRules.SpreadSpacing) * (9 + Phase * .6f),
                    duration: 98, style: FireStyle.CoreFocus);
        return true;
    }
    private void FireCannonShell(Vector2 mark, int side)
    {
        mark = Vector2.Clamp(mark, new Vector2(100), new Vector2(Main.maxTilesX * 16 - 100, Main.maxTilesY * 16 - 100));
        Vector2 port = CannonPort(side);
        int flight = 72 + Phase * 4;
        float gravity = .26f + Phase * .02f;
        Vector2 velocity = (mark - port - Vector2.UnitY * (gravity * flight * (flight + 1) * .5f)) / flight;
        Shoot(DroneShot.CannonShell, port, velocity, duration: flight + 8, sweep: flight,
            length: Phase == 3 ? 84 : 76, style: FireStyle.CoreFocus, bend: gravity, mark: mark);
    }
    private Vector2 ArtilleryBodyOffset()
    {
        if (!ArtilleryFormation) return new Vector2(MathF.Sin(Timer * .012f) * 140, -290);
        int beat = Math.Max(0, CoreBeat);
        float side = ArtilleryDirection;
        Vector2 offset = ArtilleryPlan switch {
            ArtilleryPattern.RocketRelay => new Vector2(side * MathF.Sin(beat / 180f * MathHelper.Pi) * 185, -330),
            ArtilleryPattern.WalkingBarrage => new Vector2(side * MathHelper.Lerp(-160, 160, MaterialistFormation.Smooth(beat / 150f)), -310),
            _ => new Vector2(side * MathF.Sin(beat * .025f) * 125, -345)
        };
        return offset;
    }
}
