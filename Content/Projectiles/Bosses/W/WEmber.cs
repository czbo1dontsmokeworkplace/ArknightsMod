using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Bosses.W
{
	/// <summary>
	/// 留火：D12 掷出 9~11 时在爆点周围落下的一圈余火。落地后原地烧约 1.7 秒，碰到就疼（伤害由生成方给）。<br/>
	/// 无贴图，纯粒子 + 一团加法火光。
	/// </summary>
	public class WEmber : ModProjectile
	{
		public override string Texture => "Terraria/Images/MagicPixel";

		private const int BurnTicks = 100;

		public override void SetDefaults() {
			Projectile.width = 18;
			Projectile.height = 18;
			Projectile.hostile = true;
			Projectile.friendly = false;
			Projectile.tileCollide = true;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 220;
			Projectile.aiStyle = -1;
			Projectile.alpha = 255;
		}

		private bool Landed => Projectile.ai[0] == 1f;
		private float BurnLeft => Landed ? Projectile.ai[1] : BurnTicks;

		// 只有落地后在烧的那段会伤人；最后 10 tick 熄火不再判定
		public override bool CanHitPlayer(Player target) => Landed && Projectile.ai[1] > 10f;

		public override void AI() {
			if (!Landed) {
				Projectile.velocity.Y += 0.3f;
				if (Projectile.velocity.Y > 12f)
					Projectile.velocity.Y = 12f;
				Projectile.velocity.X *= 0.98f;
			}
			else {
				Projectile.velocity = Vector2.Zero;
				Projectile.ai[1]--;
				if (Projectile.ai[1] <= 0f)
					Projectile.Kill();
			}

			if (!Main.dedServ) {
				float k = MathHelper.Clamp(BurnLeft / BurnTicks, 0f, 1f);
				Lighting.AddLight(Projectile.Center, 1.0f * k, 0.45f * k, 0.1f * k);
				int count = Landed ? 2 : 1;
				for (int i = 0; i < count; i++) {
					if (Main.rand.NextFloat() > 0.35f + 0.65f * k)
						continue;
					Dust d = Dust.NewDustPerfect(Projectile.Center + new Vector2(Main.rand.NextFloat(-8f, 8f), Main.rand.NextFloat(-4f, 6f)), DustID.Torch,
						new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), -Main.rand.NextFloat(0.8f, 2.2f)), 0, default, Main.rand.NextFloat(1.0f, 1.6f) * (0.6f + 0.4f * k));
					d.noGravity = true;
				}
				if (Landed && Main.rand.NextBool(6)) {
					Dust s = Dust.NewDustPerfect(Projectile.Center + new Vector2(0f, -6f), DustID.Smoke, new Vector2(0f, -0.8f), 150, default, 0.9f);
					s.noGravity = true;
				}
			}
		}

		public override bool OnTileCollide(Vector2 oldVelocity) {
			if (!Landed) {
				Projectile.ai[0] = 1f;
				Projectile.ai[1] = BurnTicks;
				Projectile.velocity = Vector2.Zero;
				Projectile.netUpdate = true;
			}
			return false;
		}

		public override bool PreDraw(ref Color lightColor) {
			float k = MathHelper.Clamp(BurnLeft / BurnTicks, 0f, 1f);
			float flicker = 0.8f + 0.2f * MathF.Sin(Main.GlobalTimeWrappedHourly * 23f + Projectile.whoAmI);
			Texture2D glow = NPCs.Enemy.W.WTelegraph.Glow;
			Vector2 pos = Projectile.Center - Main.screenPosition;
			Main.EntitySpriteDraw(glow, pos, null, new Color(255, 120, 40, 0) * (0.55f * k * flicker), 0f, glow.Size() * 0.5f, 0.45f + 0.15f * k, SpriteEffects.None, 0);
			Main.EntitySpriteDraw(glow, pos, null, new Color(255, 220, 150, 0) * (0.5f * k * flicker), 0f, glow.Size() * 0.5f, 0.2f, SpriteEffects.None, 0);
			return false;
		}
	}
}
