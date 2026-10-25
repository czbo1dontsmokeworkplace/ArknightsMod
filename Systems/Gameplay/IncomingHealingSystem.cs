using ArknightsMod.Players;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Systems.Gameplay;

public sealed class IncomingHealingSystem : ModSystem
{
    public override void Load()
    {
        On_Player.Heal += Heal;
        On_Player.HealEffect += HealEffect;
        IL_Player.UpdateLifeRegen += Regeneration;
        IL_Projectile.VanillaAI += LifeSteal;
        IL_Player.ApplyLifeAndOrMana += RandomPotion;
        IL_Player.ApplyLifeAndOrMana += BlockLifeIncrease;
        IL_Player.UseHealthMaxIncreasingItem += BlockLifeIncrease;
        IL_Projectile.VanillaAI += BlockLifeIncrease;
    }
    public override void Unload()
    {
        On_Player.Heal -= Heal;
        On_Player.HealEffect -= HealEffect;
        IL_Player.UpdateLifeRegen -= Regeneration;
        IL_Projectile.VanillaAI -= LifeSteal;
        IL_Player.ApplyLifeAndOrMana -= RandomPotion;
        IL_Player.ApplyLifeAndOrMana -= BlockLifeIncrease;
        IL_Player.UseHealthMaxIncreasingItem -= BlockLifeIncrease;
        IL_Projectile.VanillaAI -= BlockLifeIncrease;
    }
    private static void Heal(On_Player.orig_Heal orig, Player player, int amount)
    {
        int finalAmount = player.GetModPlayer<IncomingHealingPlayer>().Modify(amount);
        if (amount > 0 && finalAmount == 0) return;
        orig(player, finalAmount);
    }

    private static void HealEffect(On_Player.orig_HealEffect orig, Player player, int amount, bool broadcast)
    {
        if (amount > 0 && player.GetModPlayer<IncomingHealingPlayer>().HealingBlocked) return;
        orig(player, amount, broadcast);
    }

    private static void BlockLifeIncrease(ILContext il)
    {
        var cursor = new ILCursor(il);
        var targetLife = new VariableDefinition(il.Method.Module.TypeSystem.Int32);
        il.Body.Variables.Add(targetLife);
        // Guard the final HP write, including every vanilla healing projectile path.
        // Keep max-HP gains, mana restoration and damage unchanged.
        while (cursor.TryGotoNext(MoveType.Before, i => i.MatchStfld<Player>(nameof(Player.statLife))))
        {
            cursor.Emit(OpCodes.Stloc, targetLife);
            cursor.Emit(OpCodes.Dup);
            cursor.Emit(OpCodes.Ldloc, targetLife);
            cursor.EmitDelegate((Player player, int proposedLife) =>
                player.GetModPlayer<IncomingHealingPlayer>().HealingBlocked
                    ? System.Math.Min(player.statLife, proposedLife) : proposedLife);
            cursor.Index++;
        }
    }

    private static void Regeneration(ILContext il)
    {
        var cursor = new ILCursor(il);
        // Scale only the positive HP increment, after all regeneration and suppression
        // hooks. Negative regeneration and life synchronization must never be amplified.
        cursor.GotoNext(MoveType.After, i => i.MatchLdfld<Player>(nameof(Player.statLife)),
            i => i.MatchLdcI4(1));
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate((int amount, Player player) => player.GetModPlayer<IncomingHealingPlayer>().Modify(amount));
    }

    private static void LifeSteal(ILContext il)
    {
        var cursor = new ILCursor(il);
        cursor.GotoNext(i => i.MatchCall<Player>(nameof(Player.HealEffect)) || i.MatchCallvirt<Player>(nameof(Player.HealEffect)));
        // Vanilla healing projectiles (vampire knives / spectre hood) bypass Heal.
        // Scale their local amount before both the popup and the HP/network write.
        cursor.GotoPrev(MoveType.Before, i => i.OpCode.Code is Code.Stloc or Code.Stloc_S);
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate((int amount, Projectile projectile) =>
            Main.player[(int)projectile.ai[0]].GetModPlayer<IncomingHealingPlayer>().Modify(amount));
    }

    private static void RandomPotion(ILContext il)
    {
        // Strange Brew replaces GetHealLife's value with a random roll.
        var cursor = new ILCursor(il);
        cursor.GotoNext(MoveType.After, i => i.MatchCallvirt<Terraria.Utilities.UnifiedRandom>("Next"));
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate((int amount, Player player) => player.GetModPlayer<IncomingHealingPlayer>().Modify(amount));
    }
}
