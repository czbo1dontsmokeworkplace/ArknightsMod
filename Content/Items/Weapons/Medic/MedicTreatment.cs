using System;
using System.Collections.Generic;
using ArknightsMod.Content.Projectiles.Medic.Arts;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Medic;

internal static class MedicTreatment
{
    // Never cleanse potion cooldowns, resistance, or encounter-specific mechanic debuffs.
    private static readonly HashSet<int> Cleanseable =
    [
        BuffID.Poisoned, BuffID.OnFire, BuffID.Bleeding, BuffID.Darkness, BuffID.Cursed,
        BuffID.Slow, BuffID.Weak, BuffID.BrokenArmor, BuffID.Silenced, BuffID.Confused,
        BuffID.Chilled, BuffID.Frozen, BuffID.Frostburn, BuffID.Venom, BuffID.Ichor,
        BuffID.CursedInferno, BuffID.Blackout, BuffID.Electrified, BuffID.ShadowFlame,
        BuffID.Stoned, BuffID.Obstructed, BuffID.WitheredArmor, BuffID.WitheredWeapon,
        BuffID.OnFire3, BuffID.Frostburn2
    ];
    internal static bool HasCurableDebuff(Player player)
    {
        foreach (int type in player.buffType) if (Cleanseable.Contains(type)) return true;
        return false;
    }

    internal static bool TryTreat(Player healer, Player patient, int profileId, float power, int interval)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || !MedicHealingNet.Allied(healer, patient)) return false;
        var profile = MedicCatalog.Get(profileId);
        List<int> removed = [];
        if (profile.Branch == MedicBranch.Wandering)
            for (int i = patient.buffType.Length - 1; i >= 0 && removed.Count < profile.CleanseLimit; i--)
                if (Cleanseable.Contains(patient.buffType[i]))
                {
                    removed.Add(patient.buffType[i]); patient.DelBuff(i);
                }
        var state = patient.GetModPlayer<MedicalTreatmentPlayer>();
        float emergency = profileId == 4 && patient.statLife < patient.statLifeMax2 * .4f ? 1.4f : 1f;
        int requested = Math.Clamp((int)MathF.Round(profile.Healing * power * emergency * (1 + removed.Count * .35f)), 1, 400);
        bool fullHealth = patient.statLife >= patient.statLifeMax2;
        int amount = Math.Min(state.Resistance.Scale(requested), Math.Max(0, patient.statLifeMax2 - patient.statLife));
        // A full-health ally still catches the art, but an overheal cannot commit resistance or regeneration.
        int displayed = fullHealth ? state.Resistance.Scale(requested) : amount;
        if (!fullHealth && amount <= 0 && removed.Count == 0) return false;
        if (amount > 0)
        {
            state.Resistance.Commit(interval, profile.ResistanceDurationMultiplier, profile.ResistanceReduction);
            if (profile.Branch == MedicBranch.Chain)
                state.Regeneration.Add(profile.StackDuration, profile.RegenPerStack * power, profile.StackLimit, profileId);
            if (Main.netMode == NetmodeID.SinglePlayer) patient.Heal(amount);
            else patient.statLife = Math.Min(patient.statLifeMax2, patient.statLife + amount);
        }
        else if (fullHealth && Main.netMode == NetmodeID.SinglePlayer && !Main.dedServ) patient.HealEffect(displayed);
        // Negative grants are presentation only: damage taken during transit cannot turn an overheal into real healing.
        if (Main.netMode == NetmodeID.Server) state.SendState(heal: fullHealth ? -displayed : amount, profile: profileId, removed: removed.ToArray());
        else
        {
            MedicArtsAudio.Treatment(patient, profileId, removed.Count);
            MedicArtsVfx.Treatment(patient.Center, profileId, removed.Count);
        }
        return true;
    }
}
