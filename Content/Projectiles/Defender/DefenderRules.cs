using ArknightsMod.Content.Items.Weapons.Defender;
using ArknightsMod.Content.Items.Weapons.Defender.Horn;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Defender;

internal static class DefenderRules
{
    private const string Art = "ArknightsMod/Content/Projectiles/Defender/";
    internal static bool IsWeapon(Item item) => item.ModItem is DefenderWeapon or HornGrenadeLauncher;
    internal static bool IsFortress(Item item) => item.ModItem is HornGrenadeLauncher or FirewhistleLauncher;
    internal static bool IsDuelist(Item item) => item.ModItem is EunectesAxe;
    internal static bool SkillActive(Player player) => IsWeapon(player.HeldItem)
        && player.GetModPlayer<WeaponPlayer>() is { SkillActive: true } skills
        && skills.CurrentSkill?.Key.Item == player.HeldItem.ModItem.Name;
    internal static bool Skill(Player player, int index) => SkillActive(player)
        && player.GetModPlayer<WeaponPlayer>().Skill == index;
    internal static float WeaponLength(Item item) => item.ModItem?.Name switch
    {
        "CardiWeapon" => 32.56f,
        "CuoraWeapon" => 50.92f,
        "DN_Weapon" => 39.60f,
        "Vulcan_Weapon" => 36f,
        "CroissantHammer" => 80f,
        "MudrockHammer" or "EunectesAxe" => 112f,
        "NianWeapon" => 118f,
        _ => 60f
    };
    internal static DamageClass AttackClass(Player player) => IsFortress(player.HeldItem) ? DamageClass.Ranged
        : player.HeldItem.ModItem?.Name == "LiskarmPistol" ? DamageClass.Magic
        : player.HeldItem.ModItem?.Name == "DN_Weapon" && SkillActive(player) ? DamageClass.Magic
        : player.HeldItem.ModItem?.Name == "NianWeapon" && Skill(player, 0) ? DamageClass.Magic : DamageClass.Melee;
    internal static string MainArt(Item item) => item.ModItem?.Name switch
    {
        "NoirShield" => null,
        "BeagleWeapon" => Art + "Beagle/MGL_Sword",
        "CardiWeapon" => Art + "Caridgan/Cardi_Sword",
        "CuoraWeapon" => Art + "Cuora/Cuora_Bat",
        "DN_Weapon" => Art + "Durnar/DN_Sword",
        "Vulcan_Weapon" or "MudrockHammer" or "CroissantHammer" => Art + "Vulcan/Vulcan_Hammer",
        "NianWeapon" or "EunectesAxe" => Art + "Nian/NianSword",
        "LiskarmPistol" => "Terraria/Images/Item_" + Terraria.ID.ItemID.Handgun,
        _ => "ArknightsMod/Content/Items/Weapons/Defender/Horn/HornMortar"
    };
    internal static string ShieldArt(Item item) => item.ModItem?.Name switch
    {
        "NoirShield" => Art + "NoirCorne/NoirShield_Projectile",
        "BeagleWeapon" => Art + "Beagle/MGL_Shield",
        "CardiWeapon" => Art + "Caridgan/Cardi_Shield",
        "CuoraWeapon" => Art + "Cuora/Cuora_Shield",
        "DN_Weapon" or "LiskarmPistol" => Art + "Durnar/DN_Shield",
        "Vulcan_Weapon" or "MudrockHammer" or "CroissantHammer" => Art + "Vulcan/Vulcan_Shield",
        "NianWeapon" => Art + "Nian/NianShield",
        "EunectesAxe" => null,
        _ => "ArknightsMod/Content/Items/Weapons/Defender/Horn/HornShield"
    };
    internal static Color Color(Item item) => item.ModItem?.Name switch
    {
        "DN_Weapon" or "LiskarmPistol" => new Color(135, 180, 255),
        "NianWeapon" => new Color(255, 100, 80),
        "Vulcan_Weapon" or "MudrockHammer" => new Color(195, 160, 115),
        "EunectesAxe" => new Color(145, 235, 120),
        _ => new Color(255, 205, 135)
    };
}
