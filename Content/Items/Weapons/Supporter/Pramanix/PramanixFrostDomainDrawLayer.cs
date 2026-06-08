using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Supporter.Pramanix
{
	public class PramanixFrostDomainDrawLayer : PlayerDrawLayer
	{
		public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.ForbiddenSetRing);

		public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
			=> drawInfo.shadow == 0f;

		protected override void Draw(ref PlayerDrawSet drawInfo) {
			Player player = drawInfo.drawPlayer;
			if (!player.active || player.dead || player.HeldItem.ModItem is not SaintBell)
				return;

			var state = player.GetModPlayer<SaintBellPlayer>();
			if (state.FrostTier <= 0)
				return;

			float radius = FrostDomainLogic.GetRadius(state.FrostTier, state.Skill3Active);
			if (radius <= 0f)
				return;

			float budgetRatio = state.FrostHitBudget / (float)Math.Max(1, FrostDomainLogic.HitBudgetMax[state.FrostTier]);
			Vector2 center = player.Center - Main.screenPosition;
			float pulse = (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2.4f) * 0.06f + 0.94f;
			// 各级寒域使用相同的一级环带视觉，层级差异仅由粒子数量体现。
			float alpha = budgetRatio * pulse;

			DrawGroundMist(center, radius, alpha * 0.55f);
			DrawWavyRing(center, radius, alpha * 0.95f, 4.5f, 5f);
			DrawWavyRing(center, radius * 0.88f, alpha * 0.55f, 2.8f, 7f);
		}

		private static void DrawGroundMist(Vector2 center, float radius, float alpha) {
			if (alpha <= 0.01f)
				return;

			int spokes = 36;
			Color spokeColor = new Color(190, 225, 255) * (alpha * 0.35f);
			for (int i = 0; i < spokes; i++) {
				float angle = i / (float)spokes * MathHelper.TwoPi;
				Vector2 dir = angle.ToRotationVector2();
				float wobble = (float)Math.Sin(angle * 3f + Main.GlobalTimeWrappedHourly * 3f) * radius * 0.04f;
				Vector2 end = center + dir * (radius * 0.82f + wobble);
				Utils.DrawLine(Main.spriteBatch, center, end, Color.Transparent, spokeColor, 6f);
			}

			int fillRings = 6;
			for (int i = 0; i < fillRings; i++) {
				float t = (i + 1) / (float)(fillRings + 1);
				float ringRadius = radius * t;
				Color ringColor = new Color(170, 210, 255) * (alpha * (1f - t) * 0.12f);
				DrawSoftRing(center, ringRadius, ringColor, 10f);
			}
		}

		private static void DrawSoftRing(Vector2 center, float radius, Color color, float thickness) {
			if (radius <= 4f)
				return;

			int segments = 48;
			for (int i = 0; i < segments; i++) {
				float a0 = i / (float)segments * MathHelper.TwoPi;
				float a1 = (i + 1) / (float)segments * MathHelper.TwoPi;
				Vector2 p0 = center + a0.ToRotationVector2() * radius;
				Vector2 p1 = center + a1.ToRotationVector2() * radius;
				Utils.DrawLine(Main.spriteBatch, p0, p1, color, color, thickness);
			}
		}

		private static void DrawWavyRing(Vector2 center, float radius, float alpha, float thickness, float waveFreq) {
			if (radius <= 4f || alpha <= 0.01f)
				return;

			int segments = 72;
			Color color = new Color(210, 240, 255) * alpha;
			for (int i = 0; i < segments; i++) {
				float a0 = i / (float)segments * MathHelper.TwoPi;
				float a1 = (i + 1) / (float)segments * MathHelper.TwoPi;
				float wobble0 = (float)Math.Sin(a0 * waveFreq + Main.GlobalTimeWrappedHourly * 7f) * 8f;
				float wobble1 = (float)Math.Sin(a1 * waveFreq + Main.GlobalTimeWrappedHourly * 7f) * 8f;
				Vector2 p0 = center + a0.ToRotationVector2() * (radius + wobble0);
				Vector2 p1 = center + a1.ToRotationVector2() * (radius + wobble1);
				Utils.DrawLine(Main.spriteBatch, p0, p1, color, color, thickness);
			}
		}
	}
}
