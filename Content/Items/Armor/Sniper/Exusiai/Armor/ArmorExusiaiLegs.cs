using ArknightsMod.Content.Items.Material;
using ArknightsMod.Content.Tiles.Infrastructure;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.Exusiai.Armor
{
	[AutoloadEquip(EquipType.Legs)]
	internal class ArmorExusiaiLegs : ArknightsArmorLegs
	{
		public override string Texture => "ArknightsMod/Content/Items/Armor/Sniper/Exusiai/ExusiaiLegs";
		public override int Rarity => 6;
		public override int LifeBonus => 84;
		public override void SetArmorDefaults() {
			Item.defense = 4;
		}

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient(ModContent.ItemType<ExusiaiLegs>(), 1)
			.AddIngredient(ModContent.ItemType<Orundum>(), 30)
			.AddTile(ModContent.TileType<FactoryTile>())
			.Register();
		}
	}

}
