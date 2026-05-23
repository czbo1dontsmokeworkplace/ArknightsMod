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

namespace ArknightsMod.Content.Items.Armor.Sniper.Fiammetta
{
	[AutoloadEquip(EquipType.Body)]
	public class FiammettaBody : NeoArmorBody
	{
		public override int Rarity => 6;
		public override int ArmorLifeBonus => 96;

		public override void SetArmorDefaults() {
			Item.defense = 12;
		}
		
		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<FiammettaBody>(1)
			.AddIngredient<Orundum>(60)
			.AddIngredient<BipolarNanoflake>(6)
			.AddIngredient<WhiteHorseKohl>(5)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
