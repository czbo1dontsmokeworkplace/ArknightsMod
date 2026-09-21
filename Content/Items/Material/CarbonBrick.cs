using Terraria;

namespace ArknightsMod.Content.Items.Material
{
	public class CarbonBrick : ArknightsMaterial
	{
		public override int Rarity => 2;

		// 碳素是材料货架上唯一用金币结算的常驻商品（见 Closure.PriceMaterialByRarity）：
		// 不参与合成玉定价与回收，也不打合成玉回收提示。
		public override bool TradedWithOrundum => false;

		public override void SafeSetDefaults() {
			// 金币定价走 Item.value：sellPrice 的参数即玩家卖给 NPC 能拿到的价钱，
			// 这里取蓝档回收价 25 → 回收 25 银；货架买入价 = value = 1金25银（原版 5:1 差价）。
			Item.value = Item.sellPrice(0, 0, 25, 0);
		}

		public override void AddRecipes() {
		}
	}
}
