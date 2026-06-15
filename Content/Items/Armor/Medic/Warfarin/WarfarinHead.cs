using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Medic.Warfarin
{
	[AutoloadEquip(EquipType.Head)]
	public class WarfarinHead : NeoArmorHead
	{
		public override int Rarity => 5;
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
			.AddIngredient<WarfarinHead>(1)
			.AddIngredient<Orundum>(50)
			.AddIngredient<OptimizedDevice>(5)
			.AddIngredient<SugarPack>(17)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
	
