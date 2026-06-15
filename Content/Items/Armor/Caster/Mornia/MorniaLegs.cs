using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Caster.Mornia
{
	[AutoloadEquip(EquipType.Legs)]
	public class MorniaLegs : NeoArmorLegs
	{
		public override int Rarity => 4;
		public override int ArmorLifeBonus => 50;

		public override void SetArmorDefaults() {
			Item.defense = 6;
		}

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<MorniaLegs>(1)
			.AddIngredient<Orundum>(40)
			.AddIngredient<Polyester>(3)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
