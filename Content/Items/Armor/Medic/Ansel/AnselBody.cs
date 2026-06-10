using ArknightsMod.Content.Items.Material;
using ArknightsMod.Content.Tiles.Infrastructure;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Medic.Ansel
{
	[AutoloadEquip(EquipType.Body)]
	public class AnselBody : NeoArmorBody
	{
		public override int Rarity => 3;
		public override int ArmorLifeBonus => 57;

		public override void SetArmorDefaults() {
			Item.defense = 8;
		}
		
		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<AnselBody>(1)
			.AddIngredient<Orundum>(30)
			.AddIngredient<Device>(1)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
