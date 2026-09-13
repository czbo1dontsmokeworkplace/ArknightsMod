using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Capture;
using Terraria.Graphics.Effects;

namespace ArknightsMod.Content.NPCs.Enemy.W
{
	/// <summary>
	/// W 战天空：随 <see cref="WBattleVisuals.Gloom"/> 被硝烟压暗，由全程序化着色器 WBattleSky.fx 绘制，分两层：<br/>
	/// - Far（最远层，日月星辰之后、云背景与远山之前）：渐变天穹 + 域扭曲 fbm 硝烟云 + 地平线火光 + 爆炸照云 + 核爆余烬<br/>
	/// - Near（远山之后、物块之前）：噪声暗幕 + 烟霭 + 落灰/余烬粒子 + 低血量红霭 + 暗角 + 核爆白炽<br/>
	/// 两层都带镜头视差；着色器缺失时退化为旧的像素绘制（<see cref="DrawFallback"/>）。<br/>
	/// 注册名 "ArknightsMod:WBattleSky"（ArknightsMod.Load），激活/关闭由 WBattleVisuals 驱动。
	/// </summary>
	public class WBattleSky : CustomSky
	{
		private bool active;
		private float intensity;
		private float flash;        // 爆炸照亮云底的强度，逐帧衰减
		private float flashX = 0.5f; // 照云位置（屏幕横向 0~1）

		public override string ToString() => "ArknightsMod:WBattleSky";

		public override void Activate(Vector2 position, params object[] args) {
			active = true;
			// 天空不在场期间积压的照云请求作废，避免刚亮起就无来由地闪一下
			WBattleVisuals.ConsumeSkyFlash(out _, out _);
			flash = 0f;
		}

		public override void Deactivate(params object[] args) => active = false;

		public override void Reset() {
			active = false;
			intensity = 0f;
			flash = 0f;
		}

		public override bool IsActive() => active || intensity > 0.01f;

		public override void Update(GameTime gameTime) {
			float target = active && !Main.gameMenu ? WBattleVisuals.Gloom : 0f;
			intensity += (target - intensity) * 0.04f;
			if (Math.Abs(target - intensity) < 0.002f)
				intensity = target;

			if (WBattleVisuals.ConsumeSkyFlash(out float x, out float strength)) {
				flash = Math.Max(flash, strength);
				flashX = x;
			}
			flash *= 0.85f;
			if (flash < 0.01f)
				flash = 0f;
		}

		public override void Draw(SpriteBatch spriteBatch, float minDepth, float maxDepth) {
			// Far：ResetDepthTracker 后第一段区间（含 float.MaxValue）；Near：DrawRemainingDepth 的区间（含 0）
			bool far = maxDepth >= float.MaxValue && minDepth < float.MaxValue;
			bool near = minDepth < 0f && maxDepth >= 0f;
			if (!far && !near)
				return;
			float nuke = WBattleVisuals.NukeGlow;
			if (intensity <= 0.01f && nuke <= 0.01f)
				return;

			// 着色器缺失，或处在地图截图路径（那里的批次参数与正常 DrawBG 不同，不在其中重启批次）→ 像素绘制兜底
			Effect fx = ArknightsMod.WBattleSkyEffect?.Value;
			if (fx == null || CaptureManager.Instance.IsCapturing) {
				if (near)
					DrawFallback(spriteBatch);
				return;
			}

			// DrawBG 当前批次的矩阵由 Main 每帧写在这里（原版 Segments.cs 在背景层里 End/Begin 也用它），
			// 用它重启/恢复批次，缩放 ≠ 1.0 时远山才不会错位
			Matrix bgMatrix = Main.CurrentFrameFlags.Hacks.CurrentBackgroundMatrixForCreditsRoll;
			spriteBatch.End();
			spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp,
				DepthStencilState.None, Main.Rasterizer, null, bgMatrix);

			fx.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly);
			fx.Parameters["uIntensity"]?.SetValue(intensity);
			fx.Parameters["uAspect"]?.SetValue(Main.screenWidth / (float)Math.Max(1, Main.screenHeight));
			fx.Parameters["uRedPulse"]?.SetValue(WBattleVisuals.RedPulse);
			fx.Parameters["uNuke"]?.SetValue(nuke);
			fx.Parameters["uNukeX"]?.SetValue(WBattleVisuals.NukeScreenX);
			fx.Parameters["uScroll"]?.SetValue(Main.screenPosition * 0.0005f);
			fx.Parameters["uFlash"]?.SetValue(flash);
			fx.Parameters["uFlashX"]?.SetValue(flashX);

