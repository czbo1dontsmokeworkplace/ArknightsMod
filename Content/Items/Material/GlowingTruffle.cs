namespace ArknightsMod.Content.Items.Material
{
	// 没有动态估价逻辑，固定基础估价。
	public class GlowingTruffle : RareCollectibleItem
	{
		public override void SafeSetCollectibleDefaults() {
			Item.value = 32;
		}
	}
}
