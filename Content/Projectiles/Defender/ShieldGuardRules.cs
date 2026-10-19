using System;
using Terraria;
using Terraria.ModLoader;
using ArknightsMod.Content.Projectiles.Defender.Horn;

namespace ArknightsMod.Content.Projectiles.Defender;

internal static class ShieldGuardRules
{
    internal static bool Supports(Item item) => item.ModItem is IShieldGuardWeapon
        or Items.Weapons.Defender.Cuora.CuoraWeapon
        or Items.Weapons.Defender.Cardigan.CardiWeapon
        or Items.Weapons.Defender.Durnar.DN_Weapon
        or Items.Weapons.Defender.Vulcan.Vulcan_Weapon;

    internal static bool IsRaised(Player player)
    {
        if (!Supports(player.HeldItem) || !player.active || player.dead || player.CCed || player.noItems)
            return false;
        // Horn synchronizes the held right-button state through its persistent visual projectile.
        // The parry cooldown only limits successful blocks, not the held guarding stance.
        if (player.HeldItem.ModItem is Items.Weapons.Defender.Horn.HornGrenadeLauncher
            && player.whoAmI != Main.myPlayer)
        {
            int type = ModContent.ProjectileType<HornGuard>();
            foreach (Projectile projectile in Main.ActiveProjectiles)
                if (projectile.owner == player.whoAmI && projectile.type == type)
                    return projectile.ai[1] == 1f;
            return false;
        }
        return player.whoAmI == Main.myPlayer && Main.mouseRight && !player.mouseInterface;
    }

    internal static bool IsFront(Player player, float sourceX) => MathF.Sign(sourceX - player.Center.X) == player.direction;
}

/// <summary>Held shields can extend a successful frontal block without duplicating mitigation.</summary>
public interface IShieldGuardWeapon
{
    void OnGuardSuccess(Player player);
}
