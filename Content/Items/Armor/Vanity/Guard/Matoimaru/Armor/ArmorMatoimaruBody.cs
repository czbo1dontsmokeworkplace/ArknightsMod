using Terraria;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Vanity.Guard.Matoimaru.Armor
{
	[AutoloadEquip(EquipType.Body)]
	public class ArmorMatoimaruBody : ArknightsArmorBody
	{
		public override int Rarity => 4;
		public override void SetArmorDefaults() {
			Item.defense = 15;
		}
		public override int LifeBonus => 101;
		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient(ModContent.ItemType<MatoimaruBody>(), 1)
			.AddIngredient(ModContent.ItemType<Orundum>(), 40)
			.AddIngredient(ModContent.ItemType<ManganeseOre>(), 1)
			.AddIngredient(ModContent.ItemType<Device>(), 1)
			.AddTile(ModContent.TileType<FactoryTile>())
			.Register();
		}
	}
}
