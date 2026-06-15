using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Sniper.Melanite
{
	[AutoloadEquip(EquipType.Head)]
	public class MelaniteHead : NeoArmorHead
	{
		public override int Rarity => 5;
		public override int ArmorLifeBonus => 167;
		
		public override void Load() {
			if (Main.netMode == NetmodeID.Server)
				return;
		}

		public override void SetArmorDefaults() {
			Item.defense = 0;
		}
		

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<MelaniteHead>(1)
			.AddIngredient<Orundum>(50)
			.AddIngredient<RefinedSolvent>(8)
			.AddIngredient<LoxicKohl>(15)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
	
