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

namespace ArknightsMod.Content.Items.Armor.Defender.Vulcan
{
	[AutoloadEquip(EquipType.Body)]
	public class VulcanBody : NeoArmorBody
	{
		public override int Rarity => 5;
		public override int ArmorLifeBonus => 205;

		public override void SetArmorDefaults() {
			Item.defense = 44;
		}
		
		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<VulcanBody>(1)
			.AddIngredient<Orundum>(50)
			.AddIngredient<WhiteHorseKohl>(3)
			.AddIngredient<Aketon>(5)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
