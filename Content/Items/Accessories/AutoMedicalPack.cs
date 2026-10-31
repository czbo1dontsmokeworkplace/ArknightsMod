using ArknightsMod.Content.Items.Weapons.Medic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Accessories;

public sealed class AutoMedicalPack : ModItem
{
    public override string Texture=>"Terraria/Images/Item_"+ItemID.PadThai;
    // Vanilla food art contains inventory/world/held frames. Use its inventory
    // frame only; this accessory must not draw the full three-frame sheet.
    public override void SetStaticDefaults()=>Main.RegisterItemAnimation(Type,new DrawAnimationVertical(int.MaxValue,3));
    public override void SetDefaults()
    {
        Item.width=30;Item.height=24;
        Item.accessory=true;
        Item.rare=ItemRarityID.Blue;
        Item.value=Item.sellPrice(silver:20);
    }
    public override void UpdateAccessory(Player player,bool hideVisual)
        =>player.GetModPlayer<AutoMedicalPlayer>().Equipped=true;
    public override void AddRecipes()=>CreateRecipe()
        .AddIngredient(ItemID.RichMahogany,12)
        .AddIngredient(ItemID.JungleSpores,4)
        .AddIngredient(ItemID.Vine)
        .AddTile(TileID.WorkBenches).Register();
}
