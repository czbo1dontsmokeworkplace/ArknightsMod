
using ArknightsMod.Content.Items.Armor.Vanity.Guard.Melantha;
using ArknightsMod.Content.Tiles.Infrastructure;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity.Guard.Utage.Armor
{
	[AutoloadEquip(EquipType.Body)]
	internal class ArmorUtageBody : ArknightsArmorBody
	{
		public override int Rarity => 4;
		public override int LifeBonus => 98;
		public override void SetArmorDefaults() {
			Item.defense = 16;
		}
		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient(ModContent.ItemType<UtageBody>(), 1)
			.AddIngredient(ModContent.ItemType<Orundum>(), 30)
			.AddTile(ModContent.TileType<FactoryTile>())
			.Register();
		}
	}
}
