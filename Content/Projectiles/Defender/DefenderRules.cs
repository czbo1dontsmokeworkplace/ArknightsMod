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
    internal static bool IsDuelist(Item item) => DefenderRouteCatalog.Find(item)?.Branch == "Duelist";
    internal static bool IsSentinel(Item item) => DefenderRouteCatalog.Find(item)?.Branch == "SentinelProtector";
    internal static bool IsPrimal(Item item) => DefenderRouteCatalog.Find(item)?.Branch == "PrimalProtector";
    internal static bool SkillActive(Player player) => IsWeapon(player.HeldItem)
        && (player.whoAmI == Main.myPlayer
            ? player.GetModPlayer<WeaponPlayer>() is { SkillActive: true } skills
                && skills.CurrentSkill?.Key.Item == player.HeldItem.ModItem.Name
            : player.GetModPlayer<DefenderCombatPlayer>() is var remote && remote.RemoteSkill >= 0
                && remote.RemoteItem == player.HeldItem.type && Main.GameUpdateCount <= remote.RemoteUntil);
    internal static int SkillIndex(Player player) => player.whoAmI == Main.myPlayer
        ? player.GetModPlayer<WeaponPlayer>().Skill : player.GetModPlayer<DefenderCombatPlayer>().RemoteSkill;
    internal static int SkillTime(Player player) => player.whoAmI == Main.myPlayer
        ? player.GetModPlayer<WeaponPlayer>().SkillTimer : player.GetModPlayer<DefenderCombatPlayer>().RemoteTime;
    internal static bool Skill(Player player, int index) => SkillActive(player)
        && SkillIndex(player) == index;
    internal static float WeaponLength(Item item) => item.ModItem?.Name switch
    {
        "CardiWeapon" => 32.56f,
        "CuoraWeapon" => 50.92f,
        "DN_Weapon" => 39.60f,
        "Vulcan_Weapon" => 36f,
        "CroissantHammer" => 80f,
        "MudrockHammer" or "EunectesAxe" => 112f,
        "NianWeapon" => 118f,
        _ => item.ModItem is RoutedDefenderWeapon ? 65f : 60f
    };
    internal static DamageClass AttackClass(Player player) => IsFortress(player.HeldItem) ? DamageClass.Ranged
        : IsSentinel(player.HeldItem) ? DamageClass.Magic
        : DefenderRouteCatalog.Find(player.HeldItem)?.Branch == "ArtsProtector" && SkillActive(player) ? DamageClass.Magic
        : player.HeldItem.ModItem?.Name == "YuBlade" && Skill(player, 1) ? DamageClass.Magic
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
        _ => item.ModItem is RoutedDefenderWeapon ? DefenderRouteCatalog.Find(item)?.Branch switch {
            "SentinelProtector" => "Terraria/Images/Item_" + Terraria.ID.ItemID.Handgun,
            "Juggernaut" or "Duelist" => Art + "Vulcan/Vulcan_Hammer",
            _ => Art + "Durnar/DN_Sword"
        } : "ArknightsMod/Content/Items/Weapons/Defender/Horn/HornMortar"
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
        _ => IsDuelist(item) ? null : item.ModItem is RoutedDefenderWeapon ? Art + "Durnar/DN_Shield"
            : "ArknightsMod/Content/Items/Weapons/Defender/Horn/HornShield"
    };
    internal static Color Color(Item item) => item.ModItem?.Name switch
    {
        "BisonShield" => new Color(112, 158, 191),
        "HeavyrainBlade" => new Color(166, 197, 172),
        "HoshigumaShield" => new Color(120, 182, 155),
        "LutonHammer" => new Color(119, 187, 221),
        "BlitzPistol" => new Color(255, 227, 160),
        "JessicaLiberatedGun" => new Color(222, 176, 118),
        "MechanistGun" => new Color(128, 207, 206),
        "FaithMixer" => new Color(240, 211, 145),
        "AsbestosBlade" => new Color(142, 214, 189),
        "ShalemBlade" => new Color(185, 90, 145),
        "CzernyBlade" => new Color(226, 207, 156),
        "HoshigumaBlazingBlade" => new Color(244, 126, 89),
        "AshlockLauncher" => new Color(213, 186, 149),
        "BubbleMaul" => new Color(202, 178, 135),
        "AuroraAxe" => new Color(164, 222, 237),
        "CementMaul" => new Color(201, 180, 155),
        "MatterhornBlade" => new Color(182, 193, 161),
        "PhilaeBlade" => new Color(172, 122, 205),
        "BasslineBlade" => new Color(228, 193, 110),
        "UnderflowBlade" => new Color(182, 156, 195),
        "YuBlade" => new Color(240, 155, 95),
        "SanbyBlade" => new Color(164, 215, 154),
        "DN_Weapon" or "LiskarmPistol" => new Color(135, 180, 255),
        "NianWeapon" => new Color(255, 100, 80),
        "Vulcan_Weapon" or "MudrockHammer" => new Color(195, 160, 115),
        "EunectesAxe" => new Color(145, 235, 120),
        _ => DefenderRouteCatalog.Find(item)?.Branch switch {
            "PrimalProtector" => new Color(180, 130, 220),
            "ArtsProtector" => new Color(170, 130, 240),
            "SentinelProtector" => new Color(135, 200, 255),
            "Duelist" => new Color(150, 220, 185),
            _ => new Color(255, 205, 135)
        }
    };
}
