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
	/// 此面向敌（阔剑雷）：由 W 从手中抛出，落地布设，雷有**朝向**——正面是一片扇形杀伤区（常驻红色扇形预警），<br/>
	/// 半径 5 格的触发圈；玩家入圈 1 秒（蜂鸣加速）后引爆：雷身处一小团近距爆炸（48px）+ 向正面锥形喷出 7 片破片（250% + 混乱 5s）。<br/>
	/// 绕到雷的背面或锥外就安全——"此面向敌"四个字是玩法本身。<br/>
	/// ai[0]=阶段（0 下落 / 1 待机 / 2 触发倒数）；ai[1]=触发倒计时；<br/>
	/// ai[2]=±(W 的 whoAmI + 1)：绝对值 0 = 无主（W 不在场时雷失效），符号 = 朝向（正右负左）。
	/// </summary>
	public class WClaymore : ModProjectile, IWOrdnance
	{
		private const float TriggerRadius = 80f;  // 5 格
		private const int TriggerDelay = 60;      // 触发后 1s 引爆
		private const float ConeHalfAngle = 0.6f; // 约 ±35°
		private const float ConeLength = 260f;
		private const float ConeTilt = 0.2f;      // 略微上扬，破片别全贴地皮飞
		private const int Shards = 7;
		private const float PointBlastRadius = 48f;

		private bool fizzle; // 失效：Kill 时不起爆

		private float Stage {
			get => Projectile.ai[0];
			set => Projectile.ai[0] = value;
		}

		private int FacingSign => Projectile.ai[2] < 0f ? -1 : 1;
		private float FacingAngle => FacingSign > 0 ? -ConeTilt : MathHelper.Pi + ConeTilt;

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

		private bool OwnerAlive {
			get {
				int idx = Math.Abs((int)Projectile.ai[2]) - 1;
				if (idx < 0 || idx >= Main.maxNPCs)
					return true; // 无主 → 按常规寿命走
				NPC owner = Main.npc[idx];
				return owner.active && owner.type == ModContent.NPCType<NPCs.Enemy.W.WBoss>();
			}
		}

		public override void SetDefaults() {
			Projectile.width = 20;
			Projectile.height = 14;
			Projectile.hostile = true;
			Projectile.friendly = false;
			Projectile.tileCollide = false; // 手动 TileCollision：平台顶面也要能布设
			Projectile.penetrate = -1;
			Projectile.timeLeft = 7200;
			Projectile.aiStyle = -1;
		}

		public override bool CanHitPlayer(Player target) => false;

		// 带平台支撑的下落（fallThrough=false 时 TileCollision 会挡在平台顶面）
		private bool ApplyGroundedFall() => WOrdnanceUtil.ApplyGroundedFall(Projectile);

		public override void AI() {
			// Boss 战结束（W 被击败或脱战）→ 雷失效：一小撮烟，不爆。
			// 客户端也置 fizzle，这样收到 Kill 包时能播同样的失效烟而不是无声消失
			if (!OwnerAlive) {
				fizzle = true;
				if (Main.netMode != NetmodeID.MultiplayerClient)
					Projectile.Kill();
				return;
			}

			if (Stage == 0f) {
				// 从 W 手里抛出的下落段：飞行中带旋转，落到实体砖/平台顶面即布设（水平速度不衰减，SolveLob 才算得准）
				Projectile.rotation += Projectile.velocity.X * 0.06f;
				if (!Main.dedServ && Main.rand.NextBool(3)) {
					Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Smoke, -Projectile.velocity * 0.1f, 140, default, 0.7f);
					d.noGravity = true;
				}
				if (ApplyGroundedFall()) {
					Stage = 1f;
					Projectile.velocity = Vector2.Zero;
					Projectile.netUpdate = true;
					SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
				}
				return;
			}

			// 布设后：地面被挖掉会重新下落，落回新表面
			Projectile.velocity.X = 0f;
			ApplyGroundedFall();
			Projectile.rotation = 0f;

			if (Stage == 1f) {
				// 待机：服务端扫描玩家入圈
				if (Main.netMode != NetmodeID.MultiplayerClient) {
					for (int i = 0; i < Main.maxPlayers; i++) {
						Player player = Main.player[i];
						if (player.active && !player.dead && Vector2.Distance(player.Center, Projectile.Center) <= TriggerRadius) {
							Stage = 2f;
							Projectile.ai[1] = TriggerDelay;
							Projectile.netUpdate = true;
							break;
						}
					}
				}
			}
			else if (Stage == 2f) {
				// 倒数总长各端自行记住见过的最大值（远程起爆可能给 120 而非默认 60），蜂鸣/闪灯按比例走
				if (Projectile.ai[1] > Projectile.localAI[0])
					Projectile.localAI[0] = Projectile.ai[1];
				Projectile.ai[1]--;
				float total = Math.Max(Projectile.localAI[0], 1f);
				// 蜂鸣加速：剩余越少间隔越短；红灯同步越闪越急
				int interval = (int)MathHelper.Clamp(Projectile.ai[1] / 5f, 3f, 12f);
				if (!Main.dedServ) {
					if ((int)Projectile.ai[1] % interval == 0)
						SoundEngine.PlaySound(SoundID.MaxMana with { Pitch = 0.6f - Projectile.ai[1] / total * 0.6f, Volume = 0.6f }, Projectile.Center);
					float p = 1f - Projectile.ai[1] / total;
					Lighting.AddLight(Projectile.Center, 0.9f * (0.4f + 0.6f * p), 0.15f, 0.1f);
				}
				if (Projectile.ai[1] <= 0 && Main.netMode != NetmodeID.MultiplayerClient) {
					Projectile.Kill();
					return;
				}
			}
		}

		public override void OnKill(int timeLeft) {
			if (fizzle) {
				if (!Main.dedServ)
					NPCs.Enemy.W.WBoss.SmokeBurst(Projectile.Center, 6, 1.2f);
				return;
			}
			// 定向引爆：雷身一小团近距爆炸 + 正面锥形破片扇；爆炸判定体只负责贴脸那一圈，远处全靠破片
			if (Main.netMode != NetmodeID.MultiplayerClient) {
				Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
					ModContent.ProjectileType<WExplosion>(), Projectile.damage, 8f, Main.myPlayer,
					ai0: PointBlastRadius, ai1: 300f);
				Vector2 origin = Projectile.Center + new Vector2(FacingSign * 6f, -4f);
				for (int i = 0; i < Shards; i++) {
					float t = Shards == 1 ? 0f : i / (float)(Shards - 1) * 2f - 1f;
					float angle = FacingAngle + t * (ConeHalfAngle - 0.06f) + Main.rand.NextFloat(-0.03f, 0.03f);
					float speed = Main.rand.NextFloat(12f, 15f);
					Projectile.NewProjectile(Projectile.GetSource_FromThis(), origin, angle.ToRotationVector2() * speed,
						ModContent.ProjectileType<WShrapnel>(), Projectile.damage, 4f, Main.myPlayer);
				}
			}
		}

		public override bool PreDraw(ref Color lightColor) {
			if (Stage != 0f) {
				float k;
				if (Stage == 1f)
					k = 0.5f + 0.15f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4f);
				else {
					float progress = 1f - Projectile.ai[1] / Math.Max(Projectile.localAI[0], 1f);
					k = 0.55f + 0.45f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * (20f + progress * 40f));
				}
				// 触发圈：淡一点，只说"走到这里会触发"
				Texture2D ring = ModContent.Request<Texture2D>("ArknightsMod/Content/Projectiles/Bosses/W/WClaymoreRing").Value;
				Main.EntitySpriteDraw(ring, Projectile.Center - Main.screenPosition, null,
					Color.White * (k * 0.55f), 0f, ring.Size() * 0.5f, 1f, SpriteEffects.None, 0);
				// 杀伤扇：亮一点，说"这一面向敌"
				WTelegraph.BeginAdditive(Main.spriteBatch);
				WTelegraph.DrawCone(Main.spriteBatch, Projectile.Center + new Vector2(FacingSign * 6f, -4f), FacingAngle, ConeHalfAngle, ConeLength, WTelegraph.WarnRed * k);
				WTelegraph.EndAdditive(Main.spriteBatch);
			}
			// 雷体朝向：贴图默认朝右，朝左时水平翻转
			Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
			Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, tex.Size() * 0.5f, 1f,
				FacingSign < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);
			return false;
		}
	}
}
