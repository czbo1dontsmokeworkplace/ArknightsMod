using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Guard.Chen
{
	[AutoloadEquip(EquipType.Body)]
	public class ChenBody : NeoArmorBody
	{
		public override int Rarity => 6;
		public override int ArmorLifeBonus => 144;

		public override void SetArmorDefaults() {
			Item.defense = 30;
		}
		
		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<ChenBody>(1)
			.AddIngredient<Orundum>(60)
			.AddIngredient<D32Steel>(6)
			.AddIngredient<PolyesterLump>(6)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
