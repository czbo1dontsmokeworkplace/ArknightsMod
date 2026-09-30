using ArknightsMod.Content.Items.Weapons.Medic.ReedFlameShadow;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Medic.ReedFlameShadow
{
	/// <summary>
	/// 三技能「生命火种」：带灼痕的敌人死亡时的 320px 爆炸表现（纯视觉，不参与任何伤害）。
	/// 伤害在 <see cref="ReedFlameShadowScarGlobalNPC.OnKill"/> 里由服务器/单机结算，
	/// 这个弹幕只负责画一圈扩张的火环 + 撒火星。
	///
	/// <para>绘制刻意只用 <c>MagicPixel</c> 走标准 sprite 批次（与其它弹幕同一套视图矩阵）：
	/// 不用 1×1 像素拉成实心方块（会变成方盘，所以内部填充靠多层同心环叠出来），
	/// 也不混用"顶点 + ZoomMatrix"与屏幕空间两种变换——那正是之前"线外一点"的成因。</para>
	/// </summary>
	public class ReedFlameShadowScarBlast : ModProjectile
	{
		private const int Lifetime = 24;
		private const float StartRadius = 36f;

		public override string Texture => ArknightsMod.noTexture;

		public override void SetDefaults()
		{
			Projectile.width = 1;
			Projectile.height = 1;
			Projectile.friendly = false;
			Projectile.hostile = false;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.penetrate = -1;
			Projectile.timeLeft = Lifetime;
			Projectile.netImportant = true;
		}

		/// <summary>由服务器/单机生成（生成后自动同步到各客户端），纯表现、不结算伤害。</summary>
		public static void Spawn(Vector2 center, int owner)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			// 专用服务器上 Main.myPlayer 没有意义，用"施加灼痕的玩家"当归属；拿不到就给无主编号
			if (owner < 0 || owner >= Main.maxPlayers)
				owner = Main.maxPlayers;

			Projectile.NewProjectile(new EntitySource_Misc("ReedFlameShadowScarBlast"), center, Vector2.Zero,
				ModContent.ProjectileType<ReedFlameShadowScarBlast>(), 0, 0f, owner);
		}

		public override bool ShouldUpdatePosition() => false;

		public override void OnSpawn(IEntitySource source)
		{
			if (Main.dedServ)
				return;

			SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.55f, Pitch = 0.15f }, Projectile.Center);
		}

		public override void AI()
		{
			Lighting.AddLight(Projectile.Center, 1.1f, 0.45f, 0.16f);

			if (Main.dedServ)
				return;

			// 环上撒火星：越接近成形越往外
			float progress = Progress;
			float radius = CurrentRadius(progress);
			int count = progress < 0.5f ? 4 : 2;

			for (int i = 0; i < count; i++) {
				Vector2 dir = Main.rand.NextVector2Unit();
				Dust dust = Dust.NewDustPerfect(
					Projectile.Center + dir * radius * Main.rand.NextFloat(0.75f, 1f),
					Main.rand.NextBool(3) ? DustID.GoldFlame : DustID.Torch,
					dir * Main.rand.NextFloat(0.5f, 2.5f), 100, default, Main.rand.NextFloat(0.9f, 1.6f));
				dust.noGravity = true;
			}
		}

		private float Progress => 1f - Projectile.timeLeft / (float)Lifetime;

		private static float Ease(float t) => 1f - MathF.Pow(1f - t, 3f);

		private static float CurrentRadius(float progress) =>
			MathHelper.Lerp(StartRadius, ReedFlameShadowScarGlobalNPC.BlastRadius, Ease(progress));

		public override bool PreDraw(ref Color lightColor)
		{
			if (Main.dedServ)
				return false;

			Texture2D pixel = TextureAssets.MagicPixel.Value;
			if (pixel == null)
				return false;

			float progress = Progress;
			float radius = CurrentRadius(progress);
			float fade = 1f - progress;
			Vector2 center = Projectile.Center - Main.screenPosition;
			Rectangle pixelRect = new(0, 0, 1, 1);

			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState,
				DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

			// 内部柔和填充：多层同心环叠出不透明感
			for (int ring = 1; ring <= 4; ring++) {
				float innerRadius = radius * (ring / 5f);
				float alpha = 0.05f + 0.04f * ring;
				DrawRing(pixel, pixelRect, center, innerRadius, 5f, new Color(255, 96, 32) * (alpha * fade));
			}

			// 明亮火环本体
			DrawRing(pixel, pixelRect, center, radius, 9f, new Color(255, 168, 84) * (0.8f * fade));
			DrawRing(pixel, pixelRect, center, radius * 0.94f, 4f, new Color(255, 236, 200) * (0.9f * fade));

			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
				DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

			return false;
		}

		private static void DrawRing(Texture2D pixel, Rectangle sourceRect, Vector2 center,
			float radius, float thickness, Color color)
		{
			int segments = Math.Max(16, (int)(radius * 0.5f));
			for (int i = 0; i < segments; i++) {
				Vector2 dir = (MathHelper.TwoPi * i / segments).ToRotationVector2();
				Main.spriteBatch.Draw(pixel, center + dir * radius, sourceRect, color,
					0f, new Vector2(0.5f), thickness, SpriteEffects.None, 0f);
			}
		}
	}
}
