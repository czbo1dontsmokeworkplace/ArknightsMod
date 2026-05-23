using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;
using ArknightsMod.Content.Items.Material.T1;
using ArknightsMod.Content.Items.Material.T2;
using ArknightsMod.Content.Items.Material.T3;
using ArknightsMod.Content.Items.Material.T4;
using ArknightsMod.Content.Items.Material.T5;

namespace ArknightsMod.Content.Items.Armor.Guard.Skadi.Armor
{
	[AutoloadEquip(EquipType.Head)]
	public class ArmorSkadiHead : ArknightsArmorHead
	{
		public override string Texture => "ArknightsMod/Content/Items/Armor/Guard/Skadi/SkadiHead";
		public override int Rarity => 6;
		public override void SetArmorDefaults() {
			Item.defense = 0;
		}
		public override int LifeBonus => 386;
		public override void Load() {
			if (Main.netMode == NetmodeID.Server)
				return;
		}
		public override bool IsArmorSet(Item head, Item body, Item legs) {
			return body.type == ModContent.ItemType<ArmorSkadiBody>() &&
				legs.type == ModContent.ItemType<ArmorSkadiLegs>();
		}
		public override void UpdateArmorSet(Player player) {
			player.setBonus = "";
		}

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<SkadiHead>(1)
			.AddIngredient<Orundum>(60)
			.AddIngredient<D32Steel>(4)
			.AddIngredient<OrirockConcentration>(9)
			.AddTile(ModContent.TileType<FactoryTile>())
			.Register();
		}
	}
}
	
