using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Vanguard.Plume
{
	[AutoloadEquip(EquipType.Body)]
	public class PlumeBody : NeoArmorBody
	{
		public override int Rarity => 3;
		public override int ArmorLifeBonus => 61;

		public override void SetArmorDefaults() {
			Item.defense = 21;
		}
		
		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<PlumeBody>(1)
			.AddIngredient<Orundum>(30)
			.AddIngredient<Sugar>(2)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
