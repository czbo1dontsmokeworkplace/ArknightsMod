using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

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
