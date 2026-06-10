using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Medic.Warfarin
{
	[AutoloadEquip(EquipType.Body)]
	public class WarfarinBody : NeoArmorBody
	{
		public override int Rarity => 5;
		public override int ArmorLifeBonus => 76;

		public override void SetArmorDefaults() {
			Item.defense = 10;
		}
		
		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<WarfarinBody>(1)
			.AddIngredient<Orundum>(50)
			.AddIngredient<SugarLump>(3)
			.AddIngredient<RMA7012>(3)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
