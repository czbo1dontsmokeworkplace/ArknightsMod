using System;
using ArknightsMod.Content.Items.Accessories.Developer;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Systems.Gameplay;

public sealed class RecoverySuppressionSystem : ModSystem
{
    public override void Load()
    {
        IL_Player.UpdateLifeRegen += SuppressLifeRecovery;
        On_Player.UpdateManaRegen += SuppressManaRecovery;
    }

    public override void Unload()
    {
        IL_Player.UpdateLifeRegen -= SuppressLifeRecovery;
        On_Player.UpdateManaRegen -= SuppressManaRecovery;
    }

    private static void SuppressLifeRecovery(ILContext il)
    {
        var cursor = new ILCursor(il);
        // This is the positive-recovery loop condition, after all natural, equipment,
        // ModPlayer, palladium and shiny-stone contributions reach the accumulator.
        cursor.GotoNext(MoveType.Before, i => i.MatchLdarg0(),
            i => i.MatchLdfld<Player>(nameof(Player.lifeRegenCount)), i => i.MatchLdcI4(120),
            i => i.OpCode.FlowControl == FlowControl.Cond_Branch);
        cursor.Index += 2;
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate((int count, Player player) => {
            if (!player.GetModPlayer<RecoverySuppressionPlayer>().Equipped) return count;
            player.lifeRegen = Math.Min(0, player.lifeRegen);
            player.lifeRegenCount = Math.Min(0, count);
            player.lifeRegenTime = 0;
            return player.lifeRegenCount;
        });
    }

    private static void SuppressManaRecovery(On_Player.orig_UpdateManaRegen orig, Player player)
    {
        if (!player.GetModPlayer<RecoverySuppressionPlayer>().Equipped)
        {
            orig(player);
            return;
        }
        // Also blocks the separate nebula mana regeneration path.
        player.manaRegen = 0;
        player.manaRegenCount = 0;
        player.nebulaManaCounter = 0;
    }
}
