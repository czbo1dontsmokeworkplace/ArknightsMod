using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;
using ArknightsMod.Content.Items.Material.T1;
using ArknightsMod.Content.Items.Material.T2;
using ArknightsMod.Content.Items.Material.T3;
using ArknightsMod.Content.Items.Material.T4;
using ArknightsMod.Content.Items.Material.T5;

namespace ArknightsMod.Content.Items.Armor.Specialist.Mizuki
{
	[AutoloadEquip(EquipType.Body)]
	public class MizukiBody : NeoArmorBody
	{
		public override int Rarity => 6;
		public override int ArmorLifeBonus => 88;

		public override void SetArmorDefaults() {
			Item.defense = 27;
		}
		
		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<MizukiBody>(1)
			.AddIngredient<Orundum>(60)
			.AddIngredient<CrystallineElectronicUnit>(6)
			.AddIngredient<WhiteHorseKohl>(4)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
