using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;

namespace ArknightsMod.Content.Projectiles.Sniper.Rosmontis.WeaponInPlayer
{
	public class RosmontisWeaponRender
	{
		private readonly Texture2D texture;
		private readonly Texture2D glowTexture;
		private Vector2 position;
		private float rotation;
		private float alpha = 1f;
		private SpriteEffects spriteEffects;
		private BasicEffect primitiveEffect;
		private DynamicVertexBuffer primitiveVertexBuffer;

		public List<Vector2> TrailPositions = new List<Vector2>();
		// PrimitiveTrailPositions[0] 为拖尾头部，最后一个元素为拖尾尾部。
		public Vector2[] PrimitiveTrailPositions = Array.Empty<Vector2>();
		public (float Start, float End) PrimitiveTrailU = (0f, 1f);
		public float PrimitiveTrailWidth = 8f;
		public Texture2D Texture => texture;
		public Texture2D GlowTexture => glowTexture;


		public ref Vector2 Position => ref position;
		public ref float Rotation => ref rotation;
		public ref float Alpha => ref alpha;
		public ref SpriteEffects SpriteEffects => ref spriteEffects;

		private Vector2 StarLineOffest {
			get {
				if (this.SpriteEffects == SpriteEffects.FlipHorizontally) {
					return Vector2.Zero;
				}
				else {
					return new Vector2(0, 0);
				}

				return Vector2.Zero;
			}
		}

		public RosmontisWeaponRender(Texture2D texture, Texture2D glowTexture)
		{
			this.texture = texture;
			this.glowTexture = glowTexture;

		}

		public void Draw()
		{
			if (texture == null)
				return;

			float drawAlpha = MathHelper.Clamp(alpha, 0f, 1f);
			float lightMul = MathF.Sin((float)Main.timeForVisualEffects * 0.02f) * 0.5f + 0.5f;
			Lighting.AddLight(position, (Color.Cyan * lightMul).ToVector3() * 0.15f);

			TrailDraw();


			Vector2 drawPosition = position - Main.screenPosition;
			Vector2 origin = texture.Size() * 0.5f;
			Color lightColor = Lighting.GetColor(position.ToTileCoordinates()) * drawAlpha;

			Main.spriteBatch.Draw(texture, drawPosition, null, lightColor, rotation, origin, 1f, spriteEffects, 0f);

			if (glowTexture != null)
			{
				Main.spriteBatch.Draw(
					glowTexture,
					drawPosition,
					null,
					new Color(Color.Cyan.R, Color.Cyan.G, Color.Cyan.B, 0) * lightMul * drawAlpha,
					rotation,
					origin,
					1f,
					spriteEffects,
					0f);
			}

			DrawStarLine();
		}

		public void DrawPrimitive()
		{
			if (PrimitiveTrailPositions == null || PrimitiveTrailPositions.Length < 2)
				return;

			GraphicsDevice device = Main.graphics.GraphicsDevice;
			int pointCount = PrimitiveTrailPositions.Length;
			VertexPositionColorTexture[] vertices = new VertexPositionColorTexture[pointCount * 2];
			float width = MathF.Max(0f, PrimitiveTrailWidth);

			for (int i = 0; i < pointCount; i++) {
				Vector2 current = PrimitiveTrailPositions[i];
				Vector2 direction;
				if (i == 0)
					direction = current - PrimitiveTrailPositions[1];
				else if (i == pointCount - 1)
					direction = PrimitiveTrailPositions[i - 1] - current;
				else
					direction = PrimitiveTrailPositions[i - 1] - PrimitiveTrailPositions[i + 1];

				Vector2 normal = direction.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
				float value = pointCount == 1 ? 0f : i / (float)(pointCount - 1);
				float u = MathHelper.Lerp(PrimitiveTrailU.Start, PrimitiveTrailU.End, value);
				Color color = Color.White * MathHelper.Clamp(alpha, 0f, 1f);

				vertices[i * 2] = new VertexPositionColorTexture(
					new Vector3(current - normal * width * 0.5f - Main.screenPosition, 0),
					color, new Vector2(u, 0f));
				vertices[i * 2 + 1] = new VertexPositionColorTexture(
					new Vector3(current + normal * width * 0.5f - Main.screenPosition, 0),
					color, new Vector2(u, 1f));
			}

			primitiveVertexBuffer ??= new DynamicVertexBuffer(
				device,
				typeof(VertexPositionColorTexture),
				vertices.Length,
				BufferUsage.WriteOnly);
			if (primitiveVertexBuffer.VertexCount < vertices.Length) {
				primitiveVertexBuffer.Dispose();
				primitiveVertexBuffer = new DynamicVertexBuffer(
					device,
					typeof(VertexPositionColorTexture),
					vertices.Length,
					BufferUsage.WriteOnly);
			}
			primitiveVertexBuffer.SetData(
				vertices,
				0,
				vertices.Length,
				SetDataOptions.Discard);
			device.SetVertexBuffer(primitiveVertexBuffer);

			primitiveEffect ??= new BasicEffect(device) {
				TextureEnabled = true,
				VertexColorEnabled = true,
				World = Matrix.Identity,
				View = Matrix.Identity,
				Projection = Matrix.CreateOrthographicOffCenter(
					0f, Main.screenWidth, Main.screenHeight, 0f, 0f, 1f)
			};
			primitiveEffect.Texture = texture;

			// Draw() 通常由 SpriteBatch 调用，因此必须暂时结束当前批次，
			// 否则直接提交 GraphicsDevice primitive 会与 SpriteBatch 状态冲突。
			Main.spriteBatch.End();
			try {
				foreach (EffectPass pass in primitiveEffect.CurrentTechnique.Passes) {
					pass.Apply();
					device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, vertices.Length - 2);
				}
			}
			finally {
				Main.spriteBatch.Begin(
					SpriteSortMode.Deferred,
					BlendState.AlphaBlend,
					Main.DefaultSamplerState,
					DepthStencilState.None,
					RasterizerState.CullNone,
					null,
					Main.GameViewMatrix.TransformationMatrix);
			}
		}

		private void TrailDraw() {

			if (TrailPositions == null || TrailPositions.Count == 0)
				return;

			Vector2 origin = texture.Size() * 0.5f;
			float drawAlpha = MathHelper.Clamp(alpha, 0f, 1f);
			int count = TrailPositions.Count;

			for (int i = 0; i < count; i++)
			{
				float progress =1f- ((float)i) / count;
				float trailAlpha = progress * drawAlpha * 0.5f;
				Vector2 trailPosition = TrailPositions[i];
				Color trailColor =
					Lighting.GetColor(trailPosition.ToTileCoordinates())
					* trailAlpha;

				Main.spriteBatch.Draw(
					texture,
					trailPosition - Main.screenPosition,
					null,
					trailColor,
					rotation,
					origin,
					1f,
					spriteEffects,
					0f);
			}
		}

		private void DrawStarLine() {

		}
	}
}
