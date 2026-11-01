using System;
using ArknightsMod.Content.Items.Armor.Specialist.Mortis;
using ArknightsMod.Systems.Gameplay;
using ArknightsMod.Systems.Gameplay.Elemental;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Players;

// Player afflictions are separate from the NPC-only AfflictionContainer.
public sealed class NecrosisPlayer : ModPlayer
{
    public const int Threshold = NecrosisState.Threshold, BurstDuration = NecrosisState.Duration;
    private readonly NecrosisState state = new();
    public int Accumulation => state.Accumulation;
    public int BurstTime => state.Remaining;
    public bool IsBursting => state.Active;
    internal bool ApplyingBurstDamage { get; private set; }
    public static bool BlocksSkill(Player player) => player.GetModPlayer<NecrosisPlayer>() is { } necrosis
        && (necrosis.IsBursting || necrosis.ApplyingBurstDamage)
        || MortisSetPlayer.BlocksActions(player);
    public void AddDamage(int amount) {
        if (amount <= 0 || Player.dead || IsBursting || !Player.GetModPlayer<MortisSetPlayer>().IsAuthority) return;
        if (!state.Add(amount)) return;
        if (MortisSetPlayer.HasSet(Player)) MujicaFeverSystem.ReportMortisDamage(Player);
        SyncState();
    }
    public void ClearDamage() {
        state.Clear();
        SyncState();
    }
    internal void SetState(int accumulation, int remaining) {
        state.Set(accumulation, remaining);
    }
    public override void PostUpdate() {
        if (IsBursting || Accumulation > 0)
            Player.AddBuff(ModContent.BuffType<Content.Buffs.ArmorSets.NecrosisAfflictionBuff>(), 2);
        if (!IsBursting || Player.dead) return;
        if (!Player.GetModPlayer<MortisSetPlayer>().IsAuthority) {
            if (BurstTime > 1) state.Tick();
            return;
        }
        // Exactly fifteen pulses, the first after one second and the last at expiry.
        if (!state.Tick()) return;
        Player.GetModPlayer<WeaponPlayer>().DrainSP(1);
        ApplyingBurstDamage = true;
        try {
            // Arts damage ignores physical defense; keep the normal hurt/death pipeline.
            Player.Hurt(PlayerDeathReason.ByCustomReason(Terraria.Localization.Language.GetTextValue(
                "Mods.ArknightsMod.Mortis.NecrosisDeath", Player.name)), 10, 0,
                dodgeable: false, scalingArmorPenetration: 1f);
        }
        finally { ApplyingBurstDamage = false; }
        SyncState();
    }
    public override void UpdateDead() {
        if (Accumulation > 0 || BurstTime > 0) ClearDamage();
    }
    public override void SyncPlayer(int toWho, int fromWho, bool newPlayer) =>
        MujicaFeverSystem.SyncNecrosis(Player, toWho);
    private void SyncState() => MujicaFeverSystem.SyncNecrosis(Player);
}
