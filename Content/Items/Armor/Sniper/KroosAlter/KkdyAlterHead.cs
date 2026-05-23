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

namespace ArknightsMod.Content.Items.Armor.Sniper.KroosAlter
{
	[AutoloadEquip(EquipType.Head)]
	public class KkdyAlterHead : NeoArmorHead
	{
		public override int Rarity => 5;
		public override int ArmorLifeBonus => 152;
		
		public override void Load() {
			if (Main.netMode == NetmodeID.Server)
				return;
		}

		public override void SetArmorDefaults() {
			Item.defense = 0;
		}
		
		public override bool IsArmorSet(Item head, Item body, Item legs) {
			return body.type == ModContent.ItemType<KkdyAlterBody>() && body.neoarmor().hasUpgraded &&
				legs.type == ModContent.ItemType<KkdyAlterLegs>() && legs.neoarmor().hasUpgraded;
		}

		public override void UpdateArmorSet(Player player) {
			player.GetModPlayer<KkdyAlterSetPlayer>().KkdyAlterSetActive = true;
			player.setBonus = "";
		}

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<KkdyAlterHead>(1)
			.AddIngredient<Orundum>(50)
			.AddIngredient<Polyketon>(2)
			.AddIngredient<OrironCluster>(10)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
	
