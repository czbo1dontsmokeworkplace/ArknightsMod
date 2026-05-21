using ArknightsMod.Content.Items.Material;
using ArknightsMod.Content.Tiles.Infrastructure;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.Exusiai.Armor
{
	[AutoloadEquip(EquipType.Body)]
	internal class ArmorExusiaiBody : ArknightsArmorBody
	{
		public override string Texture => "ArknightsMod/Content/Items/Armor/Sniper/Exusiai/ExusiaiBody";
		public override int Rarity => 6;
		public override int LifeBonus => 84;
		public override void SetArmorDefaults() {
			Item.defense = 12;
		}
		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient(ModContent.ItemType<ExusiaiBody>(), 1)
			.AddIngredient(ModContent.ItemType<Orundum>(), 30)
			.AddTile(ModContent.TileType<FactoryTile>())
			.Register();
		}
	}
}
