using Terraria;

namespace ArknightsMod.Content.Projectiles.Defender;

internal static class ShieldGuardRules
{
    internal static bool Supports(Item item) => DefenderRules.IsWeapon(item) && !DefenderRules.IsDuelist(item);
    internal static bool WantsRaised(Player player) => Supports(player.HeldItem) && player.active && !player.dead
        && !player.CCed && !player.noItems && ((player.whoAmI == Main.myPlayer && Main.mouseRight && !player.mouseInterface)
            || DefenderSkillRules.StopsAttacking(player));
    internal static bool IsRaised(Player player)
    {
        if (!Supports(player.HeldItem) || !player.active || player.dead || player.CCed || player.noItems) return false;
        var held = DefenderHoldout.Find(player);
        if (held?.Attacking == true) return false;
        return player.whoAmI == Main.myPlayer ? WantsRaised(player) : held?.Guarding == true;
    }
    internal static bool IsFront(Player player, float sourceX) => System.MathF.Sign(sourceX - player.Center.X) == player.direction;
}