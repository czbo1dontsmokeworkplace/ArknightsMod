using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Defender.Nian
{
	[AutoloadEquip(EquipType.Body)]
	public class NianBody : NeoArmorBody
	{
		public override int Rarity => 6;
		public override int ArmorLifeBonus => 205;

		public override void SetArmorDefaults() {
			Item.defense = 60;
		}
		
		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<NianBody>(1)
			.AddIngredient<Orundum>(60)
			.AddIngredient<PolymerizationPreparation>(6)
			.AddIngredient<PolymerizedGel>(7)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
