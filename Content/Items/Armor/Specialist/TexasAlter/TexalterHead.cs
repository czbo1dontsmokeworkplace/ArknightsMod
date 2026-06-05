using ArknightsMod.Content.Items.Material;
using ArknightsMod.Content.Items.Material.T4;
using ArknightsMod.Content.Items.Material.T5;
using ArknightsMod.Content.Tiles.Infrastructure;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Specialist.TexasAlter
{
	[AutoloadEquip(EquipType.Head)]
	public class TexalterHead : NeoArmorHead
	{
		public override int Rarity => 6;
		public override int ArmorLifeBonus => 160;
		
		public override void Load() {
			if (Main.netMode == NetmodeID.Server)
				return;
		}

		public override void SetArmorDefaults() {
			Item.defense = 0;
		}
		


		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<TexalterHead>(1)
			.AddIngredient<Orundum>(60)
			.AddIngredient<NucleicCrystalSinter>(6)
			.AddIngredient<KetonColloid>(4)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
