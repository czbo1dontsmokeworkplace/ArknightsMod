using ArknightsMod.Players;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Systems.Gameplay;

// Run after natural regeneration, equipment and every ModPlayer regeneration hook,
// immediately before the final value enters Terraria's regeneration accumulator.
public sealed class CrusherTraitSystem : ModSystem
{
    public override void Load() => IL_Player.UpdateLifeRegen += BoostPositiveRegeneration;
    public override void Unload() => IL_Player.UpdateLifeRegen -= BoostPositiveRegeneration;
    private static void BoostPositiveRegeneration(ILContext il)
    {
        var cursor = new ILCursor(il);
        cursor.GotoNext(MoveType.After, i => i.MatchLdfld<Player>(nameof(Player.lifeRegenCount)),
            i => i.MatchLdarg0(), i => i.MatchLdfld<Player>(nameof(Player.lifeRegen)));
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate((int regen, Player player) => {
            // Palladium and Shiny Stone add directly to the accumulator after this IL site.
            // Account for their positive contributions once, leaving damage-over-time intact.
            int extra = player.palladiumRegen ? 4 : 0;
            if (player.shinyStone && player.IsStandingStillForSpecialEffects && player.itemAnimation == 0 && regen > 0 && player.statLife < player.statLifeMax2) extra++;
            return player.GetModPlayer<CrusherPlayer>().ApplyRegenerationBonus(regen, extra);
        });
    }
}
