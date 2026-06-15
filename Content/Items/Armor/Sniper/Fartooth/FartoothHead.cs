using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Sniper.Fartooth
{
	[AutoloadEquip(EquipType.Head)]
	public class FartoothHead : NeoArmorHead
	{
		public override int Rarity => 6;
		public override int ArmorLifeBonus => 152;
		
		public override void Load() {
			if (Main.netMode == NetmodeID.Server)
				return;
		}

		public override void SetArmorDefaults() {
			Item.defense = 0;
		}
		

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<FartoothHead>(1)
			.AddIngredient<Orundum>(60)
			.AddIngredient<BipolarNanoflake>(6)
			.AddIngredient<ManganeseTrihydrate>(5)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
	
