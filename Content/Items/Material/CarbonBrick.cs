using Terraria;

namespace ArknightsMod.Content.Items.Material
{
	public class CarbonBrick : ArknightsMaterial
	{
		public override int Rarity => 2;
		public override void SafeSetDefaults() {
			Item.value = Item.buyPrice(0, 0, 10, 0);
		}
		public override void AddRecipes() {
		}
	}
}
