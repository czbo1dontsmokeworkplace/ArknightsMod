using ArknightsMod.Common.Particle;
using ArknightsMod.Content.NPCs.Enemy.W;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Bosses.W
{
	/// <summary>
	/// 核骰「W 的大礼」：一颗黑红色的大骰子，抛向锁定点（ai[1]/ai[2]），一路拖黑红烟、蜂鸣加速、数字狂转。<br/>
	/// 落地或飞到点即起爆：300px 真爆（300% + 混乱）、640px 屏幕级冲击波、蘑菇云、8 颗落尘子弹、一圈余火、<br/>
	/// 满屏白炽慢衰减、震屏 24、天空白→红。锁定圈随骰子一起画：圈外安全，这是唯一的躲法。
	/// </summary>
	public class WNukeDie : ModProjectile
	{
		public override string Texture => "ArknightsMod/Content/Projectiles/Bosses/W/WD12";

		public const float BlastRadius = 300f;
		private const float Gravity = 0.2f;

		private Vector2 TargetPoint => new(Projectile.ai[1], Projectile.ai[2]);
		private int shownNumber = 12, shownTimer;

		public override void SetDefaults() {
			Projectile.width = 22;
			Projectile.height = 22;
			Projectile.hostile = true;
			Projectile.friendly = false;
			Projectile.tileCollide = false; // 穿地形飞到锁定点再炸：圈画在哪就炸在哪，圈不许说谎
			Projectile.penetrate = -1;
			Projectile.timeLeft = 110;
			Projectile.aiStyle = -1;
		}

		public override bool CanHitPlayer(Player target) => false;

		public override void AI() {
			Projectile.velocity.Y += Gravity;
			if (Projectile.velocity.Y > 16f)
				Projectile.velocity.Y = 16f;
			Projectile.rotation += 0.18f * Math.Sign(Projectile.velocity.X == 0f ? 1f : Projectile.velocity.X);
			Projectile.localAI[0]++;

			if (++shownTimer >= 3) {
				shownTimer = 0;
				shownNumber = Main.rand.Next(1, 13);
			}

			if (!Main.dedServ) {
				Lighting.AddLight(Projectile.Center, 1.1f, 0.25f, 0.15f);
				// 黑红烟尾 + 火星
				for (int i = 0; i < 2; i++) {
					Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(6f, 6f), DustID.Smoke, -Projectile.velocity * 0.15f + new Vector2(0f, -0.6f), 90, default, Main.rand.NextFloat(1.3f, 1.9f));
					d.color = Color.Lerp(new Color(30, 20, 24), new Color(170, 40, 40), Main.rand.NextFloat());
					d.noGravity = true;
				}
				if (Main.rand.NextBool(2)) {
					var p = new DefaultParticle(Projectile.Center, -Projectile.velocity * 0.2f + Main.rand.NextVector2Circular(1f, 1f), Main.rand.Next(14, 22), Main.rand.NextFloat(0.8f, 1.2f), new Color(255, 90, 60), false) {
						Deformation = new Vector2(0.12f, 0.6f),
					};
					p.Spawn();
				}
				// 蜂鸣越飞越急
				int interval = (int)MathHelper.Clamp(10f - Projectile.localAI[0] / 8f, 3f, 10f);
				if ((int)Projectile.localAI[0] % interval == 0)
					SoundEngine.PlaySound(SoundID.MaxMana with { Pitch = -0.3f + Projectile.localAI[0] / 70f, Volume = 0.6f }, Projectile.Center);
			}

			// 下落到锁定点高度 → 炸（寿命耗尽是兜底）
			if (Main.netMode != NetmodeID.MultiplayerClient && Projectile.velocity.Y > 0f && Projectile.Center.Y >= TargetPoint.Y - 8f)
				Projectile.Kill();
		}

		public override void OnKill(int timeLeft) {
			Vector2 c = Projectile.Center;
			// ---- 两端都播：白炽、震屏、声音、天空 ----
			WBattleVisuals.Flash(1f, new Color(255, 245, 230), 0.02f);
			WBattleVisuals.NukeSky(c);
			WBoss.ShakeNearby(c, 24, 2000f);
			SoundEngine.PlaySound(SoundID.Item14 with { Pitch = -0.7f, Volume = 1.2f }, c);
			SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode with { Pitch = -0.4f, Volume = 1.2f }, c);
			SoundEngine.PlaySound(SoundID.Roar with { Pitch = -0.6f, Volume = 0.8f }, c);
			if (!Main.dedServ) {
				// 放射火星与碎屑
				for (int i = 0; i < 60; i++) {
					Vector2 dir = Main.rand.NextVector2Unit();
					float speed = Main.rand.NextFloat(6f, 16f);
					var p = new DefaultParticle(c + dir * 10f, dir * speed, Main.rand.Next(30, 60), Main.rand.NextFloat(1.4f, 2.4f), i % 3 == 0 ? new Color(255, 240, 210) : new Color(255, 150, 70), false) {
						Deformation = new Vector2(0.1f, 1.0f),
					};
					p.Spawn();
				}
				for (int i = 0; i < 40; i++) {
					Dust d = Dust.NewDustPerfect(c + Main.rand.NextVector2Circular(60f, 40f), DustID.Smoke, Main.rand.NextVector2Circular(6f, 6f) + new Vector2(0f, -3f), 60, default, Main.rand.NextFloat(1.8f, 2.6f));
					d.color = new Color(60, 45, 45);
					d.noGravity = true;
				}
			}

			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			// ---- 服务端：真爆 + 屏幕级冲击波 + 蘑菇云 + 落尘 + 余火 ----
			Projectile.NewProjectile(Projectile.GetSource_FromThis(), c, Vector2.Zero,
				ModContent.ProjectileType<WExplosion>(), Projectile.damage, 10f, Main.myPlayer,
				ai0: BlastRadius, ai1: 300f, ai2: 3f + 10f * 40f);
			Projectile.NewProjectile(Projectile.GetSource_FromThis(), c, Vector2.Zero,
				ModContent.ProjectileType<WExplosion>(), 1, 0f, Main.myPlayer,
				ai0: 640f, ai1: -1f, ai2: 3f + 10f * 64f);
			Projectile.NewProjectile(Projectile.GetSource_FromThis(), c, Vector2.Zero,
				ModContent.ProjectileType<WNukeCloud>(), 0, 0f, Main.myPlayer);

			int bombletDamage = Math.Max(1, (int)(Projectile.damage * 0.35f));
			for (int i = 0; i < 8; i++) {
				float angle = MathHelper.Lerp(-MathHelper.Pi * 0.9f, -MathHelper.Pi * 0.1f, (i + 0.5f) / 8f);
				Vector2 vel = angle.ToRotationVector2() * Main.rand.NextFloat(8f, 11f) + new Vector2(0f, -4f);
				Projectile.NewProjectile(Projectile.GetSource_FromThis(), c, vel,
					ModContent.ProjectileType<WHEGrenade>(), bombletDamage, 1f, Main.myPlayer, ai0: 2f);
			}
			int emberDamage = Math.Max(1, (int)(Projectile.damage * 0.3f));
			for (int i = 0; i < 10; i++) {
				float t = (i + 0.5f) / 10f * 2f - 1f;
				Vector2 vel = new(t * 7.5f, -4f - (1f - Math.Abs(t)) * 2f);
				Projectile.NewProjectile(Projectile.GetSource_FromThis(), c, vel,
					ModContent.ProjectileType<WEmber>(), emberDamage, 0f, Main.myPlayer);
			}
		}

		public override bool PreDraw(ref Color lightColor) {
			Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
			Vector2 pos = Projectile.Center - Main.screenPosition;
			Vector2 origin = tex.Size() * 0.5f;
			// 锁定圈：圈外安全
			WTelegraph.BeginAdditive(Main.spriteBatch);
			WTelegraph.DrawTargetRing(Main.spriteBatch, TargetPoint, BlastRadius, 0.9f + 0.1f * MathF.Sin(Main.GlobalTimeWrappedHourly * 20f));
			Main.EntitySpriteDraw(WTelegraph.Glow, pos, null, new Color(255, 60, 40, 0) * 0.8f, 0f, WTelegraph.Glow.Size() * 0.5f, 0.9f, SpriteEffects.None, 0);
			WTelegraph.EndAdditive(Main.spriteBatch);
			// 黑红骰子本体：放大、压暗、红边
			Main.EntitySpriteDraw(tex, pos, null, lightColor.MultiplyRGB(new Color(90, 60, 70)), Projectile.rotation, origin, 1.6f, SpriteEffects.None, 0);
			Main.EntitySpriteDraw(tex, pos, null, new Color(255, 50, 40, 0) * 0.5f, Projectile.rotation, origin, 1.7f, SpriteEffects.None, 0);
			WTelegraph.DrawDieFace(Main.spriteBatch, Projectile.Center + new Vector2(0f, -28f), shownNumber, 1.1f, 0.9f);
			return false;
		}
	}
}
