using ArknightsMod.Content.Items.Material;
using ArknightsMod.Content.Items.Material.T2;
using ArknightsMod.Content.Items.Material.T3;
using ArknightsMod.Content.Tiles.Infrastructure;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.Matoimaru.Armor
{
	[AutoloadEquip(EquipType.Legs)]
	public class ArmorMatoimaruLegs : ArknightsArmorLegs
	{
		public override string Texture => "ArknightsMod/Content/Items/Armor/Guard/Matoimaru/MatoimaruLegs";
		public override int Rarity => 4;
		public override void SetArmorDefaults() {
			Item.defense = 5;
		}
		public override int LifeBonus => 101;
		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient(ModContent.ItemType<MatoimaruLegs>(), 1)
			.AddIngredient(ModContent.ItemType<Orundum>(), 40)
			.AddIngredient(ModContent.ItemType<OrirockCube>(), 1)
			.AddIngredient(ModContent.ItemType<Grindstone>(), 1)
			.AddTile(ModContent.TileType<FactoryTile>())
			.Register();
		}
	}
}
