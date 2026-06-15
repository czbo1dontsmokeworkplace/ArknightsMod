using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

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
