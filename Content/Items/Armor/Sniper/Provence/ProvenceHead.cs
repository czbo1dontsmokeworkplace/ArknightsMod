using ArknightsMod.Content.Items.Material;
using ArknightsMod.Content.Tiles.Infrastructure;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.Provence
{
	[AutoloadEquip(EquipType.Head)]
	public class ProvenceHead : NeoArmorHead
	{
		public override int Rarity => 5;
		public override int ArmorLifeBonus => 168;
		
		public override void Load() {
			if (Main.netMode == NetmodeID.Server)
				return;
		}

		public override void SetArmorDefaults() {
			Item.defense = 0;
		}
		

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<ProvenceHead>(1)
			.AddIngredient<Orundum>(50)
			.AddIngredient<SugarLump>(9)
			.AddIngredient<IntegratedDevice>(7)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
