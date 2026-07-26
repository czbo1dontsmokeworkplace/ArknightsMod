using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Medic.ReedFlameShadow
{
	[AutoloadEquip(EquipType.Legs)]
	public class ReedFlameShadowLegs : NeoArmorLegs
	{
		public override int Rarity => 5;
		public override int ArmorLifeBonus => 83;

		public override void SetArmorDefaults() {
			Item.defense = 3;
		}

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<ReedFlameShadowLegs>(1)
			.AddIngredient<Orundum>(50)
			.AddIngredient<ManganeseTrihydrate>(3)
			.AddIngredient<IntegratedDevice>(2)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
