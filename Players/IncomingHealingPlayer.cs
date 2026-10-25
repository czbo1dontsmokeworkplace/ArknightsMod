using System;
using ArknightsMod.Content.Items.Armor.Guard.Matoimaru;
using ArknightsMod.Content.Items.Armor.NeoArmorReforge;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Players;

public sealed class IncomingHealingPlayer : ModPlayer
{
    private double remainder;
    internal int BypassDepth;
    public bool HealingBlocked => Player.GetModPlayer<global::ArknightsMod.Content.Items.Armor.Guard.Surtr.SurtrSetPlayer>().HealingBlocked;
    // Read equipment directly: server-side heals can arrive outside equipment updates.
    public float Multiplier => !Player.dead && Player.armor[0].type > 0
        && Player.armor[0].type == NeoArmorReforgeSetLoader.GetSetType<MatoimaruHead>() ? 1.25f : 1f;

    public int Modify(int amount)
    {
        if (amount <= 0) return amount;
        // Final network grants bypass amplification, never healing suppression.
        if (HealingBlocked) { remainder = 0; return 0; }
        if (BypassDepth > 0) return amount;
        if (Multiplier == 1f || Player.statLife >= Player.statLifeMax2) { remainder = 0; return amount; }
        // Keep fractional healing so repeated 1 HP heals also receive the full bonus.
        double total = amount * (double)Multiplier + remainder;
        int result = (int)Math.Min(int.MaxValue, Math.Floor(total));
        remainder = total - result;
        return result;
    }

    // Network grants already contain the recipient bonus. Apply them exactly once.
    public void HealFinal(int amount)
    {
        BypassDepth++;
        try { Player.Heal(amount); }
        finally { BypassDepth--; }
    }

    public void HealSilent(int amount) => Player.statLife = Math.Min(Player.statLifeMax2,
        Player.statLife + Modify(amount));

    // Preserve skills that restore a target HP value without applying healing bonuses.
    public void RestoreLife(int targetLife)
    {
        if (targetLife > Player.statLife && HealingBlocked) return;
        Player.statLife = Math.Min(Player.statLifeMax2, targetLife);
    }

    public override void UpdateDead() => remainder = 0;
    public override void PostUpdate()
    {
        if (Multiplier == 1f || Player.statLife >= Player.statLifeMax2) remainder = 0;
    }
}
