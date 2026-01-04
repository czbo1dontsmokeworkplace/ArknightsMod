using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Placeable;

namespace ArknightsMod.Content.Items.Armor.Vanity.Guard.Matoimaru.Armor
{
	[AutoloadEquip(EquipType.Legs)]
	public class ArmorMatoimaruLegs : ArknightsArmorLegs
	{
		public override int Rarity => 4;
		public override void SetArmorDefaults() {
			Item.defense = 5;
		}
		public override int LifeBonus => 101;
		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient(ModContent.ItemType<MatoimaruLegs>(), 1)
			.AddIngredient(ModContent.ItemType<Orundum>(), 40)
			.AddIngredient(ModContent.ItemType<Material.OrirockCube>(), 1)
			.AddIngredient(ModContent.ItemType<Material.Grindstone>(), 1)
			.AddTile(ModContent.TileType<FactoryTile>())
			.Register();
		}
	}
}
