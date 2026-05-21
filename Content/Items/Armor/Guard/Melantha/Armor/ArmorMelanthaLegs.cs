using Terraria;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;
namespace ArknightsMod.Content.Items.Armor.Guard.Melantha.Armor
{
	[AutoloadEquip(EquipType.Legs)]
	public class ArmorMelanthaLegs : ArknightsArmorLegs
	{
		public override string Texture => "ArknightsMod/Content/Items/Armor/Guard/Melantha/MelanthaLegs";
		public override int Rarity => 3;
		public override int LifeBonus => 70;
		public override void SetArmorDefaults() {
			Item.defense = 4;
		}

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient(ModContent.ItemType<MelanthaLegs>(), 1)
			.AddIngredient(ModContent.ItemType<Orundum>(), 30)
			.AddTile(ModContent.TileType<FactoryTile>())
			.Register();
		}
	}
}
