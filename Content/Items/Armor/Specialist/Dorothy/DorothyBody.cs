using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Specialist.Dorothy
{
	[AutoloadEquip(EquipType.Body)]
	public class DorothyBody : NeoArmorBody
	{
		public override int Rarity => 6;
		public override int ArmorLifeBonus => 75;

		public override void SetArmorDefaults() {
			Item.defense = 13;
		}
		
		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<DorothyBody>(1)
			.AddIngredient<Orundum>(60)
			.AddIngredient<PolymerizationPreparation>(6)
			.AddIngredient<IncandescentAlloyBlock>(6)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
