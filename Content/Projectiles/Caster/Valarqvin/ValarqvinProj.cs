using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.Valarqvin
{
	public class ValarqvinProj : ModProjectile
	{

		public static int TrailLength = 9;

	
		public static float TrailWidthStart = 7f;
		public static float TrailWidthEnd = 3f;


		public static float TrailFlowSpeed = 1.5f;


		public static float TrailFadePower = 0.7f;


		public static float ProjectileSize = 0.8f;


		public static float Brightness = 1.7f;


		public static float Velocity = 35f;


		public static Color TrailColorHead = new Color(136, 164, 229);

		public static Color TrailColorTail = new Color(42, 55, 86);

		


		private Queue<Vector2> trailPositions = new Queue<Vector2>();


		private Texture2D projectileTexture;
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

	


		public override void OnKill(int timeLeft) {

		}


		public override bool PreDraw(Player player, ref Color lightColor)/* tModPorter Replace 'Main.player[Projectile.owner]' with 'player'. */ {
			return false;
		}


		public override void PostDraw(Player player, Color lightColor)/* tModPorter Replace 'Main.player[Projectile.owner]' with 'player'. */ {
			DrawTrail();
			DrawProjectile();
		}


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


		private void DrawTrail() {
			if (trailPositions.Count < 2)
				return;

			Texture2D gradientTexture = ModContent.Request<Texture2D>("ArknightsMod/Content/Projectiles/Caster/Valarqvin/Effect/LightningGradient").Value;
			if (gradientTexture == null || gradientTexture.IsDisposed)
				return;

			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
				SamplerState.LinearWrap, DepthStencilState.None,
				RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

			DrawTrailVertices(Main.graphics.GraphicsDevice, gradientTexture);

			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
				Main.DefaultSamplerState, DepthStencilState.None,
				Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
		}

		private void DrawTrailVertices(GraphicsDevice gd, Texture2D texture) {
			Vector2[] points = trailPositions.ToArray();
			int count = points.Length;

			float[] cumLength = new float[count];
			cumLength[0] = 0f;
			float totalLength = 0f;
			for (int i = 1; i < count; i++) {
				cumLength[i] = cumLength[i - 1] + Vector2.Distance(points[i], points[i - 1]);
				totalLength = cumLength[i];
			}


			float timeOffset = (float)Main.timeForVisualEffects * 0.02f * TrailFlowSpeed;
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

				Color gradientColor = Color.Lerp(TrailColorTail, TrailColorHead, t);

				float alpha = (float)Math.Pow(t, TrailFadePower);
				alpha = MathHelper.Clamp(alpha, 0.1f, 1f);

				float width = MathHelper.Lerp(TrailWidthEnd, TrailWidthStart, t);

				Vector2 left = points[i] - perp * width;
				Vector2 right = points[i] + perp * width;

		
				float v = totalLength > 0 ? cumLength[i] / totalLength : 0f;
				v = (v + timeOffset) % 1f;

				Vector2 leftScreen = left - Main.screenPosition;
				Vector2 rightScreen = right - Main.screenPosition;

		
				Color trailColor = gradientColor * alpha * Brightness;

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