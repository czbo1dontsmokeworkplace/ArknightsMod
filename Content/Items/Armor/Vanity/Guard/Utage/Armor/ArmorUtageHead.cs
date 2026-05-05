
using ArknightsMod.Content.Items.Armor.Vanity.Guard.Melantha.Armor;
using ArknightsMod.Content.Tiles.Infrastructure;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity.Guard.Utage.Armor
{
	[AutoloadEquip(EquipType.Head)]
	internal class ArmorUtageHead:ArknightsArmorHead
	{
		public override int Rarity => 4;
		public override int LifeBonus => 197;
		public override void SetArmorDefaults() {
			Item.defense = 0;
		}
		public override bool IsArmorSet(Item head, Item body, Item legs) {
			return body.type == ModContent.ItemType<ArmorUtageBody>() &&
				legs.type == ModContent.ItemType<ArmorUtageLegs>();
		}
		public override void UpdateArmorSet(Player player) {
			player.setBonus = "";
			player.GetModPlayer<UtageSetPlayer>().UtageSetActive = true;
		}
		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient(ModContent.ItemType<UtageHead>(), 1)
			.AddIngredient(ModContent.ItemType<Orundum>(), 30)
			.AddTile(ModContent.TileType<FactoryTile>())
			.Register();
		}
	}
}
