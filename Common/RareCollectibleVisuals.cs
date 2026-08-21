using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ObjectData;

namespace ArknightsMod.Common
{
	// 稀有自然采集物（血蕈/霜晶树/回声玉米...）共用的绘制表现：整体绘制向下偏移一像素，
	// 并偶尔冒出闪亮粒子。各自的 ModTile 在 SetStaticDefaults 里调 ApplyDrawOffset，
	// 在 RandomUpdate 里调 EmitAmbientSparkle。
	public static class RareCollectibleVisuals
	{
		public const int DrawYOffsetPixels = 1;

		public static void ApplyDrawOffset() {
			TileObjectData.newTile.DrawYOffset = DrawYOffsetPixels;
		}

		public static void EmitAmbientSparkle(int i, int j, int width, int height) {
			if (!Main.rand.NextBool(90)) return; // 出现得慢一点，不要糊成一片

			Vector2 pos = new Vector2(i, j) * 16f + new Vector2(Main.rand.Next(width * 16), Main.rand.Next(height * 16));
			Dust dust = Dust.NewDustPerfect(pos, DustID.TreasureSparkle, new Vector2(0f, -0.3f), 150, default, 1f);
			dust.noGravity = true;
		}
	}
}
