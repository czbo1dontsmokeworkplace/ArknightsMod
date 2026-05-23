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

namespace ArknightsMod.Content.Items.Armor.Guard.Popukar
{
	[AutoloadEquip(EquipType.Body)]
	public class PopukarBody : NeoArmorBody
	{
		public override int Rarity => 3;
		public override int ArmorLifeBonus => 93;

		public override void SetArmorDefaults() {
			Item.defense = 18;
		}
		
		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<PopukarBody>(1)
			.AddIngredient<Orundum>(30)
			.AddIngredient<Oriron>(2)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
