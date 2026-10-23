using ArknightsMod.Content.Items.Armor.Defender.Saria;
using ArknightsMod.Content.Items.Armor.Defender.Spot;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor;

// Called once after authoritative direct healing, never by regeneration ticks.
internal static class DirectHealingEffects
{
    internal static void OnDirectHeal(Player healer, Player patient, int actualAmount)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || actualAmount <= 0
            || healer == null || patient == null || !healer.active || healer.dead
            || !patient.active || patient.dead) return;
        SariaHealingHelper.OnDirectHeal(healer, patient, actualAmount);
        SpotHealingHelper.OnDirectHeal(healer, patient);
    }

    // ASTES1A reports this only after the recipient confirms life actually gained.
    internal static bool NotifyExternalHeal(Player healer, Player patient, int actualAmount, string source)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || source != "ASTES1A:LivingSpark"
            || !ModLoader.TryGetMod("ASTES1A", out _)
            || healer == null || patient == null || !healer.active || healer.dead
            || !patient.active || patient.dead || actualAmount <= 0
            || actualAmount > patient.statLifeMax2) return false;
        OnDirectHeal(healer, patient, actualAmount);
        return true;
    }
}
