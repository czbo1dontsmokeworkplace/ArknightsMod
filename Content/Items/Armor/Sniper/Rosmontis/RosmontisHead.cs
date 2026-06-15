using ArknightsMod.Content.Items.Material;
using ArknightsMod.Content.Tiles.Infrastructure;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.Rosmontis
{
	[AutoloadEquip(EquipType.Head)]
	public class RosmontisHead : NeoArmorHead
	{
		public override int Rarity => 6;
		public override int ArmorLifeBonus => 195;
		
		public override void Load() {
		}

		public override void SetArmorDefaults() {
			Item.defense = 0;
		}
		
		public override bool IsArmorSet(Item head, Item body, Item legs) {
			return body.type == ModContent.ItemType<RosmontisBody>() && body.neoarmor().hasUpgraded &&
				legs.type == ModContent.ItemType<RosmontisLegs>() && legs.neoarmor().hasUpgraded;
		}

		public override void UpdateArmorSet(Player player) {
			player.setBonus = "Kiss~喵";
		}

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<RosmontisHead>(1)
			.AddIngredient<Orundum>(60)
			.AddIngredient<CrystallineElectronicUnit>(6)
			.AddIngredient<OrirockConcentration>(4)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
