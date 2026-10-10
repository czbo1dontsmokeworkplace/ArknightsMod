using System;
using ArknightsMod.Content.Dusts.Fire;
using ArknightsMod.Common.Particle;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Laevatain
{
	/// <summary>黄昏砸地后沿地面依次出现的火柱。</summary>
	public sealed class LaevatainTwilightPillar : ModProjectile
	{
		public const int PillarHeight = 86; // 原 64 像素高度的 135%，取整到像素
		private const float PillarScale = 1.35f;
		private const int ActiveTicks = 18;
		private int age;
		private int Delay => (int)Projectile.ai[0];

		public override string Texture =>
			"ArknightsMod/Content/Projectiles/Guard/Laevatain/LaevatainProjectile_3_fire";

		public override void SetDefaults()
		{
			Projectile.width = 30; // 原 22 像素宽度的 135%，取整到像素
			Projectile.height = PillarHeight;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Melee;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = 80;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
		}

		public override bool ShouldUpdatePosition() => false;

		public override bool? CanDamage() => age >= Delay + 2 && age < Delay + ActiveTicks;

		public override void AI()
		{
			age++;
			if (age <= Delay)
				return;
			if (age >= Delay + ActiveTicks + 10) {
				Projectile.Kill();
				return;
			}
			if (!Main.dedServ) {
				Lighting.AddLight(Projectile.Center, 0.9f, 0.27f, 0.05f);
				int live = age - Delay;
				if (live <= ActiveTicks && Projectile.Center.X > Main.screenPosition.X - 170f &&
					Projectile.Center.X < Main.screenPosition.X + Main.screenWidth + 170f)
					SpawnFlameParticles(live);
			}
		}

		private void SpawnFlameParticles(int live)
		{
			Vector2 bottom = Projectile.Bottom;
			if (live == 1)
			{
				// 每根火柱爆出模组火焰，同时从底部按固定角度向上喷射原版火星。
				for (int i = 0; i < 3; i++)
					Dust.NewDustPerfect(bottom + new Vector2(Main.rand.NextFloat(-10f, 10f), -i * 24f),
						ModContent.DustType<fire_28>(), Vector2.Zero, 0, Color.White, Main.rand.NextFloat(0.38f, 0.6f));
				for (int i = -3; i <= 3; i++)
					SpawnTorch(bottom + new Vector2(i * 4f, -4f),
						new Vector2(i * 0.85f, -Main.rand.NextFloat(4.5f, 6f)), Main.rand.NextFloat(1.15f, 1.8f));
				for (int i = 0; i < 20; i++)
					SpawnEmber(bottom + new Vector2(Main.rand.NextFloat(-25f, 25f), -Main.rand.NextFloat(0f, 15f)),
						new Vector2(Main.rand.NextFloat(-4.5f, 4.5f), Main.rand.NextFloat(-10f, -5f)),
						Main.rand.NextFloat(0.6f, 1.4f));
			}
			if (live % 4 == 0)
			{
				for (int i = -2; i <= 2; i++)
					SpawnTorch(bottom + new Vector2(i * 5f, -Main.rand.NextFloat(0f, 8f)),
						new Vector2(i * 0.7f, -Main.rand.NextFloat(3f, 5f)), Main.rand.NextFloat(0.9f, 1.4f));
			}
			if (live % 3 == 0)
				Dust.NewDustPerfect(bottom + new Vector2(Main.rand.NextFloat(-15f, 15f), -Main.rand.NextFloat(8f, PillarHeight)),
					ModContent.DustType<fire_28>(), Vector2.Zero, 0, Color.White, Main.rand.NextFloat(0.25f, 0.42f));
			// 无规则散射填满柱身，与底部的规律喷射形成两层运动。
			for (int i = 0; i < 3; i++)
				SpawnTorch(bottom + new Vector2(Main.rand.NextFloat(-22f, 22f), -Main.rand.NextFloat(0f, PillarHeight)),
					new Vector2(Main.rand.NextFloat(-2.8f, 2.8f), Main.rand.NextFloat(-4f, -0.7f)),
					Main.rand.NextFloat(0.85f, 1.6f));
			for (int i = 0; i < 4; i++)
				SpawnEmber(bottom + new Vector2(Main.rand.NextFloat(-32f, 32f), -Main.rand.NextFloat(0f, PillarHeight * 1.25f)),
					new Vector2(Main.rand.NextFloat(-2.5f, 2.5f), Main.rand.NextFloat(-6f, -2f)),
					Main.rand.NextFloat(0.45f, 1.05f));
		}

		private static void SpawnEmber(Vector2 position, Vector2 velocity, float scale)
		{
			new DefaultParticle(position, velocity, Main.rand.Next(16, 28), scale,
				Main.rand.NextBool(3) ? new Color(255, 224, 96) : new Color(255, 85, 16), true)
				{ Deformation = new Vector2(0.36f, 2.1f) }.Spawn();
		}

		private static void SpawnTorch(Vector2 position, Vector2 velocity, float scale)
		{
			Dust dust = Dust.NewDustPerfect(position, DustID.Torch, velocity, 65,
				new Color(255, 104, 20), scale);
			dust.noGravity = true;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.OnFire3, 180);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			if (age <= Delay)
				return false;
			float live = age - Delay;
			float grow = MathHelper.Clamp((live + 1f) / 5f, 0f, 1f);
			float fade = MathHelper.Clamp((ActiveTicks + 10f - live) / 10f, 0f, 1f);
			Vector2 bottom = Projectile.Bottom - Main.screenPosition;
			Texture2D flame = TextureAssets.Projectile[Type].Value;
			Main.EntitySpriteDraw(flame, bottom, null, new Color(255, 92, 18) * (fade * 0.5f), 0f,
				new Vector2(flame.Width * 0.5f, flame.Height),
				new Vector2(0.42f * PillarScale * 1.65f, 0.5f * PillarScale * 1.85f * grow), SpriteEffects.None);
			Main.EntitySpriteDraw(flame, bottom, null, Color.White * (fade * 0.9f), 0f,
				new Vector2(flame.Width * 0.5f, flame.Height),
				new Vector2(0.42f * PillarScale, 0.5f * PillarScale * grow), SpriteEffects.None);
			Texture2D pixel = TextureAssets.MagicPixel.Value;
			Rectangle source = new(0, 0, 1, 1);
			float flicker = 0.9f + 0.1f * MathF.Sin(Main.GlobalTimeWrappedHourly * 30f + Projectile.whoAmI);
			DrawLayer(pixel, source, bottom, PillarHeight * grow, 30f * PillarScale, new Color(255, 55, 5) * (fade * 0.45f * flicker));
			DrawLayer(pixel, source, bottom, PillarHeight * grow, 17f * PillarScale, new Color(255, 113, 15) * (fade * 0.8f * flicker));
			DrawLayer(pixel, source, bottom, PillarHeight * grow, 7f * PillarScale, new Color(255, 228, 126) * (fade * 0.95f));
			DrawLayer(pixel, source, bottom, PillarHeight * grow, 2.5f * PillarScale, Color.White * fade);
			return false;
		}

		private static void DrawLayer(Texture2D pixel, Rectangle source, Vector2 bottom, float height, float width, Color color)
		{
			Main.EntitySpriteDraw(pixel, bottom, source, color, 0f,
				new Vector2(0.5f, 1f), new Vector2(width, height), SpriteEffects.None);
		}

		public static Vector2 FindGround(float worldX, float approximateFeetY)
		{
			int tileX = Math.Clamp((int)(worldX / 16f), 1, Main.maxTilesX - 2);
			int startTileY = Math.Clamp((int)(approximateFeetY / 16f), 10, Main.maxTilesY - 20);
			for (int step = -4; step <= 14; step++) {
				int tileY = startTileY + step;
				if (tileY < 1 || tileY >= Main.maxTilesY - 1)
					continue;
				if (WorldGen.SolidTile(tileX, tileY) && !WorldGen.SolidTile(tileX, tileY - 1))
					return new Vector2(worldX, tileY * 16f);
			}
			return new Vector2(worldX, approximateFeetY);
		}
	}
}
