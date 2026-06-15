using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Caster.Mostima
{
	[AutoloadEquip(EquipType.Legs)]
	public class MostimaLegs : NeoArmorLegs
	{
		public override int Rarity => 6;
		public override int ArmorLifeBonus => 92;
		
		public override void SetArmorDefaults() {
			Item.defense = 3;
		}

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<MostimaLegs>(1)
			.AddIngredient<Orundum>(60)
			.AddIngredient<PolymerizationPreparation>(6)
			.AddIngredient<PolyesterLump>(6)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
