using System;
using System.Collections.Generic;
using ArknightsMod.Common.VisualEffects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace ArknightsMod.Content.NPCs.Enemy.ThroughChapter4
{
	internal static class CrownslayerTrailEffects
	{
		private const float MaxContinuousNpcTrailSegment = 220f;
		private const float MaxContinuousNpcTrailSegmentSq = MaxContinuousNpcTrailSegment * MaxContinuousNpcTrailSegment;

		public static void DrawBossDashTrail(SpriteBatch spriteBatch, NPC npc, Crownslayer.NPCState currentAnimation, Crownslayer.AIState currentAIState)
		{
			if (!ShouldDrawBossRibbonTrail(npc, currentAnimation, currentAIState))
				return;

			Texture2D trailTexture = TextureAssets.MagicPixel.Value;
			List<Vector2> trailPoints = BuildNpcTrailPoints(npc, new Vector2(0f, npc.height * 0.08f));
			if (trailPoints.Count < 3)
				return;

			// 宽层橙边；内层深红。尾部只降 Alpha，不把 RGB  lerping/乘到近黑
			Color rimHead = new Color(255, 132, 42);
			Color rimTail = new Color(255, 132, 42, 0);
			Color bodyHead = new Color(138, 22, 38);
			Color bodyTail = new Color(138, 22, 38, 0);

			BeginNonPremultiplied(spriteBatch);
			// 带宽再收：橙边与深红主体同步变细（路径/长度仍由 oldPos 采样决定）
			DrawRibbonTaperFade(trailPoints, 10.5f, 0.11f, rimHead, rimTail, trailTexture);
			DrawRibbonTaperFade(trailPoints, 6f, 0f, bodyHead, bodyTail, trailTexture);
			EndNonPremultiplied(spriteBatch);
		}

		/// <summary>冲刺路径上的本体残影（先于拖尾与默认绘制）。</summary>
		public static void DrawBossDashAfterimages(SpriteBatch spriteBatch, NPC npc, Color drawColor, Crownslayer.NPCState currentAnimation, Crownslayer.AIState currentAIState)
		{
			if (Main.dedServ || !ShouldDrawBossAfterimages(npc, currentAnimation, currentAIState))
				return;

			if (npc.oldPos == null || npc.oldPos.Length < 2)
				return;

			Texture2D texture = TextureAssets.Npc[npc.type].Value;
			Rectangle frame = npc.frame;
			Vector2 origin = frame.Size() * 0.5f;
			SpriteEffects effects = npc.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

			List<int> validAfterimageIndices = new();
			Vector2 previousCenter = npc.Center;
			for (int k = 0; k < npc.oldPos.Length; k++) {
				if (npc.oldPos[k] == Vector2.Zero)
					break;

				Vector2 oldCenter = npc.oldPos[k] + npc.Size * 0.5f;
				if (Vector2.DistanceSquared(previousCenter, oldCenter) > MaxContinuousNpcTrailSegmentSq)
					break;

				if (k > 0)
					validAfterimageIndices.Add(k);

				previousCenter = oldCenter;
			}

			for (int index = validAfterimageIndices.Count - 1; index >= 0; index--) {
				int k = validAfterimageIndices[index];

				float along = 1f - k / (float)(npc.oldPos.Length - 1);
				float fade = along * along * 0.62f;
				if (fade < 0.04f)
					continue;

				Color tint = Color.Lerp(new Color(200, 72, 88), new Color(120, 18, 32), 1f - along);
				Color c = drawColor.MultiplyRGBA(tint);
				c.A = (byte)MathHelper.Clamp((int)(200f * fade), 0, 200);

				Vector2 pos = npc.oldPos[k] + npc.Size * 0.5f - Main.screenPosition;
				spriteBatch.Draw(texture, pos, frame, c, npc.rotation, origin, npc.scale, effects, 0f);
			}
		}

		public static void DrawBossOrbitLines(SpriteBatch spriteBatch, NPC npc, float intensity)
		{
			Texture2D glowTexture = TextureAssets.Extra[98].Value;
			intensity = MathHelper.Clamp(intensity, 0f, 1f);
			float time = Main.GlobalTimeWrappedHourly;

			BeginAdditive(spriteBatch);

			for (int slot = 0; slot < 3; slot++) {
				float orbitDuration = 1.85f + slot * 0.28f;
				float slotOffset = slot * 0.43f;
				float cycle = (time + slotOffset) / orbitDuration;
				int cycleIndex = (int)Math.Floor(cycle);
				float cycleFrac = cycle - cycleIndex;
				float fade = (float)Math.Sin(MathHelper.Pi * cycleFrac);
				if (fade <= 0.03f)
					continue;

				float seed = slot * 31.17f + cycleIndex * 13.37f;
				float tilt = (seed * 0.618f) % MathHelper.TwoPi;
				float phaseAngle = cycleFrac * MathHelper.TwoPi * (slot % 2 == 0 ? 1f : -1f);
				float majorRadius = 40f + slot * 10f + 4f * (float)Math.Sin(seed * 0.77f);
				float minorRadius = 16f + slot * 5f + 2f * (float)Math.Cos(seed * 1.11f);
				float alpha = fade * fade * (0.62f + intensity * 0.86f);

				List<Vector2> orbitPoints = new();
				int samples = 38;
				float tailSpan = 1.48f;
				for (int i = 0; i < samples; i++) {
					float t = i / (float)(samples - 1);
					float angle = phaseAngle - t * tailSpan;
					Vector2 historyCenter = SampleNpcHistoryCenter(npc, t);
					Vector2 ellipsePoint = new Vector2((float)Math.Cos(angle) * majorRadius, (float)Math.Sin(angle) * minorRadius).RotatedBy(tilt);
					orbitPoints.Add(historyCenter + ellipsePoint + new Vector2(0f, -4f));
				}

				DrawRibbon(orbitPoints, 13f, 0.25f, new Color(255, 132, 44) * (alpha * 0.28f), new Color(255, 132, 44) * 0f);
				DrawRibbon(orbitPoints, 7f, 0.12f, new Color(208, 20, 36) * (alpha * 1.05f), new Color(104, 0, 18) * 0f);

				Vector2 head = orbitPoints[0] - Main.screenPosition;
				Main.EntitySpriteDraw(
					glowTexture,
					head,
					null,
					new Color(255, 158, 56) * (alpha * 0.72f),
					0f,
					glowTexture.Size() / 2f,
					0.3f,
					SpriteEffects.None,
					0f
				);
				Main.EntitySpriteDraw(
					glowTexture,
					head,
					null,
					new Color(220, 28, 40) * (alpha * 0.56f),
					0f,
					glowTexture.Size() / 2f,
					0.16f,
					SpriteEffects.None,
					0f
				);
				Main.EntitySpriteDraw(
					glowTexture,
					head,
					null,
					new Color(255, 236, 226) * (alpha * 0.26f),
					0f,
					glowTexture.Size() / 2f,
					0.08f,
					SpriteEffects.None,
					0f
				);
			}

			DrawBossDiamondSigil(npc, glowTexture, intensity);

			EndAdditive(spriteBatch);
		}

		public static bool DrawGravityDaggerTrail(Projectile projectile)
		{
			if (Main.dedServ)
				return true;

			Texture2D texture = ModContent.Request<Texture2D>("ArknightsMod/Content/NPCs/Enemy/ThroughChapter4/GravityDagger_Barrage").Value;
			Texture2D pixel = TextureAssets.MagicPixel.Value;
			Vector2 drawOrigin = texture.Size() / 2f;
			Vector2 tailLocalOffset = new Vector2(-texture.Width * 0.42f, 0f);

			List<Vector2> spine = BuildProjectileSpine(projectile, tailLocalOffset);
			if (spine.Count >= 3) {
				// 非加法混合 + 顶点 Alpha 淡出：实色条带，避免半透明发灰
				BeginNonPremultiplied(Main.spriteBatch);
				DrawRibbonTaperFade(spine, 5f, 0.12f, new Color(255, 132, 44), new Color(255, 132, 44, 0), pixel);
				DrawRibbonTaperFade(spine, 3.2f, 0f, new Color(172, 22, 40), new Color(172, 22, 40, 0), pixel);
				EndNonPremultiplied(Main.spriteBatch);
			}

			Main.EntitySpriteDraw(
				texture,
				projectile.Center - Main.screenPosition,
				null,
				Color.White * projectile.Opacity,
				projectile.rotation,
				drawOrigin,
				projectile.scale,
				SpriteEffects.None,
				0f
			);

			return false;
		}

		private static bool IsCrownslayerDashRibbonSkill(Crownslayer.AIState currentAIState)
		{
			return currentAIState == Crownslayer.AIState.Skill_1
				|| currentAIState == Crownslayer.AIState.Skill_2
				|| currentAIState == Crownslayer.AIState.Skill_3
				|| currentAIState == Crownslayer.AIState.Skill_5;
		}

		/// <summary>二阶段（≤50% 血，与身边光点环绕同条件）：带状冲刺拖尾。</summary>
		private static bool ShouldDrawBossRibbonTrail(NPC npc, Crownslayer.NPCState currentAnimation, Crownslayer.AIState currentAIState)
		{
			if (npc.alpha > 190 || currentAnimation == Crownslayer.NPCState.Blank)
				return false;

			if (npc.life > npc.lifeMax / 2)
				return false;

			if (!IsCrownslayerDashRibbonSkill(currentAIState))
				return false;

			if (npc.velocity.LengthSquared() < 36f)
				return false;

			return true;
		}

		/// <summary>一/二阶段冲刺位移：残影（一阶段无带状拖尾，用残影+烟雾）。</summary>
		private static bool ShouldDrawBossAfterimages(NPC npc, Crownslayer.NPCState currentAnimation, Crownslayer.AIState currentAIState)
		{
			if (npc.alpha > 190 || currentAnimation == Crownslayer.NPCState.Blank)
				return false;

			if (npc.ModNPC is Crownslayer crownslayer && crownslayer.ShouldSuppressSkill1DashTrailEffects)
				return false;

			if (!IsCrownslayerDashRibbonSkill(currentAIState))
				return false;

			if (npc.velocity.LengthSquared() < 36f)
				return false;

			return true;
		}

		private static List<Vector2> BuildProjectileSpine(Projectile projectile, Vector2 localOffset)
		{
			List<Vector2> points = new();
			points.Add(projectile.Center + localOffset.RotatedBy(projectile.rotation));

			for (int i = 0; i < projectile.oldPos.Length; i++) {
				if (projectile.oldPos[i] == Vector2.Zero)
					break;

				float rotation = projectile.oldRot[i];
				if (rotation == 0f && projectile.velocity.LengthSquared() > 0.01f)
					rotation = projectile.velocity.ToRotation();

				points.Add(projectile.oldPos[i] + projectile.Size * 0.5f + localOffset.RotatedBy(rotation));
			}

			return points;
		}

		private static List<Vector2> BuildNpcTrailPoints(NPC npc, Vector2 localOffset)
		{
			List<Vector2> points = new();
			Vector2 previousCenter = npc.Center;
			points.Add(previousCenter + localOffset);
			for (int i = 0; i < npc.oldPos.Length; i++) {
				if (npc.oldPos[i] == Vector2.Zero)
					break;

				Vector2 oldCenter = npc.oldPos[i] + npc.Size * 0.5f;
				if (Vector2.DistanceSquared(previousCenter, oldCenter) > MaxContinuousNpcTrailSegmentSq)
					break;

				points.Add(oldCenter + localOffset);
				previousCenter = oldCenter;
			}
			return points;
		}

		private static Vector2 SampleNpcHistoryCenter(NPC npc, float factor)
		{
			if (npc.oldPos == null || npc.oldPos.Length == 0)
				return npc.Center;

			float scaled = factor * (npc.oldPos.Length - 1);
			int index = (int)scaled;
			float lerp = scaled - index;

			Vector2 current = index <= 0 || npc.oldPos[index - 1] == Vector2.Zero
				? npc.Center
				: npc.oldPos[index - 1] + npc.Size * 0.5f;
			Vector2 previous = index >= npc.oldPos.Length || npc.oldPos[index] == Vector2.Zero
				? current
				: npc.oldPos[index] + npc.Size * 0.5f;

			return Vector2.Lerp(current, previous, lerp);
		}

		private static void DrawBossDiamondSigil(NPC npc, Texture2D glowTexture, float intensity)
		{
			float time = Main.GlobalTimeWrappedHourly;
			Vector2 center = npc.Center - Main.screenPosition + new Vector2(0f, -2f);
			float baseRotation = time * 0.75f;
			float pulse = 0.88f + 0.12f * (float)Math.Sin(time * 4.2f);
			Color outer = new Color(188, 18, 34) * (0.16f + intensity * 0.18f);
			Color inner = new Color(255, 138, 92) * (0.1f + intensity * 0.14f);

			for (int i = 0; i < 4; i++) {
				float rot = baseRotation + MathHelper.PiOver2 * i;
				Vector2 offset = rot.ToRotationVector2() * 26f;
				Main.EntitySpriteDraw(
					glowTexture,
					center + offset,
					null,
					outer,
					rot + MathHelper.PiOver4,
					glowTexture.Size() / 2f,
					new Vector2(0.22f, 0.08f) * pulse,
					SpriteEffects.None,
					0f
				);
			}

			Main.EntitySpriteDraw(
				glowTexture,
				center,
				null,
				inner,
				baseRotation + MathHelper.PiOver4,
				glowTexture.Size() / 2f,
				new Vector2(0.16f, 0.16f) * pulse,
				SpriteEffects.None,
				0f
			);
		}

		public static void DrawScreenFogOverlay(SpriteBatch spriteBatch)
		{
			if (Main.gameMenu || Main.netMode == NetmodeID.Server)
				return;

			float intensity = GetFogOverlayIntensity();
			if (intensity <= 0.02f)
				return;

			Texture2D pixel = TextureAssets.MagicPixel.Value;
			Texture2D fogTex = TextureAssets.Extra[98].Value;
			float time = (float)Main.timeForVisualEffects * 0.01f;
			Rectangle screen = new(0, 0, Main.screenWidth, Main.screenHeight);

			spriteBatch.Draw(pixel, screen, new Color(96, 82, 36) * (0.06f + intensity * 0.08f));

			for (int i = 0; i < 9; i++) {
				float layer = i / 8f;
				float driftSpeed = 10f + i * 3.5f;
				float x = WrapScreenPosition(Main.screenWidth * (0.08f + layer * 0.11f) + time * driftSpeed * (i % 2 == 0 ? 1f : -1f) * 24f, Main.screenWidth, 520f);
				float y = Main.screenHeight * (0.16f + layer * 0.08f) + (float)Math.Sin(time * (0.45f + i * 0.05f) + i * 0.9f) * (22f + i * 4f);
				float scaleX = 7.5f + i * 1.5f + intensity * 4.5f;
				float scaleY = 3.2f + i * 0.7f + intensity * 2.4f;
				float rotation = (float)Math.Sin(time * 0.35f + i * 0.6f) * 0.08f;
				Color color = Color.Lerp(new Color(122, 104, 48), new Color(208, 182, 104), 0.35f + layer * 0.45f) * (0.045f + intensity * 0.055f);
				spriteBatch.Draw(fogTex, new Vector2(x, y), null, color, rotation, fogTex.Size() / 2f, new Vector2(scaleX, scaleY), SpriteEffects.None, 0f);
			}

			for (int i = 0; i < 5; i++) {
				float x = Main.screenWidth * (0.14f + i * 0.19f) + (float)Math.Sin(time * 0.55f + i * 0.7f) * 36f;
				float y = Main.screenHeight * (0.74f + i * 0.035f) + (float)Math.Cos(time * 0.42f + i) * 16f;
				float scaleX = 12f + i * 2.2f + intensity * 5f;
				float scaleY = 4.4f + i * 0.9f + intensity * 2.1f;
				Color color = new Color(164, 142, 74) * (0.035f + intensity * 0.05f);
				spriteBatch.Draw(fogTex, new Vector2(x, y), null, color, 0.04f * i, fogTex.Size() / 2f, new Vector2(scaleX, scaleY), SpriteEffects.None, 0f);
			}
		}

		private static float GetFogOverlayIntensity()
		{
			float maxIntensity = 0f;
			for (int i = 0; i < Main.maxNPCs; i++) {
				NPC npc = Main.npc[i];
				if (!npc.active || npc.type != ModContent.NPCType<Crownslayer>())
					continue;

				if (npc.ModNPC is Crownslayer crownslayer) {
					maxIntensity = Math.Max(maxIntensity, crownslayer.grayScaleIntensity);
				}
			}

			return MathHelper.Clamp(maxIntensity, 0f, 1f);
		}

		private static float WrapScreenPosition(float position, int screenSize, float padding)
		{
			float span = screenSize + padding * 2f;
			while (position < -padding)
				position += span;
			while (position > screenSize + padding)
				position -= span;
			return position;
		}

		private static void DrawRibbonTaperFade(List<Vector2> points, float headWidth, float tailWidth, Color headColor, Color tailColor, Texture2D texture)
		{
			if (points.Count < 3 || texture == null)
				return;

			List<TrailMaker.CustomVertexInfo> bars = new();
			for (int i = 1; i < points.Count; i++) {
				Vector2 current = points[i];
				Vector2 previous = points[i - 1];
				Vector2 segment = previous - current;
				if (segment.LengthSquared() < 0.001f)
					continue;

				Vector2 normal = Vector2.Normalize(new Vector2(-segment.Y, segment.X));
				float factor = i / (float)(points.Count - 1);
				float width = MathHelper.Lerp(headWidth, tailWidth, factor);
				float fade = 1f - factor;
				fade *= fade;

				Color color = Color.Lerp(headColor, tailColor, factor);
				// 仅用 Alpha 做尾部消失；不再整体压暗 RGB（否则会与贴图暗部相乘形成大块黑色）
				color.A = (byte)MathHelper.Clamp((int)(255f * fade), 0, 255);

				bars.Add(new TrailMaker.CustomVertexInfo(current + normal * width - Main.screenPosition, color, new Vector3(factor, 1f, MathHelper.Lerp(1f, 0.08f, factor))));
				bars.Add(new TrailMaker.CustomVertexInfo(current - normal * width - Main.screenPosition, color, new Vector3(factor, 0f, MathHelper.Lerp(1f, 0.08f, factor))));
			}

			if (bars.Count < 4)
				return;

			List<TrailMaker.CustomVertexInfo> vertices = new();
			for (int i = 0; i < bars.Count - 2; i += 2) {
				vertices.Add(bars[i]);
				vertices.Add(bars[i + 2]);
				vertices.Add(bars[i + 1]);
				vertices.Add(bars[i + 1]);
				vertices.Add(bars[i + 2]);
				vertices.Add(bars[i + 3]);
			}

			Main.graphics.GraphicsDevice.Textures[0] = texture;
			Main.graphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, vertices.ToArray(), 0, vertices.Count / 3);
		}

		private static void DrawRibbon(List<Vector2> points, float headWidth, float tailWidth, Color headColor, Color tailColor, Texture2D texture = null)
		{
			if (points.Count < 3)
				return;

			List<TrailMaker.CustomVertexInfo> bars = new();
			for (int i = 1; i < points.Count; i++) {
				Vector2 current = points[i];
				Vector2 previous = points[i - 1];
				Vector2 segment = previous - current;
				if (segment.LengthSquared() < 0.001f)
					continue;

				Vector2 normal = Vector2.Normalize(new Vector2(-segment.Y, segment.X));
				float factor = i / (float)(points.Count - 1);
				float width = MathHelper.Lerp(headWidth, tailWidth, factor);
				Color color = Color.Lerp(headColor, tailColor, factor);
				color.A = 0;

				bars.Add(new TrailMaker.CustomVertexInfo(current + normal * width - Main.screenPosition, color, new Vector3(factor, 1f, MathHelper.Lerp(1f, 0.1f, factor))));
				bars.Add(new TrailMaker.CustomVertexInfo(current - normal * width - Main.screenPosition, color, new Vector3(factor, 0f, MathHelper.Lerp(1f, 0.1f, factor))));
			}

			if (bars.Count < 4)
				return;

			List<TrailMaker.CustomVertexInfo> vertices = new();
			for (int i = 0; i < bars.Count - 2; i += 2) {
				vertices.Add(bars[i]);
				vertices.Add(bars[i + 2]);
				vertices.Add(bars[i + 1]);
				vertices.Add(bars[i + 1]);
				vertices.Add(bars[i + 2]);
				vertices.Add(bars[i + 3]);
			}

			Main.graphics.GraphicsDevice.Textures[0] = texture ?? ModContent.Request<Texture2D>("ArknightsMod/Common/VisualEffects/LineTrail").Value;
			Main.graphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, vertices.ToArray(), 0, vertices.Count / 3);
		}

		private static void BeginAdditive(SpriteBatch spriteBatch)
		{
			spriteBatch.End();
			spriteBatch.Begin(
				SpriteSortMode.Deferred,
				BlendState.Additive,
				Main.DefaultSamplerState,
				DepthStencilState.None,
				Main.Rasterizer,
				null,
				Main.GameViewMatrix.TransformationMatrix);
		}

		private static void BeginNonPremultiplied(SpriteBatch spriteBatch)
		{
			spriteBatch.End();
			spriteBatch.Begin(
				SpriteSortMode.Deferred,
				BlendState.NonPremultiplied,
				Main.DefaultSamplerState,
				DepthStencilState.None,
				Main.Rasterizer,
				null,
				Main.GameViewMatrix.TransformationMatrix);
		}

		private static void EndAdditive(SpriteBatch spriteBatch)
		{
			spriteBatch.End();
			spriteBatch.Begin(
				SpriteSortMode.Deferred,
				BlendState.AlphaBlend,
				Main.DefaultSamplerState,
				DepthStencilState.None,
				Main.Rasterizer,
				null,
				Main.GameViewMatrix.TransformationMatrix);
		}

		private static void EndNonPremultiplied(SpriteBatch spriteBatch)
		{
			spriteBatch.End();
			spriteBatch.Begin(
				SpriteSortMode.Deferred,
				BlendState.AlphaBlend,
				Main.DefaultSamplerState,
				DepthStencilState.None,
				Main.Rasterizer,
				null,
				Main.GameViewMatrix.TransformationMatrix);
		}
	}

	public class CrownslayerFogOverlaySystem : ModSystem
	{
		public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
		{
			int mouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
			if (mouseTextIndex == -1)
				return;

			layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
				"ArknightsMod:CrownslayerFogOverlay",
				delegate {
					CrownslayerTrailEffects.DrawScreenFogOverlay(Main.spriteBatch);
					return true;
				},
				InterfaceScaleType.UI));
		}
	}
}
