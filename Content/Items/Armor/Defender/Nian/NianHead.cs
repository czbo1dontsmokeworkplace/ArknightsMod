using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Defender.Nian
{
	[AutoloadEquip(EquipType.Head)]
	public class NianHead : NeoArmorHead
	{
		public override int Rarity => 6;
		public override int ArmorLifeBonus => 410;
		
		public override void Load() {
			if (Main.netMode == NetmodeID.Server)
				return;
		}

		public override void SetArmorDefaults() {
			Item.defense = 0;
		}
		

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<NianHead>(1)
			.AddIngredient<Orundum>(60)
			.AddIngredient<BipolarNanoflake>(6)
			.AddIngredient<PolymerizedGel>(6)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
	
