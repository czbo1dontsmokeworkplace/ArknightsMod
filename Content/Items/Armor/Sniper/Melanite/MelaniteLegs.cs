using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Sniper.Melanite
{
	[AutoloadEquip(EquipType.Legs)]
	public class MelaniteLegs : NeoArmorLegs
	{
		public override int Rarity => 5;
		public override int ArmorLifeBonus => 83;
		
		public override void SetArmorDefaults() {
			Item.defense = 5;
		}

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<MelaniteLegs>(1)
			.AddIngredient<Orundum>(50)
			.AddIngredient<TransmutedSaltAgglomerate>(3)
			.AddIngredient<PolyesterPack>(3)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
