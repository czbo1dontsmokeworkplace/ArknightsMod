using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace ArknightsMod.Content.Items.Weapons.Supporter.Pramanix
{
	public static class PramanixHitVfx
	{
		private static readonly Color SlowSmokeColor = new(140, 195, 255);

		// 命中敌人时生成淡蓝色下落烟雾，表示被减速
		public static void SpawnSlowSmoke(NPC npc) {
			if (Main.netMode == NetmodeID.Server || !npc.active)
				return;

			for (int i = 0; i < 5; i++) {
				Vector2 pos = npc.Center + new Vector2(
					Main.rand.NextFloat(-npc.width * 0.35f, npc.width * 0.35f),
					Main.rand.NextFloat(-npc.height * 0.25f, npc.height * 0.2f));
				Dust d = Dust.NewDustPerfect(pos, DustID.Cloud,
					new Vector2(Main.rand.NextFloat(-0.6f, 0.6f), Main.rand.NextFloat(0.5f, 2.4f)),
					90, SlowSmokeColor, 0.75f + Main.rand.NextFloat(0.35f));
				d.noGravity = false;
				d.fadeIn = 0.35f;
			}
		}
	}
}
