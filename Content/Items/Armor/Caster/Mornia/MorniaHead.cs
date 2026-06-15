using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Caster.Mornia
{
	[AutoloadEquip(EquipType.Head)]
	public class MorniaHead : NeoArmorHead
	{
		public override int Rarity => 4;
		public override int ArmorLifeBonus => 100;

		public override void SetArmorDefaults() {
			Item.defense = 8;
		}

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<MorniaHead>(1)
			.AddIngredient<Orundum>(40)
			.AddIngredient<Aketon>(3)
			.AddIngredient<RefinedSolvent>(2)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
