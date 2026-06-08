using System;
using ArknightsMod.Content.Items.Weapons.Supporter.Pramanix;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Systems
{
	public class PramanixFrostVisualSystem : ModSystem
	{
		public override void PostUpdatePlayers() {
			if (Main.netMode == NetmodeID.Server)
				return;

			Player local = Main.LocalPlayer;
			if (local == null || !local.active)
				return;

			var sb = local.GetModPlayer<SaintBellPlayer>();
			if (local.HeldItem.ModItem is not SaintBell || sb.FrostTier <= 0)
				return;

			SpawnDomainDust(local, sb);
		}

		private static void SpawnDomainDust(Player player, SaintBellPlayer state) {
			int tier = state.FrostTier;
			// 一级寒域粒子密度为基准；二、三级仅增加生成次数。
			int spawnPasses = tier switch {
				1 => 1,
				2 => 2,
				3 => 3,
				_ => 0,
			};
			if (state.Skill3Active)
				spawnPasses += 2;

			int skipChance = tier switch {
				1 => 4,
				2 => 3,
				3 => 2,
				_ => 4,
			};
			if (state.Skill3Active)
				skipChance = Math.Max(1, skipChance - 1);

			if (Main.rand.NextBool(skipChance))
				return;

			float radius = FrostDomainLogic.GetRadius(tier, state.Skill3Active);

			for (int pass = 0; pass < spawnPasses; pass++) {
				float ring = radius * Main.rand.NextFloat(0.55f, 0.95f);
				Vector2 pos = player.Center + Main.rand.NextVector2CircularEdge(ring, ring);
				Dust d = Dust.NewDustPerfect(pos, DustID.Ice, Main.rand.NextVector2Circular(0.5f, 0.5f), 140, new Color(180, 220, 255), 1f);
				d.noGravity = true;
				d.fadeIn = 0.6f;

				if (Main.rand.NextBool(3)) {
					Vector2 inner = player.Center + Main.rand.NextVector2Circular(radius * 0.5f, radius * 0.5f);
					Dust snow = Dust.NewDustPerfect(inner, DustID.Snow, Main.rand.NextVector2Circular(0.3f, 0.3f), 100, Color.White, 0.85f);
					snow.noGravity = true;
				}
			}
		}
	}
}
