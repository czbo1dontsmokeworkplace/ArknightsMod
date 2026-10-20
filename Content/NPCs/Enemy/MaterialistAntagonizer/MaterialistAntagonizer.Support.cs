using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

public sealed partial class MaterialistAntagonizer
{
    internal int SupportMode, SupportAge, SupportTarget = -1, SupportSlot = -1;
    internal Vector2 SupportOrigin;
    internal int SuppressedCoreRound = -1;
    internal bool CoreSuppressed => SupportMode != 0 || SuppressedCoreRound == Revision * 4 + Math.Max(0, (Timer - 60) / 180);
    private int supportClock, supportSequence, healedSlots;
    internal NPC RepairTarget => SupportTarget >= 0 && SupportTarget < Main.maxNPCs &&
        Main.npc[SupportTarget].active && Main.npc[SupportTarget].TryGetGlobalNPC<MaterialistWing>(out var wing) &&
        wing.BelongsTo(this) && !wing.Retreating && wing.Slot == SupportSlot ? Main.npc[SupportTarget] : null;
    internal float RepairRadius => 1400 * MaterialistFormation.Smooth((SupportAge - 42) / 60f);

    private void UpdateSupport()
    {
        if (Paused || !EscortFire)
        {
            if (SupportMode != 0) { SupportMode = SupportAge = 0; SupportTarget = SupportSlot = -1; if (Authority) NPC.netUpdate = true; }
            return;
        }
        if (Authority && SupportMode == 0 && ++supportClock >= (supportSequence == 0 ? 540 : 900))
        {
            NPC lowest = null;
            foreach (NPC escort in Main.ActiveNPCs)
                if (escort.TryGetGlobalNPC<MaterialistWing>(out var wing) && wing.BelongsTo(this) && !wing.Retreating &&
                    wing.Age >= MaterialistRules.EscortGrace && escort.life < escort.lifeMax &&
                    (lowest == null || escort.life < lowest.life)) lowest = escort;
            if (lowest != null)
            {
                SupportMode = supportSequence++ % 2 == 0 ? 1 : 2;
                SupportTarget = lowest.whoAmI; SupportSlot = lowest.GetGlobalNPC<MaterialistWing>().Slot;
                SupportAge = supportClock = healedSlots = 0;
                SupportOrigin = NPC.Center;
                NPC.netUpdate = true;
            }
        }
        if (SupportMode == 0) return;
        SuppressedCoreRound = Revision * 4 + Math.Max(0, (Timer - 60) / 180);
        SupportAge++;
        if (SupportMode == 1 && SupportAge == 42)
        {
            SupportOrigin = NPC.Center;
            if (Authority) NPC.netUpdate = true;
        }
        if (Authority && SupportMode == 1 && SupportAge >= 42)
            foreach (NPC escort in Main.ActiveNPCs)
            {
                if (!escort.TryGetGlobalNPC<MaterialistWing>(out var wing) || !wing.BelongsTo(this) || wing.Retreating ||
                    (healedSlots & (1 << wing.Slot)) != 0 || SupportAge < 102 && escort.Distance(SupportOrigin) > RepairRadius) continue;
                healedSlots |= 1 << wing.Slot;
                RestoreEscort(escort, (int)Math.Ceiling(escort.lifeMax * .90f));
            }
        if (Authority && SupportMode == 2)
        {
            NPC target = RepairTarget;
            if (target == null) { SupportMode = SupportAge = 0; NPC.netUpdate = true; return; }
            // Two full-repair pulses normally; phase-clear waiting scales each pulse to 67% of missing life.
            if (SupportAge is 48 or 72) RestoreEscort(target, target.lifeMax - target.life);
        }
        if (!Main.dedServ && (SupportMode == 1 && SupportAge == 42 || SupportMode == 2 && SupportAge == 36))
        {
            MaterialistEffects.Pulse(NPC.Center, SupportMode == 1 ? 500 : 150, new Color(62, 255, 143), 1);
            SoundEngine.PlaySound(SoundID.Item29 with { Volume = .7f, Pitch = -.3f }, NPC.Center);
        }
        if (SupportAge >= (SupportMode == 1 ? 112 : 90))
        {
            SupportMode = SupportAge = 0; SupportTarget = SupportSlot = -1;
            if (Authority) NPC.netUpdate = true;
        }
    }
    private void RestoreEscort(NPC escort, int amount)
    {
        // Apply to the actual cast as it lands, including casts started before the phase gate.
        if (FinalStand) amount = (int)Math.Floor(amount * .67f);
        amount = Math.Min(amount, escort.lifeMax - escort.life);
        if (amount <= 0) return;
        escort.life += amount;
        escort.HealEffect(amount);
        escort.netUpdate = true;
    }
}
