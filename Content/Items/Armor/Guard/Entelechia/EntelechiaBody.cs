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

namespace ArknightsMod.Content.Items.Armor.Guard.Entelechia
{
	[AutoloadEquip(EquipType.Body)]
	public class EntelechiaBody : NeoArmorBody
	{
		public override int Rarity => 6;
		public override int ArmorLifeBonus => 129;

		public override void SetArmorDefaults() {
			Item.defense = 34;
		}
		
		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<EntelechiaBody>(1)
			.AddIngredient<Orundum>(60)
			.AddIngredient<PolymerizationPreparation>(6)
			.AddIngredient<RMA7024>(6)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
