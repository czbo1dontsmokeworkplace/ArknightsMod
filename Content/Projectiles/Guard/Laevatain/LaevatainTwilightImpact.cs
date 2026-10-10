using System;
using ArknightsMod.Common.Particle;
using ArknightsMod.Content.Dusts.Fire;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Laevatain
{
	/// <summary>傀儡砸地的第一段爆炸；火柱仍负责实际伤害。</summary>
	public sealed class LaevatainTwilightImpact : ModProjectile
	{
		private const int Lifetime = 16;
		private int age;

		public override string Texture => "ArknightsMod/Content/Textures/PassengerImpactBurst";

		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.friendly = false;
			Projectile.hostile = false;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = Lifetime;
		}

		public override bool ShouldUpdatePosition() => false;

		public override void OnSpawn(IEntitySource source)
		{
			if (Main.dedServ)
				return;

			Vector2 center = Projectile.Center;
			for (int i = 0; i < 9; i++)
			{
				Vector2 offset = Main.rand.NextVector2Circular(65f, 30f);
				Dust.NewDustPerfect(center + offset, ModContent.DustType<fire_28>(),
					Vector2.Zero, 0, Color.White, Main.rand.NextFloat(0.75f, 1.35f));
			}
			for (int i = 0; i < 72; i++)
			{
				float angle = MathHelper.TwoPi * i / 72f + Main.rand.NextFloat(-0.12f, 0.12f);
				Vector2 direction = angle.ToRotationVector2();
				Vector2 velocity = new(direction.X * Main.rand.NextFloat(4f, 12f),
					direction.Y * Main.rand.NextFloat(3f, 9f) - 2f);
				Dust spark = Dust.NewDustPerfect(center + Main.rand.NextVector2Circular(8f, 8f),
					DustID.Torch, velocity, 60, new Color(255, 143, 27), Main.rand.NextFloat(1.35f, 2.35f));
				spark.noGravity = true;
				if (i % 2 == 0)
					new DefaultParticle(center, velocity * Main.rand.NextFloat(0.8f, 1.3f),
						Main.rand.Next(20, 32), Main.rand.NextFloat(0.7f, 1.5f),
						i % 4 == 0 ? new Color(255, 228, 92) : new Color(255, 68, 12), true)
						{ Deformation = new Vector2(0.38f, 2.4f) }.Spawn();
			}
		}

		public override void AI()
		{
			age++;
			if (!Main.dedServ)
				Lighting.AddLight(Projectile.Center, new Vector3(1.5f, 0.75f, 0.18f) *
					(1f - age / (float)Lifetime));
		}

		public override bool PreDraw(ref Color lightColor)
		{
			float progress = MathHelper.Clamp(age / (float)Lifetime, 0f, 1f);
			float flash = MathF.Pow(MathHelper.Clamp(1f - progress * 2.1f, 0f, 1f), 1.2f);
			float body = MathF.Pow(1f - progress, 1.35f);
			float expansion = 1f - MathF.Pow(1f - progress, 3f);
			Vector2 center = Projectile.Center - Main.screenPosition;
			Texture2D burst = TextureAssets.Projectile[Type].Value;
			Texture2D flare = ModContent.Request<Texture2D>("ArknightsMod/Content/Textures/PassengerImpactFlash").Value;
			Texture2D ring = ModContent.Request<Texture2D>("ArknightsMod/Content/Textures/PassengerImpactRing").Value;

			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState,
				DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
			Draw(burst, center, new Vector2(425f, 295f) * (0.8f + expansion * 0.3f),
				new Color(255, 80, 8) * (body * 0.85f));
			Draw(burst, center, new Vector2(280f, 235f) * (0.75f + expansion * 0.15f),
				new Color(255, 210, 44) * (body * 0.9f), MathHelper.PiOver2);
			Draw(flare, center, new Vector2(315f, 225f), Color.White * (flash * 0.95f));
			Draw(ring, center + new Vector2(0f, 20f), new Vector2(MathHelper.Lerp(60f, 460f, expansion),
				MathHelper.Lerp(30f, 145f, expansion)), new Color(255, 134, 22) * (body * 0.8f));
			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
				DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
			return false;
		}

		private static void Draw(Texture2D texture, Vector2 center, Vector2 size, Color tint, float rotation = 0f)
		{
			Main.EntitySpriteDraw(texture, center, null, tint, rotation, texture.Size() * 0.5f,
				size / texture.Size(), SpriteEffects.None);
		}
	}
}
