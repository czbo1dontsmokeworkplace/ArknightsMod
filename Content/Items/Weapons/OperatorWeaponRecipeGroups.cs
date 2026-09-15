using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons;

public sealed class OperatorWeaponRecipeGroups : ModSystem
{
    public const string AnyVanillaPiano = "ArknightsMod:AnyVanillaPiano";
    public const string CopperOrTinBar = "ArknightsMod:CopperOrTinBar";
    public const string IronOrLeadBar = "ArknightsMod:IronOrLeadBar";
    public const string GoldOrPlatinumBar = "ArknightsMod:GoldOrPlatinumBar";
    public const string AnyVanillaWoodenBow = "ArknightsMod:AnyVanillaWoodenBow";
    public const string AnyVanillaTombstone = "ArknightsMod:AnyVanillaTombstone";
    public const string CobaltOrPalladiumChainsaw = "ArknightsMod:CobaltOrPalladiumChainsaw";
    public const string CobaltOrPalladiumBar = "ArknightsMod:CobaltOrPalladiumBar";
    public const string AnyVanillaMeteorPhaseblade = "ArknightsMod:AnyVanillaMeteorPhaseblade";

    public override void AddRecipeGroups()
    {
        // 按实际放置的家具类型收集原版钢琴，所有家具套装共用一个材料槽和一道配方。
        int[] pianos = ContentSamples.ItemsByType
            .Where(entry => entry.Key > 0 && entry.Key < ItemID.Count && entry.Value.createTile == TileID.Pianos)
            .Select(entry => entry.Key).OrderBy(type => type).Prepend(ItemID.Piano).Distinct().ToArray();
        RecipeGroup.RegisterGroup(AnyVanillaPiano, new RecipeGroup(
            () => Language.GetTextValue("LegacyMisc.37") + " " + Lang.GetItemNameValue(ItemID.Piano), pianos));

        Register(CopperOrTinBar, Lang.GetItemNameValue(ItemID.CopperBar), ItemID.CopperBar, ItemID.TinBar);
        Register(IronOrLeadBar, Lang.GetItemNameValue(ItemID.IronBar), ItemID.IronBar, ItemID.LeadBar);
        Register(GoldOrPlatinumBar, Lang.GetItemNameValue(ItemID.GoldBar), ItemID.GoldBar, ItemID.PlatinumBar);
        Register(CobaltOrPalladiumChainsaw, Lang.GetItemNameValue(ItemID.CobaltChainsaw), ItemID.CobaltChainsaw, ItemID.PalladiumChainsaw);
        Register(CobaltOrPalladiumBar, Lang.GetItemNameValue(ItemID.CobaltBar), ItemID.CobaltBar, ItemID.PalladiumBar);
        Register(AnyVanillaMeteorPhaseblade, Lang.GetItemNameValue(ItemID.BluePhaseblade), ItemID.BluePhaseblade, ItemID.RedPhaseblade, ItemID.GreenPhaseblade, ItemID.PurplePhaseblade, ItemID.WhitePhaseblade, ItemID.YellowPhaseblade, ItemID.OrangePhaseblade);

        int[] bows = [ItemID.WoodenBow, ItemID.BorealWoodBow, ItemID.PalmWoodBow, ItemID.RichMahoganyBow, ItemID.EbonwoodBow, ItemID.ShadewoodBow];
        Register(AnyVanillaWoodenBow, Lang.GetItemNameValue(ItemID.WoodenBow), bows);
        int[] tombstones = ContentSamples.ItemsByType
            .Where(entry => entry.Key > 0 && entry.Key < ItemID.Count && entry.Value.createTile == TileID.Tombstones)
            .Select(entry => entry.Key).OrderBy(type => type).Prepend(ItemID.Tombstone).Distinct().ToArray();
        Register(AnyVanillaTombstone, Lang.GetItemNameValue(ItemID.Tombstone), tombstones);
    }

    public override void AddRecipes()
    {
        Recipe.Create(ItemID.Spear)
            .AddRecipeGroup(RecipeGroupID.IronBar, 3)
            .AddIngredient(ItemID.Chain, 5)
            .AddRecipeGroup(RecipeGroupID.Wood, 15)
            .AddTile(TileID.WorkBenches)
            .Register();

        Recipe.Create(ItemID.SawtoothShark)
            .AddIngredient(ItemID.Bass, 5)
            .AddIngredient(ItemID.SharkFin)
            .AddIngredient(ItemID.SandBlock, 20)
            .AddIngredient(ItemID.Coral, 5)
            .AddTile(TileID.WorkBenches)
            .Register();

        Recipe.Create(ItemID.CursedFlame).AddIngredient(ItemID.Ichor).Register();
        Recipe.Create(ItemID.Ichor).AddIngredient(ItemID.CursedFlame).Register();
    }

    private static void Register(string name, string label, params int[] items) =>
        RecipeGroup.RegisterGroup(name, new RecipeGroup(() => label, items));
}
