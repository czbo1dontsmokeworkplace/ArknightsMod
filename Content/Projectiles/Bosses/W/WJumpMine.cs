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
	/// 跳雷（S 型跳雷）：由 W 抛出落地布设；玩家靠近（90px）时"哒"一声跃到胸口高度，约 16 tick 后在顶点附近炸（72px，100%）。<br/>
	/// 与阔剑雷的区别：不是踩进圈里 1 秒后炸，而是先跳起来给你看清再炸，节奏更急、范围更小，躲法是立刻拉开距离。<br/>
	/// ai[0]=阶段（0 下落 / 1 待机 / 2 已起跳 / 3 远程起爆倒数）；ai[1]=倒数；ai[2]=W 的 whoAmI + 1（0 无主）。
	/// </summary>
	public class WJumpMine : ModProjectile, IWOrdnance
	{
		public override string Texture => "ArknightsMod/Content/Projectiles/Bosses/W/WClaymore";

		private const float TriggerRadius = 90f;
		private const float BlastRadius = 72f;
		private const float JumpSpeed = -7.5f;
		private const int JumpTicks = 16; // 起跳到起爆

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
			if (Stage != 1f && !(Stage == 3f && Projectile.ai[1] > ticks))
				return;
			Stage = 3f;
			Projectile.ai[1] = ticks;
			Projectile.netUpdate = true;
		}

		public override void SetDefaults() {
			Projectile.width = 18;
			Projectile.height = 14;
			Projectile.hostile = true;
			Projectile.friendly = false;
			Projectile.tileCollide = false; // 手动 TileCollision：平台顶面也要能布设
			Projectile.penetrate = -1;
			Projectile.timeLeft = 7200;
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
				Projectile.rotation += Projectile.velocity.X * 0.06f;
				if (WOrdnanceUtil.ApplyGroundedFall(Projectile)) {
					Stage = 1f;
					Projectile.velocity = Vector2.Zero;
					Projectile.netUpdate = true;
					SoundEngine.PlaySound(SoundID.Dig with { Pitch = 0.2f }, Projectile.Center);
				}
				return;
			}

			if (Stage == 1f || Stage == 3f) {
				Projectile.velocity.X = 0f;
				WOrdnanceUtil.ApplyGroundedFall(Projectile);
				Projectile.rotation = 0f;
				if (!Main.dedServ) {
					// 待机红灯：慢闪
					float blink = (MathF.Sin(Main.GlobalTimeWrappedHourly * 6f + Projectile.whoAmI) + 1f) * 0.5f;
					Lighting.AddLight(Projectile.Center, 0.5f * blink, 0.12f * blink, 0.05f);
				}

				if (Stage == 1f) {
					if (Main.netMode != NetmodeID.MultiplayerClient) {
						for (int i = 0; i < Main.maxPlayers; i++) {
							Player player = Main.player[i];
							if (player.active && !player.dead && Vector2.Distance(player.Center, Projectile.Center) <= TriggerRadius) {
								Jump();
								break;
							}
						}
					}
				}
				else {
					// 远程起爆倒数：到点前 JumpTicks 起跳，让总时长对得上
					if (Projectile.ai[1] > Projectile.localAI[0])
						Projectile.localAI[0] = Projectile.ai[1];
					Projectile.ai[1]--;
					int interval = (int)MathHelper.Clamp(Projectile.ai[1] / 5f, 3f, 12f);
					if (!Main.dedServ && (int)Projectile.ai[1] % interval == 0)
						SoundEngine.PlaySound(SoundID.MaxMana with { Pitch = 0.6f - Projectile.ai[1] / Math.Max(Projectile.localAI[0], 1f) * 0.6f, Volume = 0.55f }, Projectile.Center);
					if (Projectile.ai[1] <= JumpTicks && Main.netMode != NetmodeID.MultiplayerClient)
						Jump();
				}
				return;
			}

			// Stage 2：跃起 → 顶点附近炸
			Projectile.tileCollide = true;
			Projectile.velocity.Y += 0.35f;
			Projectile.velocity.X *= 0.95f;
			Projectile.rotation += 0.25f;
			Projectile.ai[1]++;
			if (!Main.dedServ) {
				Lighting.AddLight(Projectile.Center, 1.0f, 0.3f, 0.1f);
				Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Torch, -Projectile.velocity * 0.2f, 0, default, 1.2f);
				d.noGravity = true;
			}
			if (Projectile.ai[1] >= JumpTicks && Main.netMode != NetmodeID.MultiplayerClient)
				Projectile.Kill();
		}

		private void Jump() {
			Stage = 2f;
			Projectile.ai[1] = 0f;
			Projectile.velocity = new Vector2(Main.rand.NextFloat(-0.6f, 0.6f), JumpSpeed);
			Projectile.netUpdate = true;
			SoundEngine.PlaySound(SoundID.Unlock with { Pitch = 0.6f, Volume = 0.8f }, Projectile.Center);
			WBoss.FootDust(Projectile.Center + new Vector2(0f, 6f), 6, 1.6f);
		}

		public override bool OnTileCollide(Vector2 oldVelocity) {
			// 起跳撞到低矮顶棚：就地炸
			if (Stage == 2f && Main.netMode != NetmodeID.MultiplayerClient)
				Projectile.Kill();
			return false;
		}

		public override void OnKill(int timeLeft) {
			if (fizzle) {
				if (!Main.dedServ)
					WBoss.SmokeBurst(Projectile.Center, 6, 1.2f);
				return;
			}
			if (Main.netMode != NetmodeID.MultiplayerClient) {
				Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
					ModContent.ProjectileType<WExplosion>(), Projectile.damage, 6f, Main.myPlayer,
					ai0: BlastRadius, ai1: 0f);
			}
		}

		public override bool PreDraw(ref Color lightColor) {
			Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
			Vector2 pos = Projectile.Center - Main.screenPosition;
			Vector2 origin = tex.Size() * 0.5f;
			// 与阔剑雷区分：橙色调 + 更小更淡的触发圈
			Color tint = lightColor.MultiplyRGB(new Color(255, 200, 150));
			if (Stage >= 1f && Stage != 2f) {
				Texture2D ring = ModContent.Request<Texture2D>("ArknightsMod/Content/Projectiles/Bosses/W/WClaymoreRing").Value;
				float pulse = 0.22f + 0.08f * MathF.Sin(Main.GlobalTimeWrappedHourly * 3f + Projectile.whoAmI);
				if (Stage == 3f)
					pulse = 0.35f + 0.3f * MathF.Sin(Main.GlobalTimeWrappedHourly * 30f);
				Main.EntitySpriteDraw(ring, pos, null, new Color(255, 150, 60) * pulse, 0f, ring.Size() * 0.5f, TriggerRadius / 80f, SpriteEffects.None, 0);
			}
			Main.EntitySpriteDraw(tex, pos, null, tint, Projectile.rotation, origin, 0.85f, SpriteEffects.None, 0);
			if (Stage == 2f) {
				float k = Projectile.ai[1] / JumpTicks;
				Main.EntitySpriteDraw(WTelegraph.Glow, pos, null, new Color(255, 90, 40, 0) * (0.4f + 0.6f * k), 0f, WTelegraph.Glow.Size() * 0.5f, 0.3f + 0.5f * k, SpriteEffects.None, 0);
			}
			return false;
		}
	}
}
