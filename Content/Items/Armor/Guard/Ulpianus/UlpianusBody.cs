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
	[AutoloadEquip(EquipType.Body)]
	public class UlpianusBody : NeoArmorBody
	{
		public override int Rarity => 6;
		public override int ArmorLifeBonus => 301;

		public override void SetArmorDefaults() {
			Item.defense = 0;
		}
		
		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<UlpianusBody>(1)
			.AddIngredient<Orundum>(60)
			.AddIngredient<NucleicCrystalSinter>(6)
			.AddIngredient<RefinedSolvent>(2)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
