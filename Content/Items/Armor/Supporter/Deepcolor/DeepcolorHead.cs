using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Supporter.Deepcolor
{
	// PRTS 官方数据（精二满级）：生命 1050 / 防御 125，由 OperatorArmorStatFormula 统一换算
	[AutoloadEquip(EquipType.Head)]
	public class DeepcolorHead : NeoArmorHead
	{
		public override int Rarity => 4;
		public override int ArmorLifeBonus => OperatorArmorStatFormula.HeadLifeBonus(1050);
		public override void SetArmorDefaults() {
			Item.defense = OperatorArmorStatFormula.HeadDefenseBonus(125);
		}
		
		public override bool IsArmorSet(Item head, Item body, Item legs) {
			return body.type == ModContent.ItemType<DeepcolorBody>() && body.neoarmor().hasUpgraded &&
				legs.type == ModContent.ItemType<DeepcolorLegs>() && legs.neoarmor().hasUpgraded;
		}

		public override void UpdateArmorSet(Player player) {
			player.setBonus = "";
		}

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<DeepcolorHead>(1)
			.AddIngredient<Orundum>(40)
			.AddIngredient<Device>(1)
			.AddIngredient<ManganeseOre>(9)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
	
