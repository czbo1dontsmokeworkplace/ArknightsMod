using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.Valarqvin
{
	public class ValarqvinProj_2 : ModProjectile
	{
	
		
		public static int TrailLength = 15;

	
		public static float TrailWidthStart = 16f;
		public static float TrailWidthEnd = 6f;


		public static float Trail2WidthStart = 18f;
		public static float Trail2WidthEnd = 7f;

	
		public static float Trail2FlowSpeed = 1.3f;
		public static float TrailFlowSpeed = 3f; 

	
		public static float Trail2FadePower = 2f;
		public static float TrailFadePower = 1.2f;

		public static float ProjectileSize = 1.4f;


		public static float Brightness = 2.3f;
		public static float Trail2Brightness = 1.7f; 

		public static float Velocity = 35f;


		public static Color Trail2ColorHead = new Color(217, 252, 255);
		public static Color Trail2ColorTail = new Color(86, 127, 251);

		public static Color TrailColorHead = new Color(68, 90, 172);
		public static Color TrailColorTail = new Color(73, 113, 223);




		private Queue<Vector2> trailPositions = new Queue<Vector2>();


		private Texture2D projectileTexture;
		private Texture2D gradientTexture;
		private Texture2D gradientTexture2;
		private Vector2 textureSize;

		public override void SetDefaults() {
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.aiStyle = -1;
			Projectile.friendly = true;
			Projectile.hostile = false;
			Projectile.DamageType = DamageClass.Magic;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 300;
			Projectile.ignoreWater = true;
			Projectile.tileCollide = true;
			Projectile.extraUpdates = 0;

	
			Projectile.velocity = new Vector2(Velocity, 0f);


			projectileTexture = ModContent.Request<Texture2D>("ArknightsMod/Content/Projectiles/Caster/Valarqvin/ValarqvinProj").Value;
			gradientTexture = ModContent.Request<Texture2D>("ArknightsMod/Content/Projectiles/Caster/Valarqvin/Effect/LightningGradient_2").Value;
			gradientTexture2 = ModContent.Request<Texture2D>("ArknightsMod/Content/Projectiles/Caster/Valarqvin/Effect/LightningGradient_3").Value;

			if (projectileTexture != null && !projectileTexture.IsDisposed) {
				textureSize = new Vector2(projectileTexture.Width, projectileTexture.Height);
			}
		}

		public override void AI() {
	
			Projectile.rotation = Projectile.velocity.ToRotation();


			UpdateTrail();
		}

		private void UpdateTrail() {
			trailPositions.Enqueue(Projectile.Center);
			while (trailPositions.Count > TrailLength)
				trailPositions.Dequeue();
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) { }
		public override void OnKill(int timeLeft) { }

		public override bool PreDraw(Player player, ref Color lightColor)/* tModPorter Replace 'Main.player[Projectile.owner]' with 'player'. */ {
			return false;
		}

		public override void PostDraw(Player player, Color lightColor)/* tModPorter Replace 'Main.player[Projectile.owner]' with 'player'. */ {
			DrawTrail();      
			DrawTrail2();      
			DrawProjectile();   
		}

		// ---------- 弹幕本体绘制 ----------
		private void DrawProjectile() {
			if (projectileTexture == null || projectileTexture.IsDisposed)
				return;

			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
				SamplerState.LinearClamp, DepthStencilState.None,
				RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

			DrawProjectileVertices(Main.graphics.GraphicsDevice, projectileTexture);

			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
				Main.DefaultSamplerState, DepthStencilState.None,
				Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
		}

		private void DrawProjectileVertices(GraphicsDevice gd, Texture2D texture) {
			float alphaFactor = (float)Projectile.timeLeft / 300f;
			Color drawColor = TrailColorHead * alphaFactor * Brightness;

			float textureAspect = textureSize.X / textureSize.Y;
			float baseSize = Projectile.width * Projectile.scale * ProjectileSize;
			float width = baseSize * textureAspect;
			float height = baseSize;

			Vector2 center = Projectile.Center - Main.screenPosition;
			float rot = Projectile.rotation;

			Vector2 offset = new Vector2(width / 2f - 2f, 0).RotatedBy(rot);
			Vector2 adjustedCenter = center - offset;

			Vector2 topLeft = adjustedCenter + new Vector2(-width / 2f, -height / 2f).RotatedBy(rot);
			Vector2 topRight = adjustedCenter + new Vector2(width / 2f, -height / 2f).RotatedBy(rot);
			Vector2 bottomLeft = adjustedCenter + new Vector2(-width / 2f, height / 2f).RotatedBy(rot);
			Vector2 bottomRight = adjustedCenter + new Vector2(width / 2f, height / 2f).RotatedBy(rot);

			VertexData[] vertices = new VertexData[]
			{
				new VertexData(topLeft, new Vector3(0, 0, 1), drawColor),
				new VertexData(topRight, new Vector3(1, 0, 1), drawColor),
				new VertexData(bottomLeft, new Vector3(0, 1, 1), drawColor),
				new VertexData(bottomRight, new Vector3(1, 1, 1), drawColor)
			};

			gd.Textures[0] = texture;
			gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertices, 0, 2);
		}

		// ---------- 第一层拖尾绘制 ----------
		private void DrawTrail() {
			if (trailPositions.Count < 2 || gradientTexture == null || gradientTexture.IsDisposed)
				return;

			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
				SamplerState.LinearWrap, DepthStencilState.None,
				RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

			DrawTrailVertices(Main.graphics.GraphicsDevice, gradientTexture,
				TrailWidthStart, TrailWidthEnd, TrailFlowSpeed, TrailFadePower,
				TrailColorHead, TrailColorTail, Brightness);

			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
				Main.DefaultSamplerState, DepthStencilState.None,
				Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
		}

		// ---------- 第二层拖尾绘制 ----------
		private void DrawTrail2() {
			if (trailPositions.Count < 2 || gradientTexture2 == null || gradientTexture2.IsDisposed)
				return;

			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
				SamplerState.LinearWrap, DepthStencilState.None,
				RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

			DrawTrailVertices(Main.graphics.GraphicsDevice, gradientTexture2,
				Trail2WidthStart, Trail2WidthEnd, Trail2FlowSpeed, Trail2FadePower,
				Trail2ColorHead, Trail2ColorTail, Trail2Brightness);

			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
				Main.DefaultSamplerState, DepthStencilState.None,
				Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
		}

		private void DrawTrailVertices(GraphicsDevice gd, Texture2D texture,
			float widthStart, float widthEnd, float flowSpeed, float fadePower,
			Color colorHead, Color colorTail, float brightness) {

			Vector2[] points = trailPositions.ToArray();
			int count = points.Length;

			float[] cumLength = new float[count];
			cumLength[0] = 0f;
			float totalLength = 0f;
			for (int i = 1; i < count; i++) {
				cumLength[i] = cumLength[i - 1] + Vector2.Distance(points[i], points[i - 1]);
				totalLength = cumLength[i];
			}

			float timeOffset = (float)Main.timeForVisualEffects * 0.02f * flowSpeed;
			timeOffset -= (float)Math.Floor(timeOffset);

			List<VertexData> vertices = new List<VertexData>();

			for (int i = 0; i < count; i++) {
				Vector2 dir;
				if (i == 0)
					dir = points[i + 1] - points[i];
				else if (i == count - 1)
					dir = points[i] - points[i - 1];
				else
					dir = points[i + 1] - points[i - 1];

				if (dir.LengthSquared() < 0.001f)
					dir = Vector2.UnitX;
				else
					dir.Normalize();

				Vector2 perp = new Vector2(-dir.Y, dir.X);
				float t = (float)i / (count - 1); // t=0 尾部，t=1 头部

				// 颜色渐变
				Color gradientColor = Color.Lerp(colorTail, colorHead, t);

				// 使用更高的 fadePower 让渐变更陡
				float alpha = (float)Math.Pow(t, fadePower);
				alpha = MathHelper.Clamp(alpha, 0.05f, 1f);

				float width = MathHelper.Lerp(widthEnd, widthStart, t);

				Vector2 left = points[i] - perp * width;
				Vector2 right = points[i] + perp * width;

				float v = totalLength > 0 ? cumLength[i] / totalLength : 0f;
				v = (v + timeOffset) % 1f;

				Vector2 leftScreen = left - Main.screenPosition;
				Vector2 rightScreen = right - Main.screenPosition;

				Color trailColor = gradientColor * alpha * brightness;

				vertices.Add(new VertexData(leftScreen, new Vector3(0, v, 1), trailColor));
				vertices.Add(new VertexData(rightScreen, new Vector3(1, v, 1), trailColor));
			}

			if (vertices.Count >= 4) {
				gd.Textures[0] = texture;
				gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertices.ToArray(), 0, vertices.Count - 2);
			}
		}
	}
}