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

namespace ArknightsMod.Content.Items.Armor.Defender.Saria
{
	[AutoloadEquip(EquipType.Head)]
	public class SariaHead : NeoArmorHead
	{
		public override int Rarity => 6;
		public override int ArmorLifeBonus => 315;
		
		public override void Load() {
			if (Main.netMode == NetmodeID.Server)
				return;
		}

		public override void SetArmorDefaults() {
			Item.defense = 0;
		}
		
		public override bool IsArmorSet(Item head, Item body, Item legs) {
			return body.type == ModContent.ItemType<SariaBody>() && body.neoarmor().hasUpgraded &&
				legs.type == ModContent.ItemType<SariaLegs>() && legs.neoarmor().hasUpgraded;
		}

		public override void UpdateArmorEquip(Player Player) {
			Player.GetModPlayer<ArknightsArmorPlayer>().extraDefenseBonus += 0.05f;
		}

		public override void UpdateArmorSet(Player player) {
			player.setBonus = "";
			player.GetModPlayer<SariaSetPlayer>().SariaSetActive = true;
		}

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<SariaHead>(1)
			.AddIngredient<Orundum>(60)
			.AddIngredient<BipolarNanoflake>(6)
			.AddIngredient<ManganeseTrihydrate>(5)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
	
