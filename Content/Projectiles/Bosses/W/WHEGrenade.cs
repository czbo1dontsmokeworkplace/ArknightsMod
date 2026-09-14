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
	/// 高爆手雷，一类四模式（ai[0]）：<br/>
	/// 0 普通——重力抛体，触块/触玩家/引信到时以手雷范围引爆，100%；<br/>
	/// 1 集束母弹——越接近抛物线顶点闪得越急，到顶点炸成 5 颗子弹扇形落下（母弹本体只是一声脆响）；<br/>
	/// 2 集束子弹——小范围（40px），伤害由母弹给（70%）；<br/>
	/// 3 烟火弹——发射器朝天打出的大弹，拖金色火尾升到顶点，金光炸开成 12 颗子弹环形洒下（满屏落雨）。
	/// </summary>
	public class WHEGrenade : ModProjectile
	{
		private const float Gravity = 0.25f;
		private const float BlastRadius = 64f;    // 手雷范围（约 4 格）
		private const float BombletRadius = 40f;
		private const int Bomblets = 5;
		private const int FireworkBomblets = 12;

		private int Mode => (int)Projectile.ai[0];
		private bool IsCluster => Mode == 1;
		private bool IsBomblet => Mode == 2;
		private bool IsFirework => Mode == 3;

		public override void SetDefaults() {
			Projectile.width = 10;
			Projectile.height = 16;
			Projectile.hostile = true;
			Projectile.friendly = false;
			Projectile.tileCollide = true;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 240;
			Projectile.aiStyle = -1;
		}

		// 伤害只走爆炸判定体
		public override bool CanHitPlayer(Player target) => false;

		public override void AI() {
			Projectile.velocity.Y += Gravity;
			if (Projectile.velocity.Y > 16f)
				Projectile.velocity.Y = 16f;
			Projectile.rotation += Projectile.velocity.X * 0.06f;

			if (IsCluster) {
				// 越接近顶点越急的红闪 + 红光；顶点即炸开
				Projectile.localAI[0]++;
				float rise = MathHelper.Clamp(-Projectile.velocity.Y / 10f, 0f, 1f); // 1 刚出手，0 到顶
				int interval = (int)MathHelper.Lerp(3f, 14f, rise);
				if (!Main.dedServ) {
					if ((int)Projectile.localAI[0] % Math.Max(interval, 1) == 0)
						SoundEngine.PlaySound(SoundID.MaxMana with { Pitch = 0.2f + (1f - rise) * 0.6f, Volume = 0.4f }, Projectile.Center);
					Lighting.AddLight(Projectile.Center, 0.8f * (1f - rise * 0.6f), 0.2f, 0.1f);
				}
				if (Projectile.velocity.Y >= -0.5f && Main.netMode != NetmodeID.MultiplayerClient) {
					Split();
					Projectile.Kill();
					return;
				}
			}
			else if (IsFirework) {
				// 烟火弹：金色火尾 + 一路上扬的哨音，顶点开花
				Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
				Projectile.localAI[0]++;
				if (!Main.dedServ) {
					Lighting.AddLight(Projectile.Center, 1.0f, 0.8f, 0.35f);
					for (int i = 0; i < 2; i++) {
						var p = new DefaultParticle(Projectile.Center + Main.rand.NextVector2Circular(3f, 3f), -Projectile.velocity * Main.rand.NextFloat(0.1f, 0.3f) + Main.rand.NextVector2Circular(0.8f, 0.8f),
							Main.rand.Next(16, 26), Main.rand.NextFloat(0.9f, 1.4f), new Color(255, 215, 120), false) {
							Deformation = new Vector2(0.12f, 0.6f),
						};
						p.Spawn();
					}
					if ((int)Projectile.localAI[0] % 8 == 0)
						SoundEngine.PlaySound(SoundID.MaxMana with { Pitch = -0.2f + Projectile.localAI[0] / 60f, Volume = 0.35f }, Projectile.Center);
				}
				if (Projectile.velocity.Y >= -0.5f && Main.netMode != NetmodeID.MultiplayerClient) {
					Bloom();
					Projectile.Kill();
					return;
				}
			}

			if (!Main.dedServ && !IsFirework && Main.rand.NextBool(IsBomblet ? 2 : 4)) {
				Dust d = Dust.NewDustPerfect(Projectile.Center, IsBomblet ? DustID.Torch : DustID.Smoke, -Projectile.velocity * 0.15f, 130, default, 0.8f);
				d.noGravity = true;
			}

			// 服务端：触玩家即引爆
			if (Main.netMode != NetmodeID.MultiplayerClient) {
				for (int i = 0; i < Main.maxPlayers; i++) {
					Player player = Main.player[i];
					if (player.active && !player.dead && Projectile.Hitbox.Intersects(player.Hitbox)) {
						Projectile.Kill();
						return;
					}
				}
			}
		}

		/// <summary>集束：顶点处扇形撒出 5 颗子弹（仅服务端/单机端）</summary>
		private void Split() {
			int dmg = Math.Max(1, (int)(Projectile.damage * 0.7f));
			for (int i = 0; i < Bomblets; i++) {
				float t = i / (float)(Bomblets - 1) * 2f - 1f; // -1..1
				Vector2 vel = new(t * 4.6f + Main.rand.NextFloat(-0.4f, 0.4f), -2.6f + Math.Abs(t) * 1.2f);
				Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, vel,
					ModContent.ProjectileType<WHEGrenade>(), dmg, 1f, Main.myPlayer, ai0: 2f);
			}
		}

		public override bool OnTileCollide(Vector2 oldVelocity) {
			// 烟火弹撞到顶棚：就地开花，别哑掉
			if (IsFirework && Main.netMode != NetmodeID.MultiplayerClient)
				Bloom();
			return true;
		}

		/// <summary>烟火开花：12 颗子弹环形洒下 + 纯演出的金色冲击波（仅服务端/单机端）</summary>
		private void Bloom() {
			int dmg = Math.Max(1, (int)(Projectile.damage * 0.7f));
			for (int i = 0; i < FireworkBomblets; i++) {
				float angle = MathHelper.TwoPi * i / FireworkBomblets + Main.rand.NextFloat(-0.08f, 0.08f);
				Vector2 vel = angle.ToRotationVector2() * Main.rand.NextFloat(5f, 6.5f) + new Vector2(0f, -1f);
				Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, vel,
					ModContent.ProjectileType<WHEGrenade>(), dmg, 1f, Main.myPlayer, ai0: 2f);
			}
			// 金色冲击波只是演出（ai1 = -1：不伤人）
			Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
				ModContent.ProjectileType<WExplosion>(), 1, 0f, Main.myPlayer, ai0: 130f, ai1: -1f, ai2: 2f);
		}

		public override void OnKill(int timeLeft) {
			if (IsFirework) {
				// 开花那一下：白闪 + 金火星，冲击波由 Bloom 生成的演出体负责
				WBattleVisuals.Flash(0.3f, new Color(255, 230, 170));
				WBoss.ShakeNearby(Projectile.Center, 6, 1200f);
				if (!Main.dedServ) {
					for (int i = 0; i < 24; i++) {
						Vector2 dir = Main.rand.NextVector2Unit();
						var p = new DefaultParticle(Projectile.Center, dir * Main.rand.NextFloat(4f, 9f), Main.rand.Next(24, 40), Main.rand.NextFloat(1.2f, 1.9f), new Color(255, 225, 140), false) {
							Deformation = new Vector2(0.1f, 0.9f),
						};
						p.Spawn();
					}
				}
				return;
			}
			if (IsCluster) {
				// 母弹只是一声脆响 + 一小团火光，真正的爆点是子弹
				SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.5f, Pitch = 0.5f }, Projectile.Center);
				WBoss.SmokeBurst(Projectile.Center, 8, 2f);
				if (!Main.dedServ) {
					for (int i = 0; i < 10; i++) {
						Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Torch, Main.rand.NextVector2Circular(4f, 4f), 0, default, 1.6f);
						d.noGravity = true;
					}
				}
				return;
			}
			if (Main.netMode != NetmodeID.MultiplayerClient) {
				Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
					ModContent.ProjectileType<WExplosion>(), Projectile.damage, IsBomblet ? 4f : 6f, Main.myPlayer,
					ai0: IsBomblet ? BombletRadius : BlastRadius, ai1: 0f);
			}
		}

		public override bool PreDraw(ref Color lightColor) {
			Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
			Vector2 pos = Projectile.Center - Main.screenPosition;
			Vector2 origin = tex.Size() * 0.5f;
			float scale = IsBomblet ? 0.75f : (IsFirework ? 1.35f : 1f);
			if (IsFirework) {
				// 烟火弹整体裹一层金光
				Main.EntitySpriteDraw(WTelegraph.Glow, pos, null, new Color(255, 200, 100, 0) * 0.7f, 0f, WTelegraph.Glow.Size() * 0.5f, 0.55f, SpriteEffects.None, 0);
			}
			Main.EntitySpriteDraw(tex, pos, null, lightColor, Projectile.rotation, origin, scale, SpriteEffects.None, 0);
			// 引信火星：贴着罐体顶端一点跳动的亮光（母弹另有整体红闪）
			if (!IsCluster && !IsFirework) {
				float spark = 0.55f + 0.45f * MathF.Sin(Main.GlobalTimeWrappedHourly * 30f + Projectile.whoAmI * 1.7f);
				Vector2 tip = pos + new Vector2(0f, -9f * scale).RotatedBy(Projectile.rotation);
				Main.EntitySpriteDraw(WTelegraph.Glow, tip, null, new Color(255, 170, 80, 0) * (0.6f * spark), 0f, WTelegraph.Glow.Size() * 0.5f, 0.16f + 0.06f * spark, SpriteEffects.None, 0);
			}
			if (IsCluster) {
				// 越接近顶点越急的红闪
				float rise = MathHelper.Clamp(-Projectile.velocity.Y / 10f, 0f, 1f);
				float freq = MathHelper.Lerp(28f, 8f, rise);
				float k = (MathF.Sin(Main.GlobalTimeWrappedHourly * freq) + 1f) * 0.5f * (0.4f + 0.6f * (1f - rise));
				Main.EntitySpriteDraw(tex, pos, null, new Color(255, 60, 40, 0) * k, Projectile.rotation, origin, 1.08f, SpriteEffects.None, 0);
			}
			return false;
		}
	}
}
