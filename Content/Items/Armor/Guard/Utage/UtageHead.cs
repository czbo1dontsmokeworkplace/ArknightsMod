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

namespace ArknightsMod.Content.Items.Armor.Guard.Utage
{
	[AutoloadEquip(EquipType.Head)]
	public class UtageHead : NeoArmorHead
	{
		public override int Rarity => 4;
		public override int ArmorLifeBonus => 344;
		
		public override void Load() {
			if (Main.netMode == NetmodeID.Server)
				return;
		}

		public override void SetArmorDefaults() {
			Item.defense = 0;
		}
		
		public override bool IsArmorSet(Item head, Item body, Item legs) {
			return body.type == ModContent.ItemType<UtageBody>() && body.neoarmor().hasUpgraded &&
				legs.type == ModContent.ItemType<UtageLegs>() && legs.neoarmor().hasUpgraded;
		}

		public override void UpdateArmorSet(Player player) {
			player.setBonus = "";
			player.GetModPlayer<UtageSetPlayer>().UtageSetActive = true;
		}

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<UtageHead>(1)
			.AddIngredient<Orundum>(40)
			.AddIngredient<Sugar>(1)
			.AddIngredient<OrirockCluster>(14)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
	
