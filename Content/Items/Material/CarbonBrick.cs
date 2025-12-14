using ArknightsMod.Content.Tiles.Infrastructure;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Material
{
	public class CarbonBrick : ArknightsMaterial
	{
		public override int Rarity => 2;
		public override void SafeSetDefaults() {
			Item.value = Item.sellPrice(0, 0, 10, 0);
		}
		public override void AddRecipes() {
		}
	}
}
