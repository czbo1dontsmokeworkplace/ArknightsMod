using ArknightsMod.Content.Items.Armor.Vanity.Defender.Beagle.Armor;
using ArknightsMod.Content.Items.Armor.Vanity.Sniper.KroosAlter.Armor;
using ArknightsMod.Content.Items.Placeable;
using ArknightsMod.Content.Tiles.Infrastructure;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity.Guard.Matoimaru.Armor
{
	[AutoloadEquip(EquipType.Head)]
	public class ArmorMatoimaruHead : ArknightsArmorHead
	{
		public override int Rarity => 4;
		public override void SetArmorDefaults() {
			Item.defense = 0;
		}
		public override void UpdateArmorSet(Player player) {
			player.setBonus = "";
			player.GetModPlayer<MatoimaruSetPlayer>().MatoimaruSetActive = true;
		}
		public override (float ratio, int value) LifeReplacement => (0.5f, 202);
		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient(ModContent.ItemType<MatoimaruHead>(), 1)
			.AddIngredient(ModContent.ItemType<Orundum>(), 40)
			.AddIngredient(ModContent.ItemType<Material.ManganeseOre>(), 1)
			.AddIngredient(ModContent.ItemType<Material.Grindstone>(), 1)
			.AddTile(ModContent.TileType<FactoryTile>())
			.Register();
		}
		public override bool IsArmorSet(Item head, Item body, Item legs) {
			return body.type == ModContent.ItemType<ArmorMatoimaruBody>() && legs.type == ModContent.ItemType<ArmorMatoimaruLegs>();
		}
		
	}
}
