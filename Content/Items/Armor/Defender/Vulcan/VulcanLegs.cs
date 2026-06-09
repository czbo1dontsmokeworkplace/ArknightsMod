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

namespace ArknightsMod.Content.Items.Armor.Defender.Vulcan
{
	[AutoloadEquip(EquipType.Legs)]
	public class VulcanLegs : NeoArmorLegs
	{
		public override int Rarity => 5;
		public override int ArmorLifeBonus => 205;
		
		public override void SetArmorDefaults() {
			Item.defense = 15;
		}

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<VulcanLegs>(1)
			.AddIngredient<Orundum>(50)
			.AddIngredient<ManganeseTrihydrate>(3)
			.AddIngredient<IntegratedDevice>(2)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
