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
	/// 线雷：烟带撒雷时从烟里一路落下的小炸药包。落地待机（红灯慢闪），由 W 按顺序远程起爆（Prime），<br/>
	/// 连成一道推进的爆炸墙；5 秒内没人起爆就自己散掉。爆炸 70px，伤害由 W 给（100%），不带混乱。<br/>
	/// ai[0]=阶段（0 下落 / 1 待机 / 2 倒数）；ai[1]=倒数；ai[2]=W 的 whoAmI + 1（0 无主）。
	/// </summary>
	public class WLineCharge : ModProjectile, IWOrdnance
	{
		public override string Texture => "ArknightsMod/Content/Projectiles/Bosses/W/WHEGrenade";

		private const float BlastRadius = 60f; // 小一点：爆炸墙要能被一跳跨过
		private const int IdleTimeout = 300;

		private bool fizzle;

		private float Stage {
			get => Projectile.ai[0];
			set => Projectile.ai[0] = value;
		}

		private bool OwnerAlive {
			get {
				int idx = (int)Projectile.ai[2] - 1;
				if (idx < 0 || idx >= Main.maxNPCs)
					return true;
				NPC owner = Main.npc[idx];
				return owner.active && owner.type == ModContent.NPCType<WBoss>();
			}
		}

		public bool IsLive => Stage >= 1f;

		public void Prime(int ticks) {
			if (Stage < 1f)
				return;
			if (Stage == 1f || Projectile.ai[1] > ticks) {
				Stage = 2f;
				Projectile.ai[1] = ticks;
				Projectile.netUpdate = true;
			}
		}

		public override void SetDefaults() {
			Projectile.width = 12;
			Projectile.height = 14;
			Projectile.hostile = true;
			Projectile.friendly = false;
			Projectile.tileCollide = false; // 手动 TileCollision：平台顶面也要能落
			Projectile.penetrate = -1;
			Projectile.timeLeft = 1200;
			Projectile.aiStyle = -1;
		}

		public override bool CanHitPlayer(Player target) => false;

		public override void AI() {
			if (!OwnerAlive) {
				fizzle = true;
				if (Main.netMode != NetmodeID.MultiplayerClient)
					Projectile.Kill();
				return;
			}

			if (Stage == 0f) {
				Projectile.rotation += 0.12f;
				if (!Main.dedServ && Main.rand.NextBool(2)) {
					Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Smoke, new Vector2(0f, -0.5f), 150, default, 0.8f);
					d.noGravity = true;
				}
				if (WOrdnanceUtil.ApplyGroundedFall(Projectile)) {
					Stage = 1f;
					Projectile.velocity = Vector2.Zero;
					Projectile.rotation = 0f;
					Projectile.netUpdate = true;
					SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.6f, Pitch = 0.4f }, Projectile.Center);
				}
				return;
			}

			Projectile.velocity.X = 0f;
			WOrdnanceUtil.ApplyGroundedFall(Projectile);
			Projectile.rotation = 0f;

			if (Stage == 1f) {
				Projectile.localAI[0]++;
				if (!Main.dedServ) {
					float blink = (MathF.Sin(Projectile.localAI[0] * 0.12f + Projectile.whoAmI) + 1f) * 0.5f;
					Lighting.AddLight(Projectile.Center, 0.45f * blink, 0.1f * blink, 0.05f);
				}
				if (Projectile.localAI[0] >= IdleTimeout && Main.netMode != NetmodeID.MultiplayerClient) {
					fizzle = true;
					Projectile.Kill();
				}
				return;
			}

			// Stage 2：倒数，越近越急
			if (Projectile.ai[1] > Projectile.localAI[1])
				Projectile.localAI[1] = Projectile.ai[1];
			Projectile.ai[1]--;
			float total = Math.Max(Projectile.localAI[1], 1f);
			int interval = (int)MathHelper.Clamp(Projectile.ai[1] / 4f, 2f, 10f);
			if (!Main.dedServ) {
				if ((int)Projectile.ai[1] % interval == 0)
					SoundEngine.PlaySound(SoundID.MaxMana with { Pitch = 0.5f - Projectile.ai[1] / total * 0.5f, Volume = 0.5f }, Projectile.Center);
				Lighting.AddLight(Projectile.Center, 1.0f, 0.3f, 0.1f);
			}
			if (Projectile.ai[1] <= 0f && Main.netMode != NetmodeID.MultiplayerClient)
				Projectile.Kill();
		}

		public override void OnKill(int timeLeft) {
			if (fizzle) {
				if (!Main.dedServ)
					WBoss.SmokeBurst(Projectile.Center, 4, 1f);
				return;
			}
			if (Main.netMode != NetmodeID.MultiplayerClient) {
				Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
					ModContent.ProjectileType<WExplosion>(), Projectile.damage, 5f, Main.myPlayer,
					ai0: BlastRadius, ai1: 0f);
			}
		}

		public override bool PreDraw(ref Color lightColor) {
			Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
			Vector2 pos = Projectile.Center - Main.screenPosition;
			Main.EntitySpriteDraw(tex, pos, null, lightColor.MultiplyRGB(new Color(255, 210, 180)), Projectile.rotation, tex.Size() * 0.5f, 0.9f, SpriteEffects.None, 0);
			if (Stage >= 1f) {
				float k = Stage == 2f
					? 0.5f + 0.5f * MathF.Sin(Main.GlobalTimeWrappedHourly * 32f)
					: 0.25f + 0.25f * MathF.Sin(Projectile.localAI[0] * 0.12f + Projectile.whoAmI);
				Main.EntitySpriteDraw(WTelegraph.Glow, pos + new Vector2(0f, -4f), null, new Color(255, 70, 40, 0) * k, 0f, WTelegraph.Glow.Size() * 0.5f, 0.22f + 0.1f * k, SpriteEffects.None, 0);
			}
			return false;
		}
	}
}
