using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Material.T3
{
	/// <summary>
	/// 轻锰矿，稀有度：蓝
	/// </summary>
	public class ManganeseOre : ArknightsMaterial
	{
		public override int Rarity => 2;
		public override void SafeSetStaticDefaults() {
			ItemID.Sets.SortingPriorityMaterials[Item.type] = 58;
		}
		public override void SafeSetDefaults() {
			Item.DefaultToPlaceableTile(ModContent.TileType<Tiles.ManganeseOreTile>());
		}
	}
}