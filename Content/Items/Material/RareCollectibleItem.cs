using Terraria.ID;

namespace ArknightsMod.Content.Items.Material
{
	// 血蕈/霜晶树/回声玉米这类“稀有自然采集物”的共同基类：统一强制紫色稀有度，
	// 不用基类 ArknightsMaterial 的稀有度色表（那套只到 LightPurple）。
	// 具体估价逻辑各自在 UpdateInventory 里动态改写 Item.value，这里不管。
	public abstract class RareCollectibleItem : ArknightsMaterial
	{
		public override int Rarity => 0; // 不生效，颜色统一在下面改写

		public sealed override void SafeSetDefaults() {
			Item.rare = ItemRarityID.Purple;
			SafeSetCollectibleDefaults();
		}

		public virtual void SafeSetCollectibleDefaults() { }
	}
}
