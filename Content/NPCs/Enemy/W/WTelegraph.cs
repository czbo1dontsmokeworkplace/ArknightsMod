using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace ArknightsMod.Content.NPCs.Enemy.W
{
	/// <summary>
	/// W 战的预警与演出绘制库：激光、枪口火光、锥形扇、落点 X、骰面数字、♥K 卡牌、烟带。<br/>
	/// 全部程序化绘制（不依赖新贴图），供 boss / 弹幕 / 粒子在各自的绘制钩子里调用。<br/>
	/// 加法混合的图元用 <see cref="BeginAdditive"/> / <see cref="EndAdditive"/> 包裹，恢复参数与原版 NPC/弹幕批次一致。
	/// </summary>
	public static class WTelegraph
	{
		public static Texture2D Pixel => TextureAssets.MagicPixel.Value;
		public static Texture2D Glow => ModContent.Request<Texture2D>("ArknightsMod/Content/Projectiles/Bosses/W/WSmoke").Value;

		public static readonly Color LaserCore = new(255, 70, 60);
		public static readonly Color LaserHalo = new(200, 30, 30);
		public static readonly Color WarnRed = new(255, 60, 50);
		public static readonly Color SmokeDark = new(48, 40, 44);
		public static readonly Color SmokeRed = new(120, 46, 44);

		private static readonly Rectangle OnePixel = new(0, 0, 1, 1);

		public static void BeginAdditive(SpriteBatch sb) {
			sb.End();
			sb.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState,
				DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
		}

		public static void EndAdditive(SpriteBatch sb) {
			sb.End();
			sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
				DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
		}

		/// <summary>一条从 from 出发、长 length 的线段（世界坐标，厚度 px）</summary>
		public static void DrawLine(SpriteBatch sb, Vector2 from, float rotation, float length, float thickness, Color color) {
			sb.Draw(Pixel, from - Main.screenPosition, OnePixel, color, rotation, new Vector2(0f, 0.5f), new Vector2(length, thickness), SpriteEffects.None, 0f);
		}

		/// <summary>瞄准激光（加法批次内调用）：光晕 + 细线 + 末端跳动红点 + 起点微光；k 为强度 0~1</summary>
		public static void DrawLaser(SpriteBatch sb, Vector2 muzzle, float rotation, float length, float k) {
			k = MathHelper.Clamp(k, 0f, 1f);
			Vector2 dir = rotation.ToRotationVector2();
			Color core = LaserCore * (0.55f + 0.45f * k);
			Color halo = LaserHalo * (0.18f + 0.22f * k);
			DrawLine(sb, muzzle, rotation, length, 5f + 3f * k, halo);
			DrawLine(sb, muzzle, rotation, length, 1.5f, core);
			float pulse = 0.6f + 0.4f * MathF.Sin(Main.GlobalTimeWrappedHourly * 18f);
			sb.Draw(Glow, muzzle + dir * length - Main.screenPosition, null, core * pulse * k, 0f, Glow.Size() * 0.5f, 0.28f * k, SpriteEffects.None, 0f);
			sb.Draw(Glow, muzzle - Main.screenPosition, null, core * (0.5f * k), 0f, Glow.Size() * 0.5f, 0.22f, SpriteEffects.None, 0f);
		}

		/// <summary>枪口火光（加法批次内调用）：两层软光球 + 沿射向的火舌；f 为剩余比例 0~1</summary>
		public static void DrawMuzzleFlash(SpriteBatch sb, Vector2 muzzle, float rotation, float f) {
			Color hot = new Color(255, 200, 120) * f;
			Color warm = new Color(255, 110, 40) * (f * 0.8f);
			sb.Draw(Glow, muzzle - Main.screenPosition, null, hot, 0f, Glow.Size() * 0.5f, 0.35f + 0.5f * f, SpriteEffects.None, 0f);
			sb.Draw(Glow, muzzle - Main.screenPosition, null, warm, 0f, Glow.Size() * 0.5f, 0.7f + 0.6f * f, SpriteEffects.None, 0f);
			DrawLine(sb, muzzle, rotation, 26f + 30f * f, 3f + 3f * f, hot);
		}

		/// <summary>
		/// 锥形扇预警（加法批次内调用）：从 origin 沿 facing 张开 ±halfAngle、长 length 的一束射线，中心亮边缘暗。<br/>
		/// 用于「此面向敌」的正面杀伤区。
		/// </summary>
		public static void DrawCone(SpriteBatch sb, Vector2 origin, float facing, float halfAngle, float length, Color color, int rays = 17) {
			for (int i = 0; i < rays; i++) {
				float t = rays == 1 ? 0f : i / (float)(rays - 1) * 2f - 1f; // -1..1
				float angle = facing + t * halfAngle;
				float edge = 1f - Math.Abs(t);
				Color c = color * (0.12f + 0.3f * edge);
				DrawLine(sb, origin, angle, length * (0.85f + 0.15f * edge), 1.5f, c);
			}
			// 两条边界线略亮，扇形轮廓读得出来
			DrawLine(sb, origin, facing - halfAngle, length * 0.85f, 1.5f, color * 0.55f);
			DrawLine(sb, origin, facing + halfAngle, length * 0.85f, 1.5f, color * 0.55f);
		}

		/// <summary>
		/// 大型锁定圈（加法批次内调用）：核爆的杀伤边界——用 160px 的预警圈贴图放大到 radius，外加十字线与旋转刻度；k 为强度。
		/// </summary>
		public static void DrawTargetRing(SpriteBatch sb, Vector2 center, float radius, float k) {
			Texture2D ring = ModContent.Request<Texture2D>("ArknightsMod/Content/Projectiles/Bosses/W/WClaymoreRing").Value;
			Color c = WarnRed * (0.55f * k);
			sb.Draw(ring, center - Main.screenPosition, null, c, 0f, ring.Size() * 0.5f, radius / 80f, SpriteEffects.None, 0f);
			sb.Draw(ring, center - Main.screenPosition, null, c * 0.5f, Main.GlobalTimeWrappedHourly * 0.6f, ring.Size() * 0.5f, radius / 80f * 0.97f, SpriteEffects.None, 0f);
			// 十字线
			DrawLine(sb, center - new Vector2(radius, 0f), 0f, radius * 2f, 1.5f, c * 0.6f);
			DrawLine(sb, center - new Vector2(0f, radius), MathHelper.PiOver2, radius * 2f, 1.5f, c * 0.6f);
			// 旋转刻度：八个短线沿圈转
			for (int i = 0; i < 8; i++) {
				float a = Main.GlobalTimeWrappedHourly * 1.2f + i * MathHelper.TwoPi / 8f;
				Vector2 tick = center + a.ToRotationVector2() * (radius - 14f);
				DrawLine(sb, tick, a, 14f, 3f, c);
			}
			sb.Draw(Glow, center - Main.screenPosition, null, c * 0.35f, 0f, Glow.Size() * 0.5f, radius / 60f, SpriteEffects.None, 0f);
		}

		/// <summary>落点 X 标记（加法批次内调用）：曲射弹将落之处；k 为强度</summary>
		public static void DrawGroundX(SpriteBatch sb, Vector2 center, float size, float k) {
			Color c = WarnRed * (0.45f + 0.45f * k);
			Vector2 c1 = center - new Vector2(size, size) * 0.5f;
			Vector2 c2 = center - new Vector2(-size, size) * 0.5f;
			float diag = size * 1.4142f;
			DrawLine(sb, c1, MathHelper.PiOver4, diag, 2f, c);
			DrawLine(sb, c2, MathHelper.PiOver4 * 3f, diag, 2f, c);
			sb.Draw(Glow, center - Main.screenPosition, null, c * 0.5f, 0f, Glow.Size() * 0.5f, size / 40f, SpriteEffects.None, 0f);
		}

		/// <summary>骰面数字（普通批次内调用）：带描边的数字，12 用金色</summary>
		public static void DrawDieFace(SpriteBatch sb, Vector2 center, int number, float scale, float alpha) {
			DynamicSpriteFont font = FontAssets.MouseText.Value;
			string text = number.ToString();
			Vector2 size = font.MeasureString(text) * scale;
			Color color = (number == 12 ? new Color(255, 214, 90) : Color.White) * alpha;
			Color shadow = Color.Black * (0.8f * alpha);
			ChatManager.DrawColorCodedStringWithShadow(sb, font, text, center - Main.screenPosition - size * 0.5f,
				color, shadow, 0f, Vector2.Zero, new Vector2(scale), -1f, 2f);
		}

		/// <summary>
		/// ♥K 小卡（普通批次内调用）：12×16 白卡红边。flip 为翻转进度，0→1 从背面翻到正面（0.5 处侧立），<br/>
		/// 继续到 2 翻回背面——连续递增即可让卡牌一直转。
		/// </summary>
		public static void DrawCard(SpriteBatch sb, Vector2 center, float rotation, float flip, float scale, float alpha) {
			float p = flip % 2f;
			if (p < 0f)
				p += 2f;
			float xScale = Math.Abs(MathF.Cos(p * MathHelper.Pi));
			if (xScale < 0.05f)
				return;
			Vector2 pos = center - Main.screenPosition;
			Vector2 cardSize = new Vector2(12f * xScale, 16f) * scale;
			Vector2 origin = new(0.5f, 0.5f);
			bool face = p >= 0.5f && p < 1.5f;
			Color body = (face ? Color.White : new Color(150, 40, 45)) * alpha;
			Color border = (face ? new Color(200, 40, 45) : new Color(240, 220, 200)) * alpha;
			sb.Draw(Pixel, pos, OnePixel, border, rotation, origin, cardSize + new Vector2(2f * scale), SpriteEffects.None, 0f);
			sb.Draw(Pixel, pos, OnePixel, body, rotation, origin, cardSize, SpriteEffects.None, 0f);
			if (face && xScale > 0.35f) {
				DynamicSpriteFont font = FontAssets.MouseText.Value;
				float textScale = 0.55f * scale;
				Vector2 size = font.MeasureString("K") * textScale;
				ChatManager.DrawColorCodedStringWithShadow(sb, font, "K", pos - size * 0.5f + new Vector2(0f, 1f), new Color(200, 40, 45) * alpha, Color.Transparent,
					rotation, Vector2.Zero, new Vector2(textScale * xScale, textScale), -1f, 1.5f);
			}
		}

		/// <summary>
		/// 烟带（普通批次内调用）：一团沿 dir 拖尾的暗色软烟，用于烟中冲刺的可见本体；k 为浓度 0~1。
		/// </summary>
		public static void DrawSmokeBank(SpriteBatch sb, Vector2 head, Vector2 dir, float k) {
			for (int i = 0; i < 5; i++) {
				float back = i * 22f;
				float t = Main.GlobalTimeWrappedHourly * 3f + i * 1.7f;
				Vector2 pos = head - dir * back + new Vector2(MathF.Sin(t) * 4f, MathF.Cos(t * 1.3f) * 4f);
				float fade = 1f - i / 5.5f;
				Color c = Color.Lerp(SmokeDark, SmokeRed, 0.25f + 0.1f * i) * (0.78f * fade * k);
				float scale = (1.35f - i * 0.12f) * (0.9f + 0.1f * MathF.Sin(t * 2.1f));
				sb.Draw(Glow, pos - Main.screenPosition, null, c, t * 0.4f, Glow.Size() * 0.5f, scale, SpriteEffects.None, 0f);
			}
		}
	}
}
