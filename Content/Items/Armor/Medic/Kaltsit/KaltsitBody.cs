using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Medic.Kaltsit
{
	[AutoloadEquip(EquipType.Body)]
	public class KaltsitBody : NeoArmorBody
	{
		public override int Rarity => 6;
		public override int ArmorLifeBonus => 102;

		public override void SetArmorDefaults() {
			Item.defense = 20;
		}
		
		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<KaltsitBody>(1)
			.AddIngredient<Orundum>(60)
			.AddIngredient<D32Steel>(6)
			.AddIngredient<KetonColloid>(5)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
