using System;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Defender;

public sealed class DefenderRecipes : ModSystem
{
    internal const string StarterShield = "ArknightsMod:StarterDefenderShield";
    private static string Group(DefenderRouteNode node) => node.Parents.SequenceEqual(new[]{"BeagleWeapon", "CardiWeapon"})
        ? StarterShield : "ArknightsMod:DefenderParents_" + node.Parents[0];
    public override void AddRecipeGroups()
    {
        foreach (var node in DefenderRouteCatalog.Nodes.Where(n => n.Parents.Length > 1)
            .GroupBy(Group).Select(g => g.First()))
        {
            var members = node.Parents.Select(n => Mod.Find<ModItem>(n).Type).ToArray();
            string key = "Mods.ArknightsMod.DefenderRecipeGroups." + node.Parents[0];
            RecipeGroup.RegisterGroup(Group(node), new RecipeGroup(() => Language.GetTextValue(key), members));
        }
    }
    public override void AddRecipes() => Recipe.Create(ItemID.CobaltShield)
        .AddIngredient(Mod.Find<ModItem>("NoirShield").Type).AddIngredient(ItemID.Bone, 10).AddTile(TileID.Anvils).Register();

    internal static void Register(ModItem weapon)
    {
        var node = DefenderRouteCatalog.Find(weapon.Name);
        if (node == null) return;
        Recipe r = weapon.CreateRecipe();
        if (node.Parents.Length == 1) r.AddIngredient(weapon.Mod.Find<ModItem>(node.Parents[0]).Type);
        else if (node.Parents.Length > 1) r.AddRecipeGroup(Group(node));
        int tile = node.Tier >= 3 ? TileID.MythrilAnvil : node.Tier == 0 ? TileID.WorkBenches : TileID.Anvils;
        if (node.Tier == 0) r.AddRecipeGroup(RecipeGroupID.IronBar, 5).AddRecipeGroup(RecipeGroupID.Wood, 10);
        else if (node.Tier == 1) {
            r.AddRecipeGroup(RecipeGroupID.IronBar, 8);
            if (node.Name == "CardiWeapon") r.AddIngredient(ItemID.LifeCrystal);
            else r.AddIngredient(ItemID.Topaz, 2);
        }
        else if (node.Tier == 2) {
            switch (node.Name) {
                case "CuoraWeapon": r.AddIngredient(ItemID.BatBat).AddRecipeGroup(RecipeGroupID.IronBar, 8); break;
                case "DN_Weapon": r.AddRecipeGroup(OperatorWeaponRecipeGroups.AnyVanillaMeteorPhaseblade).AddIngredient(ItemID.HellstoneBar, 8); break;
                case "FirewhistleLauncher":
                    r.AddIngredient(ItemID.Grenade, 30).AddIngredient(ItemID.HellstoneBar, 12).AddIngredient(ItemID.IllegalGunParts); break;
                case "LutonHammer": r.AddIngredient(ItemID.HellstoneBar, 10).AddIngredient(ItemID.Bone, 20); break;
                case "BubbleMaul": r.AddIngredient(ItemID.HellstoneBar, 10).AddIngredient(ItemID.Obsidian, 20); break;
                case "MatterhornBlade": r.AddIngredient(ItemID.HellstoneBar, 8).AddIngredient(ItemID.Amethyst, 10); break;
            }
        }
        else if (node.Tier == 3) {
            r.AddRecipeGroup(OperatorWeaponRecipeGroups.CobaltOrPalladiumBar, 15);
            r.AddIngredient(node.Branch is "Juggernaut" or "PrimalProtector" ? ItemID.SoulofNight : ItemID.SoulofLight, 8);
            if (node.Branch == "SentinelProtector") r.AddIngredient(ItemID.Handgun).AddIngredient(ItemID.CrystalShard, 12);
            else r.AddIngredient(node.Name == "AuroraAxe" ? ItemID.IceBlock : ItemID.CrystalShard, node.Name == "AuroraAxe" ? 50 : 10);
        }
        else {
            r.AddIngredient(ItemID.ChlorophyteBar, 18).AddIngredient(ItemID.Ectoplasm, 8);
            if (node.Name == "HornGrenadeLauncher") {
                r.AddIngredient<global::ArknightsMod.Content.Items.Material.D32Steel>()
                    .AddIngredient<global::ArknightsMod.Content.Items.Material.OrironBlock>(2)
                    .AddIngredient(ItemID.ObsidianShield).AddIngredient(ItemID.GrenadeLauncher)
                    .AddIngredient(ItemID.IllegalGunParts);
                tile = ModContent.TileType<global::ArknightsMod.Content.Tiles.Infrastructure.FactoryTile>();
            }
            else if (node.Name == "NianWeapon") r.AddIngredient<global::ArknightsMod.Content.Items.Material.PolymerizationPreparation>();
            else if (node.Name == "HoshigumaBlazingBlade") r.AddIngredient(ItemID.BeetleHusk, 8);
            else if (node.Branch == "SentinelProtector") r.AddIngredient(ItemID.SoulofSight, 8);
            else if (node.Branch == "PrimalProtector") r.AddIngredient(ItemID.SoulofFright, 8);
            else if (node.Branch == "Duelist") r.AddIngredient(ItemID.SoulofMight, 8);
        }
        r.AddTile(tile).Register();
    }
}
