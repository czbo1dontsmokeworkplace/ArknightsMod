using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity.Caster.Indigo
{
	// See also: ExampleCostume
	[AutoloadEquip(EquipType.Legs)]
	public class IndigoLegs : ArknightsVanityLegs
	{
		public override int Rarity => ItemRarityID.Pink;

		public override void UpdateEquip(Player player)
		{
		}

		//public override void AddRecipes()
		//{
		//    Recipe recipe = CreateRecipe();
		//    recipe.AddRecipeGroup(RecipeGroupID.Wood, 2);
		//    recipe.AddTile(TileID.WorkBenches);
		//    recipe.Register();
		//}
	}
}
