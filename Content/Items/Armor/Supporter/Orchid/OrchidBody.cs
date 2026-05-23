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

namespace ArknightsMod.Content.Items.Armor.Supporter.Orchid
{
	[AutoloadEquip(EquipType.Body)]
	public class OrchidBody : NeoArmorBody
	{
		public override int Rarity => 3;
		public override int ArmorLifeBonus => 47;

		public override void SetArmorDefaults() {
			Item.defense = 6;
		}
		
		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<OrchidBody>(1)
			.AddIngredient<Orundum>(30)
			.AddIngredient<Polyester>(2)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
