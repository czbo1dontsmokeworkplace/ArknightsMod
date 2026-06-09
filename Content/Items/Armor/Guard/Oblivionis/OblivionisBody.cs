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

namespace ArknightsMod.Content.Items.Armor.Guard.Oblivionis
{
	[AutoloadEquip(EquipType.Body)]
	public class OblivionisBody : NeoArmorBody
	{
		public override int Rarity => 6;
		public override int ArmorLifeBonus => 118;

		public override void SetArmorDefaults() {
			Item.defense = 32;
		}
		
		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<OblivionisBody>(1)
			.AddIngredient<Orundum>(60)
			.AddIngredient<D32Steel>(6)
			.AddIngredient<CuttingFluidSolution>(6)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
