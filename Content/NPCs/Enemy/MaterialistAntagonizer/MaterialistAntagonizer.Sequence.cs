using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

public sealed partial class MaterialistAntagonizer
{
    internal const int PincerAttack = 12, SaucerAttack = 13, RocketAttack = 14, CannonAttack = 15,
        CrossfireAttack = 16, PulseAttack = 17, SkyAttack = 18, GravityAttack = 19,
        BeaconAttack = 20, ScanAttack = 21, RepairAttack = 22, BomberAttack = 23;
    internal bool BomberOpeningUsed;
    internal int BombersLaunched;
    internal int CoreAttack = -1, CoreAttackAge, CoreSerial, AttacksSinceRepair;
    internal bool CoreWaiting;
    internal int CoreCooldownRemaining;
    internal WingPattern ContinuousWingPlan;
    internal Vector2 PulseEmitter;
    internal bool CoreSequenceActive => Order == FlightOrder.Command && EscortsReleased && CoreAttack >= 0 && !CoreWaiting;
    internal bool BomberSortieInProgress => Phase == 3 && Order == FlightOrder.Command &&
        CoreAttack == BomberAttack && !CoreWaiting && BombersLaunched < 6;
    internal int CoreBeat => CoreAttackAge;

    private int CoreAttackDuration => CoreAttack switch
    {
        PincerAttack => 8 + PincerWarningTicks + PincerDurationTicks,
        PulseAttack => 103,
        SkyAttack => 96,
        GravityAttack => 96,
        BeaconAttack => 132,
        ScanAttack => MaterialistRules.ScanWarning + 24,
        RepairAttack => SupportMode == 1 ? 112 : 90,
        BomberAttack => 192,
        _ => 136
    };

    private void ResetCoreSequence()
    {
        CoreAttack = -1; CoreAttackAge = AttacksSinceRepair = 0;
        CoreWaiting = false; CoreCooldownRemaining = 0;
        SupportMode = SupportAge = 0;
        SupportTarget = SupportSlot = -1;
    }

    private void SelectCoreAttack()
    {
        CoreWaiting = false; CoreCooldownRemaining = 0;
        int previous = CoreAttack;
        if (previous >= 0 && previous != RepairAttack) AttacksSinceRepair++;
        if (Phase == 3 && !BomberOpeningUsed)
        {
            CoreAttack = BomberAttack;
            BomberOpeningUsed = true;
        }
        else if (AttacksSinceRepair >= 4 && BeginScheduledRepair())
        {
            CoreAttack = RepairAttack;
            AttacksSinceRepair = 0;
        }
        else
        {
            // Uniform random instruments without consecutive repeats; escorts keep their own operation clock.
            int count = ScanAttack + 1 + (Phase == 3 ? 1 : 0);
            bool previousInPool = previous is >= 0 and <= ScanAttack || Phase == 3 && previous == BomberAttack;
            CoreAttack = AttackChoice(Phase, previous, Main.rand.Next(count - (previousInPool ? 1 : 0)));
        }
        BombersLaunched = 0;
        CoreAttackAge = 0; CoreSerial++;
        PulseEmitter = Vector2.Clamp(CoreMuzzle + new Vector2((CoreSerial % 2 == 0 ? -1 : 1) * 84, 35),
            new Vector2(48), new Vector2(Main.maxTilesX * 16 - 48, Main.maxTilesY * 16 - 48));
        NPC.netUpdate = true;
    }

    private void RunCoreSequence()
    {
        if (!EscortsReleased || Order != FlightOrder.Command) return;
        if (CoreWaiting)
        {
            if (CoreCooldownRemaining > 0) CoreCooldownRemaining--;
            if (CoreCooldownRemaining > 0 || !Authority) return;
            SelectCoreAttack();
        }
        else if (Authority && CoreAttack >= 0 && (CoreAttack != RepairAttack && CoreAttackAge >= CoreAttackDuration ||
            CoreAttack == RepairAttack && CoreAttackAge > 0 && SupportMode == 0) &&
            (CoreAttack != BomberAttack || BombersLaunched >= 6))
        {
            CoreWaiting = true; CoreCooldownRemaining = 90;
            NPC.netUpdate = true;
            return;
        }
        if (Authority && CoreAttack < 0) SelectCoreAttack();
        if (CoreAttack < 0) return;
        int beat = CoreAttackAge;
        if (Authority)
        {
            Player player = Main.player[NPC.target];
            if (CoreAttack <= CrossfireAttack) RunCoreBarrage(beat);
            else if (CoreAttack == PulseAttack && beat >= 28 && (beat - 28) % 13 == 0 && (beat - 28) / 13 < 5)
            {
                Vector2 direction = (player.Center - PulseEmitter).SafeNormalize(Vector2.UnitY);
                SpawnInstrument(false, PulseEmitter, player.Center, 8, direction);
            }
            else if (CoreAttack == SkyAttack && beat == 0)
            {
                // Sample the whole constellation once; later player motion never drags the markers.
                float rotation = Main.rand.NextFloat(MathHelper.TwoPi);
                for (int i = 0; i < 14; i++)
                {
                    float angle = rotation + i * MathHelper.TwoPi / 14 + Main.rand.NextFloat(-.12f, .12f);
                    Vector2 mark = player.Center + angle.ToRotationVector2() * Main.rand.NextFloat(160, 620);
                    mark = Vector2.Clamp(mark, new Vector2(64), new Vector2(Main.maxTilesX * 16 - 64, Main.maxTilesY * 16 - 64));
                    Vector2 origin = new(mark.X + Main.rand.NextFloat(-90, 90), Math.Max(32, mark.Y - 1200));
                    SpawnInstrument(true, origin, mark, 56 + i * 2, Vector2.UnitY);
                }
            }
            else if (CoreAttack == GravityAttack) RunPayloadBattery(beat, 0);
            else if (CoreAttack == BeaconAttack) RunPayloadBattery(beat, 2);
            else if (CoreAttack == ScanAttack && beat == 0) FireAreaScan(player);
            else if (CoreAttack == BomberAttack && Phase == 3) RunBomberSortie(beat);
        }
        CoreAttackAge++;
    }

    internal static int AttackChoice(int phase, int previous, int choice)
    {
        int previousIndex = previous == BomberAttack ? ScanAttack + 1 : previous;
        bool exclude = previous is >= 0 and <= ScanAttack || phase == 3 && previous == BomberAttack;
        int index = exclude && choice >= previousIndex ? choice + 1 : choice;
        return index > ScanAttack ? BomberAttack : index;
    }
}
