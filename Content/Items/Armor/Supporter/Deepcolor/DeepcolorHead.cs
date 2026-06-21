using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Supporter.Deepcolor
{
	// 数值范例：取干员「对应时期」(精二满级) 官方数据 生命1050/防御125，
	// 生命基数 = 生命÷5 = 210，防御基数 = 防御÷10 = 12，
	// 再按 头50%/躯干25%/腿25%(生命) 与 头0%/躯干75%/腿25%(防御) 分配到三部位。
	[AutoloadEquip(EquipType.Head)]
	public class DeepcolorHead : NeoArmorHead
	{
		public override int Rarity => 4;
		public override int ArmorLifeBonus => 105; // 210 * 50%
		public override void SetArmorDefaults() {
			Item.defense = 0;
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
	
