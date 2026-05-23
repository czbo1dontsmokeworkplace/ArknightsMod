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
	[AutoloadEquip(EquipType.Body)]
	public class SariaBody : NeoArmorBody
	{
		public override int Rarity => 6;
		public override int ArmorLifeBonus => 158;

		public override void SetArmorDefaults() {
			Item.defense = 45;
		}
		
		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<SariaBody>(1)
			.AddIngredient<Orundum>(60)
			.AddIngredient<BipolarNanoflake>(6)
			.AddIngredient<KetonColloid>(5)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
