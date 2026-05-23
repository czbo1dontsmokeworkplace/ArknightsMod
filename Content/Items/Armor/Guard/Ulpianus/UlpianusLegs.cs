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

namespace ArknightsMod.Content.Items.Armor.Guard.Ulpianus
{
	[AutoloadEquip(EquipType.Legs)]
	public class UlpianusLegs : NeoArmorLegs
	{
		public override int Rarity => 6;
		public override int ArmorLifeBonus => 301;
		
		public override void SetArmorDefaults() {
			Item.defense = 0;
		}

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<UlpianusLegs>(1)
			.AddIngredient<Orundum>(60)
			.AddIngredient<D32Steel>(6)
			//.AddIngredient<固化纤维板>(6) 材料不存在！
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
