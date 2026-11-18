using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace ArknightsMod.Content.Items.Weapons.Supporter.Pramanix
{
	internal static class PramanixFrostDomainVisuals
	{
		internal static void Draw(SpriteBatch batch, Vector2 center, byte tier, float radius) {
			float pulse = (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2.4f) * 0.06f + 0.94f;
			float alpha = pulse * (tier == 1 ? 0.45f : tier == 2 ? 0.60f : 0.75f);

			// 各等级颜色
			Color tierColor = tier switch {
				1 => new Color(200, 230, 255),
				2 => new Color(140, 200, 255),
				3 => new Color(80, 160, 255),
				_ => new Color(200, 230, 255),
			};
			// 三级寒域外环额外增亮
			float brightness = tier == 3 ? 1.35f : 1f;

			DrawGroundMist(batch, center, radius, alpha * 0.55f, tierColor);

			// 各等级绘制对应数量的波动环（等级越高，环越多、颜色越深、亮度越强）
			for (int r = 0; r < tier; r++) {
				float ringScale = r == 0 ? 1f : (r == 1 ? 0.88f : 0.76f);
				float ringAlpha = r == 0 ? alpha * 0.95f : (r == 1 ? alpha * 0.65f : alpha * 0.40f);
				float waveFreq = r == 0 ? 5f : (r == 1 ? 7f : 9f);
				float thickness = r == 0 ? 4.5f : (r == 1 ? 3f : 2f);
				DrawWavyRing(batch, center, radius * ringScale, ringAlpha * brightness, thickness, waveFreq, tierColor);
			}
			DrawDriftingFlakes(batch, center, radius, tier, tierColor);
		}

		private static void DrawDriftingFlakes(SpriteBatch batch, Vector2 center, float radius, byte tier, Color tierColor) {
			float time = Main.GlobalTimeWrappedHourly;
			Texture2D pixel = TextureAssets.MagicPixel.Value;
			int count = 5 + tier * 5;
			for (int i = 0; i < count; i++) {
				float t = (i * 0.618034f + time * 0.28f) % 1f;
				float angle = i * 2.399963f + time * (i % 2 == 0 ? 0.20f : -0.20f);
				float distance = MathHelper.Lerp(radius * 0.18f, radius * 0.92f, t);
				Vector2 pos = center + angle.ToRotationVector2() * distance;
				float alpha = MathF.Sin(t * MathHelper.Pi) * (0.60f + tier * 0.09f);
				int size = tier == 1 ? 2 : 3;
				batch.Draw(pixel, new Rectangle((int)pos.X, (int)pos.Y, size, size), new Rectangle(0, 0, 1, 1), tierColor * alpha);
			}
		}

		private static void DrawGroundMist(SpriteBatch batch, Vector2 center, float radius, float alpha, Color tierColor) {
			if (alpha <= 0.01f)
				return;

			int spokes = 36;
			Color spokeColor = tierColor * (alpha * 0.30f);
			for (int i = 0; i < spokes; i++) {
				float angle = i / (float)spokes * MathHelper.TwoPi;
				Vector2 dir = angle.ToRotationVector2();
				float wobble = (float)Math.Sin(angle * 3f + Main.GlobalTimeWrappedHourly * 3f) * radius * 0.04f;
				Vector2 end = center + dir * (radius * 0.82f + wobble);
				Utils.DrawLine(batch, center, end, Color.Transparent, spokeColor, 6f);
			}

			int fillRings = 6;
			for (int i = 0; i < fillRings; i++) {
				float t = (i + 1) / (float)(fillRings + 1);
				float ringRadius = radius * t;
				Color ringColor = tierColor * (alpha * (1f - t) * 0.10f);
				DrawSoftRing(batch, center, ringRadius, ringColor, 10f);
			}
		}

		private static void DrawSoftRing(SpriteBatch batch, Vector2 center, float radius, Color color, float thickness) {
			if (radius <= 4f)
				return;

			int segments = 48;
			for (int i = 0; i < segments; i++) {
				float a0 = i / (float)segments * MathHelper.TwoPi;
				float a1 = (i + 1) / (float)segments * MathHelper.TwoPi;
				Vector2 p0 = center + a0.ToRotationVector2() * radius;
				Vector2 p1 = center + a1.ToRotationVector2() * radius;
				Utils.DrawLine(batch, p0, p1, color, color, thickness);
			}
		}

		private static void DrawWavyRing(SpriteBatch batch, Vector2 center, float radius, float alpha, float thickness, float waveFreq, Color tierColor) {
			if (radius <= 4f || alpha <= 0.01f)
				return;

			int segments = 72;
			Color color = tierColor * alpha;
			for (int i = 0; i < segments; i++) {
				float a0 = i / (float)segments * MathHelper.TwoPi;
				float a1 = (i + 1) / (float)segments * MathHelper.TwoPi;
				float wobble0 = (float)Math.Sin(a0 * waveFreq + Main.GlobalTimeWrappedHourly * 7f) * 8f;
				float wobble1 = (float)Math.Sin(a1 * waveFreq + Main.GlobalTimeWrappedHourly * 7f) * 8f;
				Vector2 p0 = center + a0.ToRotationVector2() * (radius + wobble0);
				Vector2 p1 = center + a1.ToRotationVector2() * (radius + wobble1);
				Utils.DrawLine(batch, p0, p1, color, color, thickness);
			}
		}
	}
}
