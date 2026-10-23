using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Accessories.Developer;

public sealed class RecoverySuppressorTest : ModItem
{
    public override string Texture => $"Terraria/Images/Item_{ItemID.BandofRegeneration}";

    public override void SetDefaults()
    {
        Item.width = Item.height = 28;
        Item.accessory = true;
        Item.rare = ItemRarityID.Purple;
        Item.value = 0;
    }

    public override void UpdateAccessory(Player player, bool hideVisual) =>
        player.GetModPlayer<RecoverySuppressionPlayer>().Equipped = true;

    public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.DirtBlock).Register();
}

public sealed class RecoverySuppressionPlayer : ModPlayer
{
    public bool Equipped;
    public override void ResetEffects() => Equipped = false;
}
