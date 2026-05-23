using ArknightsMod.Content.Items.Material;
using ArknightsMod.Content.Items.Material.T1;
using ArknightsMod.Content.Items.Material.T2;
using ArknightsMod.Content.Items.Material.T3;
using ArknightsMod.Content.Items.Material.T4;
using ArknightsMod.Content.Items.Material.T5;
using ArknightsMod.Content.Tiles.Infrastructure;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Medic.Kaltsit.Armor
{
	[AutoloadEquip(EquipType.Head)]
	public class ArmorKaltsitHead : ArknightsArmorHead
	{
		public override string Texture => "ArknightsMod/Content/Items/Armor/Medic/Kaltsit/KaltsitHead";
		public override int Rarity => 6;
		public override void SetArmorDefaults() {
			Item.defense = 0;
		}
		public override int LifeBonus => 164;
		public override void Load() {
			if (Main.netMode == NetmodeID.Server)
				return;
		}
		public override bool IsArmorSet(Item head, Item body, Item legs) {
			return body.type == ModContent.ItemType<ArmorKaltsitBody>() &&
				legs.type == ModContent.ItemType<ArmorKaltsitLegs>();
		}
		public override void UpdateArmorSet(Player player) {
			player.setBonus = "";
		}

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<KaltsitHead>(1)
			.AddIngredient<Orundum>(60)
			.AddIngredient<CrystallineElectronicUnit>(4)
			.AddIngredient<OptimizedDevice>(4)
			.AddTile(ModContent.TileType<FactoryTile>())
			.Register();
		}
	}
}
	
