using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using ArknightsMod.Content.Items.Weapons.Defender.NoirCorne;
using ArknightsMod.Content.Items.Weapons.Defender.Beagle;
using ArknightsMod.Content.Items.Weapons.Defender.Cardigan;
using ArknightsMod.Content.Items.Weapons.Defender.Cuora;
using ArknightsMod.Content.Items.Weapons.Defender.Durnar;
using ArknightsMod.Content.Items.Weapons.Defender.Vulcan;

namespace ArknightsMod.Content.Items.Weapons.Defender;

public sealed class DefenderRecipes : ModSystem
{
    internal const string StarterShield = "ArknightsMod:StarterDefenderShield";
    public override void AddRecipeGroups() => RecipeGroup.RegisterGroup(StarterShield, new RecipeGroup(
        () => Language.GetTextValue("Mods.ArknightsMod.DefenderRecipeGroup"),
        ModContent.ItemType<BeagleWeapon>(), ModContent.ItemType<CardiWeapon>()));
    public override void AddRecipes() => Recipe.Create(ItemID.CobaltShield)
        .AddIngredient<NoirShield>().AddIngredient(ItemID.Bone, 10).AddTile(TileID.Anvils).Register();

    internal static void Register(ModItem weapon)
    {
        Recipe r = weapon.CreateRecipe();
        switch (weapon.Name)
        {
            case "NoirShield": r.AddRecipeGroup(RecipeGroupID.IronBar, 5).AddRecipeGroup(RecipeGroupID.Wood, 10); break;
            case "BeagleWeapon": r.AddIngredient<NoirShield>().AddRecipeGroup(RecipeGroupID.IronBar, 8).AddIngredient(ItemID.Topaz, 2); break;
            case "CardiWeapon": r.AddIngredient<NoirShield>().AddRecipeGroup(RecipeGroupID.IronBar, 5).AddIngredient(ItemID.LifeCrystal); break;
            case "CuoraWeapon": r.AddRecipeGroup(StarterShield).AddIngredient(ItemID.BatBat).AddRecipeGroup(RecipeGroupID.IronBar, 8); break;
            case "DN_Weapon": r.AddRecipeGroup(StarterShield).AddRecipeGroup(OperatorWeaponRecipeGroups.AnyVanillaMeteorPhaseblade).AddIngredient(ItemID.HellstoneBar, 8); break;
            case "FirewhistleLauncher": r.AddRecipeGroup(StarterShield).AddIngredient(ItemID.Grenade, 30).AddIngredient(ItemID.HellstoneBar, 12).AddIngredient(ItemID.IllegalGunParts); break;
            case "Vulcan_Weapon": r.AddIngredient<CuoraWeapon>().AddRecipeGroup(OperatorWeaponRecipeGroups.CobaltOrPalladiumBar, 15).AddIngredient(ItemID.SoulofNight, 8); break;
            case "CroissantHammer": r.AddIngredient<CuoraWeapon>().AddRecipeGroup(OperatorWeaponRecipeGroups.CobaltOrPalladiumBar, 15).AddIngredient(ItemID.SoulofLight, 8); break;
            case "LiskarmPistol": r.AddIngredient<DN_Weapon>().AddIngredient(ItemID.Handgun).AddIngredient(ItemID.SoulofLight, 12).AddIngredient(ItemID.CrystalShard, 15); break;
            case "MudrockHammer": r.AddIngredient<Vulcan_Weapon>().AddIngredient(ItemID.ChlorophyteBar, 18).AddIngredient(ItemID.Ectoplasm, 8); break;
            case "EunectesAxe": r.AddIngredient<CroissantHammer>().AddIngredient<Vulcan_Weapon>().AddIngredient(ItemID.HallowedBar, 18).AddIngredient(ItemID.SoulofMight, 12); break;
            case "NianWeapon": r.AddIngredient<CroissantHammer>().AddIngredient(ItemID.ChlorophyteBar, 18).AddIngredient<global::ArknightsMod.Content.Items.Material.PolymerizationPreparation>(); break;
            default: return;
        }
        r.AddTile(weapon.Name is "MudrockHammer" or "NianWeapon" or "EunectesAxe" ? TileID.MythrilAnvil
            : weapon.Name == "NoirShield" ? TileID.WorkBenches : TileID.Anvils).Register();
    }
}