			// DrawBG 期间 screenWidth/Height 已按背景缩放折算，配合 bgMatrix 正好铺满整屏
			Rectangle screen = new(0, 0, Main.screenWidth, Main.screenHeight);
			Texture2D pixel = TextureAssets.MagicPixel.Value;
			if (far) {
				fx.CurrentTechnique = fx.Techniques["Far"];
				fx.CurrentTechnique.Passes[0].Apply();
				spriteBatch.Draw(pixel, screen, Color.White);
			}
			if (near) {
				fx.CurrentTechnique = fx.Techniques["Near"];
				fx.CurrentTechnique.Passes[0].Apply();
				spriteBatch.Draw(pixel, screen, Color.White);
			}

			spriteBatch.End();
			spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
				DepthStencilState.None, Main.Rasterizer, null, bgMatrix);
		}

		/// <summary>着色器不可用时的像素绘制（暗幕条带 + 方块落灰 + 贴图烟霭 + 红光），只在 Near 层调用</summary>
		private void DrawFallback(SpriteBatch spriteBatch) {
			Texture2D pixel = TextureAssets.MagicPixel.Value;
			Texture2D smoke = WTelegraph.Glow;
			int w = Main.screenWidth;
			int h = Main.screenHeight;

			// 核爆白炽：整层天空先泛白，随后落成血红
			float nuke = WBattleVisuals.NukeGlow;
			if (nuke > 0.01f) {
				float white = MathHelper.Clamp((nuke - 0.5f) * 2f, 0f, 1f);
				float afterglow = MathHelper.Clamp(nuke * 2f, 0f, 1f) * (1f - white);
				if (white > 0f)
					spriteBatch.Draw(pixel, new Rectangle(0, 0, w, h), Color.White * white);
				if (afterglow > 0f)
					spriteBatch.Draw(pixel, new Rectangle(0, 0, w, h), new Color(160, 30, 25) * (0.55f * afterglow));
			}

			if (intensity <= 0.01f)
				return;

			// 暗幕：12 段渐变，顶部最深
			const int bands = 12;
			for (int i = 0; i < bands; i++) {
				float t = i / (float)bands;
				float a = MathHelper.Lerp(0.72f, 0.22f, t) * intensity;
				Color c = new Color(26, 20, 24) * a;
				int y = (int)(h * t);
				int bh = (int)Math.Ceiling(h / (float)bands) + 1;
				spriteBatch.Draw(pixel, new Rectangle(0, y, w, bh), c);
			}

			// 落灰：几十片暗灰/暗红的碎屑斜着往下飘，位置由序号哈希 + 时间推算，不占实体
			int flakes = (int)(46 * intensity);
			for (int i = 0; i < flakes; i++) {
				float seed = i * 12.9898f;
				float speed = 22f + (i % 7) * 6f;
				float x = ((seed * 43758.5453f) % w + Main.GlobalTimeWrappedHourly * (14f + (i % 5) * 4f) * -1f) % (w + 40f);
				if (x < 0f)
					x += w + 40f;
				float y = ((seed * 7.13f) % h + Main.GlobalTimeWrappedHourly * speed) % (h + 20f);
				float size = 2f + (i % 3);
				Color c = (i % 4 == 0 ? new Color(120, 40, 36) : new Color(58, 50, 52)) * (0.55f * intensity);
				spriteBatch.Draw(pixel, new Rectangle((int)x - 20, (int)y - 10, (int)size, (int)size), c);
			}

			// 烟霭：几团大软烟缓慢横移，远处更小更淡
			for (int i = 0; i < 6; i++) {
				float speed = 9f + i * 4f;
				float span = w + 600f;
				float x = ((Main.GlobalTimeWrappedHourly * speed + i * 517f) % span) - 300f;
				float y = h * (0.12f + 0.11f * i) + MathF.Sin(Main.GlobalTimeWrappedHourly * 0.35f + i) * 18f;
				float scale = 7f + (i % 3) * 2.5f;
				Color c = Color.Lerp(WTelegraph.SmokeDark, WTelegraph.SmokeRed, 0.2f + 0.08f * i) * (0.10f * intensity);
				spriteBatch.Draw(smoke, new Vector2(x, y), null, c, i * 0.7f, smoke.Size() * 0.5f, scale, SpriteEffects.None, 0f);
			}

			// 低血量红光：地平线附近一层脉动的红
			float red = WBattleVisuals.RedPulse * intensity;
			if (red > 0.01f) {
				float pulse = 0.55f + 0.45f * MathF.Sin(Main.GlobalTimeWrappedHourly * 2.4f);
				for (int i = 0; i < 6; i++) {
					float t = i / 6f;
					Color c = new Color(150, 30, 30) * (0.22f * (1f - t) * red * pulse);
					int y = (int)(h * (0.55f + 0.45f * t));
					spriteBatch.Draw(pixel, new Rectangle(0, y, w, (int)(h * 0.08f) + 1), c);
				}
			}
		}

		public override float GetCloudAlpha() => 1f - intensity * 0.8f;
	}
}
