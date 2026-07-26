using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Caster.Necrass
{
	[AutoloadEquip(EquipType.Body)]
	public class NecrassBody : NeoArmorBody
	{
		public override int Rarity => 5;
		public override int ArmorLifeBonus => 83;

		public override void SetArmorDefaults() {
			Item.defense = 9;
		}

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<NecrassBody>(1)
			.AddIngredient<Orundum>(50)
			.AddIngredient<WhiteHorseKohl>(3)
			.AddIngredient<Aketon>(5)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
