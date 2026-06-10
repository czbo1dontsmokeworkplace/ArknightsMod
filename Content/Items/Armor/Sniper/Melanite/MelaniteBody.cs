using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Sniper.Melanite
{
	[AutoloadEquip(EquipType.Body)]
	public class MelaniteBody : NeoArmorBody
	{
		public override int Rarity => 5;
		public override int ArmorLifeBonus => 83;

		public override void SetArmorDefaults() {
			Item.defense = 16;
		}
		
		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<MelaniteBody>(1)
			.AddIngredient<Orundum>(50)
			.AddIngredient<IncandescentAlloyBlock>(3)
			.AddIngredient<LoxicKohl>(4)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
