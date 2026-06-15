using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Defender.Spot
{
	[AutoloadEquip(EquipType.Body)]
	public class SpotBody : NeoArmorBody
	{
		public override int Rarity => 3;
		public override int ArmorLifeBonus => 92;

		public override void SetArmorDefaults() {
			Item.defense = 35;
		}
		
		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<SpotBody>(1)
			.AddIngredient<Orundum>(30)
			.AddIngredient<OrirockCube>(3)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
