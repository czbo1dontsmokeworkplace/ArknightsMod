using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Caster.Haze
{
	[AutoloadEquip(EquipType.Legs)]
	public class HazeLegs : NeoArmorLegs
	{
		public override int Rarity => 4;
		public override int ArmorLifeBonus => 44;
		
		public override void SetArmorDefaults() {
			Item.defense = 2;
		}

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<HazeLegs>(1)
			.AddIngredient<Orundum>(40)
			.AddIngredient<Sugar>(2)
			.AddIngredient<Aketon>(3)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
