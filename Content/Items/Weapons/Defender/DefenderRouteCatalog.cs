using System.Linq;
using Terraria;
namespace ArknightsMod.Content.Items.Weapons.Defender;

// One authoritative DAG for recipes, progression and profession membership.
internal sealed record DefenderRouteNode(string Name, string Operator, string Branch,
    int Tier, string[] Parents, int Damage, int Interval);
internal static class DefenderRouteCatalog
{
    internal static readonly DefenderRouteNode[] Nodes = [
        new("NoirShield", "黑角", "Protector", 0, [], 10, 32),
        new("BeagleWeapon", "米格鲁", "Protector", 1, ["NoirShield"], 16, 30),
        new("CardiWeapon", "卡缇", "Protector", 1, ["NoirShield"], 16, 30),
        new("CuoraWeapon", "蛇屠箱", "Protector", 2, ["BeagleWeapon", "CardiWeapon"], 28, 30),
        new("CroissantHammer", "可颂", "Protector", 3, ["CuoraWeapon"], 80, 36),
        new("BisonShield", "拜松", "Protector", 3, ["CuoraWeapon"], 74, 30),
        new("HeavyrainBlade", "暴雨", "Protector", 3, ["CuoraWeapon"], 70, 26),
        new("HoshigumaShield", "星熊", "Protector", 4, ["CroissantHammer", "BisonShield", "HeavyrainBlade"], 148, 32),
        new("NianWeapon", "年", "Protector", 4, ["CroissantHammer", "BisonShield", "HeavyrainBlade"], 124, 45),
        new("LutonHammer", "露托", "Juggernaut", 2, ["BeagleWeapon", "CardiWeapon"], 32, 38),
        new("Vulcan_Weapon", "火神", "Juggernaut", 3, ["LutonHammer"], 65, 35),
        new("MudrockHammer", "泥岩", "Juggernaut", 4, ["Vulcan_Weapon"], 170, 42),
        new("LiskarmPistol", "雷蛇", "SentinelProtector", 3, ["CuoraWeapon"], 78, 24),
        new("BlitzPistol", "闪击", "SentinelProtector", 3, ["CuoraWeapon"], 86, 28),
        new("JessicaLiberatedGun", "涤火杰西卡", "SentinelProtector", 4, ["LiskarmPistol", "BlitzPistol"], 162, 20),
        new("MechanistGun", "机械师", "SentinelProtector", 4, ["LiskarmPistol", "BlitzPistol"], 180, 28),
        new("FaithMixer", "信仰搅拌机", "SentinelProtector", 4, ["LiskarmPistol", "BlitzPistol"], 174, 24),
        new("DN_Weapon", "坚雷", "ArtsProtector", 2, ["BeagleWeapon", "CardiWeapon"], 23, 25),
        new("AsbestosBlade", "石棉", "ArtsProtector", 3, ["DN_Weapon"], 82, 34),
        new("ShalemBlade", "暮落", "ArtsProtector", 3, ["DN_Weapon"], 90, 36),
        new("CzernyBlade", "车尼尔", "ArtsProtector", 3, ["DN_Weapon"], 76, 28),
        new("HoshigumaBlazingBlade", "斩业星熊", "ArtsProtector", 4, ["AsbestosBlade", "ShalemBlade", "CzernyBlade"], 190, 34),
        new("FirewhistleLauncher", "火哨", "Fortress", 2, ["BeagleWeapon", "CardiWeapon"], 58, 48),
        new("AshlockLauncher", "灰毫", "Fortress", 2, ["BeagleWeapon", "CardiWeapon"], 64, 54),
        new("HornGrenadeLauncher", "号角", "Fortress", 4, ["FirewhistleLauncher", "AshlockLauncher"], 388, 42),
        new("BubbleMaul", "泡泡", "Duelist", 2, ["BeagleWeapon", "CardiWeapon"], 34, 38),
        new("AuroraAxe", "极光", "Duelist", 3, ["BubbleMaul"], 106, 40),
        new("CementMaul", "洋灰", "Duelist", 3, ["BubbleMaul"], 116, 46),
        new("EunectesAxe", "森蚺", "Duelist", 4, ["AuroraAxe", "CementMaul"], 150, 38),
        new("MatterhornBlade", "角峰", "PrimalProtector", 2, ["BeagleWeapon", "CardiWeapon"], 30, 34),
        new("PhilaeBlade", "菲莱", "PrimalProtector", 3, ["MatterhornBlade"], 78, 30),
        new("BasslineBlade", "响石", "PrimalProtector", 3, ["MatterhornBlade"], 84, 34),
        new("UnderflowBlade", "裂响", "PrimalProtector", 3, ["MatterhornBlade"], 92, 36),
        new("YuBlade", "余", "PrimalProtector", 4, ["PhilaeBlade", "BasslineBlade", "UnderflowBlade"], 172, 30),
        new("SanbyBlade", "珊比", "PrimalProtector", 4, ["PhilaeBlade", "BasslineBlade", "UnderflowBlade"], 164, 26),
    ];
    internal static DefenderRouteNode Find(Item item) => item.ModItem?.Mod.Name == "ArknightsMod"
        ? Nodes.FirstOrDefault(n => n.Name == item.ModItem.Name) : null;
    internal static DefenderRouteNode Find(string name) => Nodes.FirstOrDefault(n => n.Name == name);
}
