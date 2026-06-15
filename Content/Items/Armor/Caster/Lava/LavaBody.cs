using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Caster.Lava
{
	[AutoloadEquip(EquipType.Body)]
	public class LavaBody : NeoArmorBody
	{
		public override int Rarity => 3;
		public override int ArmorLifeBonus => 57;

		public override void SetArmorDefaults() {
			Item.defense = 8;
		}
		
		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<LavaBody>(1)
			.AddIngredient<Orundum>(30)
			.AddIngredient<Oriron>(2)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
