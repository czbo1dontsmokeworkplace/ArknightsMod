using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

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
