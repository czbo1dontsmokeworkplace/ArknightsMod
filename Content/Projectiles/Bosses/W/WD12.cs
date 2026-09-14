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
	/// D12 骰雷：<br/>
	/// ai[0] 低位 = 阶段：0 一阶段（倒数计时技能）——近距离（120px）跟踪并黏附玩家，黏附/滚停时启动 3s 引信；<br/>
	/// 1 二阶段（技能3）——切向加速弧线飞行，更远（300px）黏附，或落于方块成雷，黏附/落地才启动引信；<br/>
	/// ai[0] 十位以上 = 掷出的点数（1~12，0 未掷）：引信启动那一刻掷出并同步，数字决定爆法（见 OnKill）；<br/>
	/// ai[1]=黏附玩家 whoAmI（-1 未黏附；-2 已被甩脱，不再跟踪/黏附）；ai[2]=引信剩余 tick（0 未启动）。<br/>
	/// 被黏附玩家快速左键连点 15 次可甩脱（WD12Player + ArkMessageID.WD12Detach）。
	/// 爆炸：雷管范围，300%，混乱 5s（一阶段）/ 10s（二阶段）；点数只改变形状与附带物，不改基础伤害。
	/// </summary>
	public class WD12 : ModProjectile, IWOrdnance
	{
		private const int FuseTicks = 180;
		private const float BlastRadius = 125f;      // 雷管范围参照
		private const float AttachRangeP1 = 120f;
		private const float AttachRangeP2 = 300f;
		private const float DetachedMark = -2f;

		private bool IsPhase2 => (int)Projectile.ai[0] % 10 == 1;
		/// <summary>掷出的点数，0 = 还没掷</summary>
		private int Roll => (int)Projectile.ai[0] / 10;
		private int AttachedPlayer => (int)Projectile.ai[1];
		/// <summary>甩脱后的骰子：只剩引信在走，不再追人</summary>
		private bool Detached => Projectile.ai[1] == DetachedMark;

		// 表现层本地状态
		private int shownNumber = 1, shownTimer;   // 未掷时乱跳的数字
		private bool rollSeen;                      // 本端第一次看到点数
		private int rollShowTimer;                  // 点数放大展示计时

		public bool IsLive => Landed || AttachedPlayer >= 0 || Projectile.ai[2] > 0f;

		public void Prime(int ticks) {
			if (Projectile.ai[2] <= 0f || Projectile.ai[2] > ticks)
				StartFuse(ticks, force: true);
		}

		/// <summary>启动引信并掷骰（仅服务端/单机端）；force 时允许把已在走的引信缩短</summary>
		private void StartFuse(int ticks, bool force = false) {
			if (Projectile.ai[2] > 0f && !force)
				return;
			Projectile.ai[2] = ticks;
			if (Roll == 0)
				Projectile.ai[0] = (IsPhase2 ? 1f : 0f) + 10f * Main.rand.Next(1, 13);
			Projectile.netUpdate = true;
		}
		private bool Landed {
			get => Projectile.localAI[1] == 1f;
			set => Projectile.localAI[1] = value ? 1f : 0f;
		}

		public override void SetDefaults() {
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.hostile = true;
			Projectile.friendly = false;
			Projectile.tileCollide = true;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 900;
			Projectile.aiStyle = -1;
		}

		public override bool CanHitPlayer(Player target) => false;

		public override void AI() {
			if (AttachedPlayer >= 0)
				TickAttached();
			else if (Landed)
				TickLanded();
			else
				TickFlying();

			TickFuse();
			TickRollDisplay();
		}

		/// <summary>点数表现：没掷时数字乱跳；第一次看到点数放大展示一拍（12 用更高的音）</summary>
		private void TickRollDisplay() {
			if (Roll == 0) {
				if (++shownTimer >= 4) {
					shownTimer = 0;
					shownNumber = Main.rand.Next(1, 13);
				}
				return;
			}
			if (!rollSeen) {
				rollSeen = true;
				rollShowTimer = 45;
				if (!Main.dedServ)
					SoundEngine.PlaySound(SoundID.MaxMana with { Pitch = Roll == 12 ? 0.8f : 0.3f, Volume = 0.8f }, Projectile.Center);
			}
			if (rollShowTimer > 0)
				rollShowTimer--;
		}

		private void TickFlying() {
			Projectile.tileCollide = true; // 甩脱/黏附状态切换后各端字段自愈
			Projectile.velocity.Y += 0.2f;
			if (Projectile.velocity.Y > 15f)
				Projectile.velocity.Y = 15f;
			Projectile.rotation += Projectile.velocity.X * 0.05f;

			// 一阶段：未黏附且在地面滚停 → 启动引信（保证黏附时玩家总有完整 3s 甩脱窗口）
			if (!IsPhase2 && Projectile.ai[2] <= 0f && Main.netMode != NetmodeID.MultiplayerClient) {
				if (Projectile.velocity.Length() < 0.6f) {
					if (++Projectile.localAI[0] >= 20f)
						StartFuse(FuseTicks);
				}
				else {
					Projectile.localAI[0] = 0f;
				}
			}

			// 甩脱后不再追踪、不再黏附：否则甩出去的骰子下一帧就被 120px 追踪拉回来重新贴上，15 连点等于白点
			float attachRange = IsPhase2 ? AttachRangeP2 : AttachRangeP1;
			Player nearest = Detached ? null : FindNearestPlayer(attachRange);
			if (nearest != null) {
				// 追踪：朝目标匀速修正；二阶段附加随飞行时间渐增的切向分量（弧线逼近，策划：切向速度逐渐增加）
				Vector2 toTarget = (nearest.Center - Projectile.Center).SafeNormalize(Vector2.UnitY);
				float speed = Math.Max(Projectile.velocity.Length(), IsPhase2 ? 11f : 9f);
				Vector2 desired = toTarget * speed;
				if (IsPhase2) {
					float curveSign = Math.Sign(Projectile.velocity.X * toTarget.Y - Projectile.velocity.Y * toTarget.X);
					if (curveSign == 0)
						curveSign = 1;
					float ramp = Math.Min((900 - Projectile.timeLeft) / 90f, 1f) * 4.5f;
					desired += toTarget.RotatedBy(MathHelper.PiOver2 * curveSign) * ramp;
				}
				Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.08f);
			}

			if (!Main.dedServ && Main.rand.NextBool(3)) {
				Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Torch, -Projectile.velocity * 0.1f, 100, default, 0.8f);
				d.noGravity = true;
			}

			// 服务端：碰到玩家 → 黏附（甩脱后的骰子不再黏）
			if (Main.netMode != NetmodeID.MultiplayerClient && !Detached) {
				for (int i = 0; i < Main.maxPlayers; i++) {
					Player player = Main.player[i];
					if (player.active && !player.dead && Projectile.Hitbox.Intersects(player.Hitbox)) {
						Projectile.ai[1] = i;
						StartFuse(FuseTicks);
						Projectile.tileCollide = false;
						Projectile.netUpdate = true;
						break;
					}
				}
			}
		}

		private void TickAttached() {
			Player carrier = Main.player[AttachedPlayer];
			if (!carrier.active || carrier.dead) {
				// 载体没了：原地落雷
				if (Main.netMode != NetmodeID.MultiplayerClient) {
					Projectile.ai[1] = -1f;
					Projectile.tileCollide = true;
					Projectile.netUpdate = true;
				}
				return;
			}
			Projectile.tileCollide = false;
			// 贴在玩家身上小幅晃动
			float wobble = (float)Math.Sin(Main.GlobalTimeWrappedHourly * 18f) * 2f;
			Projectile.Center = carrier.Center + new Vector2(carrier.direction * -6f + wobble, -6f);
			Projectile.velocity = Vector2.Zero;
			Projectile.rotation += 0.08f;
		}

		private void TickLanded() {
			Projectile.velocity.X = 0f;
			Projectile.velocity.Y += 0.3f; // 地面被挖掉时继续下落
			Projectile.rotation = 0f;
		}

		private void TickFuse() {
			if (Projectile.ai[2] <= 0f)
				return;
			Projectile.ai[2]--;

			// 蜂鸣加速 + 尾声闪烁
			int remain = (int)Projectile.ai[2];
			int interval = (int)MathHelper.Clamp(remain / 8f, 4f, 18f);
			if (!Main.dedServ && remain % Math.Max(interval, 1) == 0) {
				SoundEngine.PlaySound(SoundID.MaxMana with { Pitch = 0.7f - remain / (float)FuseTicks, Volume = 0.55f }, Projectile.Center);
				Projectile.localAI[2] = 6f; // 蜂鸣的同时闪一下红光（PreDraw 读）
			}
			if (Projectile.localAI[2] > 0f)
				Projectile.localAI[2]--;
			if (!Main.dedServ)
				Lighting.AddLight(Projectile.Center, 0.9f * FuseGlow, 0.25f * FuseGlow, 0.1f * FuseGlow);

			if (remain <= 0 && Main.netMode != NetmodeID.MultiplayerClient)
				Projectile.Kill();
		}

		/// <summary>引信红光强度 0~1：越接近引爆越亮，蜂鸣瞬间再顶一下</summary>
		private float FuseGlow {
			get {
				if (Projectile.ai[2] <= 0f)
					return 0f;
				float progress = 1f - Projectile.ai[2] / FuseTicks;
				return MathHelper.Clamp(0.25f + 0.55f * progress + 0.35f * (Projectile.localAI[2] / 6f), 0f, 1f);
			}
		}

		public override bool PreDraw(ref Color lightColor) {
			// 骰子本体照常画，引信启动后叠一层随进度加深、随蜂鸣跳动的红
			Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
			Vector2 pos = Projectile.Center - Main.screenPosition;
			Vector2 origin = tex.Size() * 0.5f;
			Main.EntitySpriteDraw(tex, pos, null, lightColor, Projectile.rotation, origin, 1f, SpriteEffects.None, 0);
			float glow = FuseGlow;
			if (glow > 0.01f) {
				Color red = new Color(255, 60, 40, 0) * glow;
				Main.EntitySpriteDraw(tex, pos, null, red, Projectile.rotation, origin, 1f + 0.08f * glow, SpriteEffects.None, 0);
			}
			// 点数：没掷时小字乱跳；掷出后放大定格再缩回，12 是金色
			if (Roll == 0)
				WTelegraph.DrawDieFace(Main.spriteBatch, Projectile.Center + new Vector2(0f, -18f), shownNumber, 0.75f, 0.75f);
			else {
				float pop = rollShowTimer / 45f;
				WTelegraph.DrawDieFace(Main.spriteBatch, Projectile.Center + new Vector2(0f, -20f - 10f * pop), Roll, 0.95f + 0.8f * pop, 1f);
			}
			return false;
		}

		private Player FindNearestPlayer(float range) {
			Player best = null;
			float bestDist = range;
			for (int i = 0; i < Main.maxPlayers; i++) {
				Player player = Main.player[i];
				if (!player.active || player.dead)
					continue;
				float dist = Vector2.Distance(player.Center, Projectile.Center);
				if (dist < bestDist) {
					bestDist = dist;
					best = player;
				}
			}
			return best;
		}

		public override bool OnTileCollide(Vector2 oldVelocity) {
			if (IsPhase2) {
				// 二阶段：落地成雷，启动引信
				Landed = true;
				Projectile.velocity = Vector2.Zero;
				if (Main.netMode != NetmodeID.MultiplayerClient)
					StartFuse(FuseTicks);
			}
			else {
				// 一阶段：手雷式弹跳衰减（滚停后由 TickFlying 的静止判定点燃引信）
				if (Math.Abs(oldVelocity.Y) > 1f)
					Projectile.velocity.Y = oldVelocity.Y * -0.4f;
				else
					Projectile.velocity.Y = 0f;
				if (Math.Abs(oldVelocity.X) > 0.5f)
					Projectile.velocity.X = oldVelocity.X * 0.5f;
			}
			return false;
		}

		public override void OnKill(int timeLeft) {
			int roll = Roll;
			// JACKPOT 的金色白闪两端都播（点数已同步）
			if (roll == 12) {
				WBattleVisuals.Flash(0.3f, new Color(255, 225, 150));
				SoundEngine.PlaySound(SoundID.Item4 with { Pitch = 0.4f }, Projectile.Center);
			}
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			float confused = IsPhase2 ? 600f : 300f;
			// 点数决定形状：1~4 小爆；5~8 爆 + 四向碎片；9~11 爆 + 一圈留火；12 大爆 + 六向碎片 + W 立刻接红桃K
			float radius = roll switch {
				>= 1 and <= 4 => 100f,
				12 => 150f,
				_ => BlastRadius,
			};
			Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
				ModContent.ProjectileType<WExplosion>(), Projectile.damage, 8f, Main.myPlayer,
				ai0: radius, ai1: confused, ai2: roll == 12 ? 2f : 0f);

			int bombletDamage = Math.Max(1, (int)(Projectile.damage * 0.5f));
			if (roll is >= 5 and <= 8)
				SpawnFragments(4, 7f, bombletDamage);
			else if (roll is >= 9 and <= 11)
				SpawnEmberRing(6, bombletDamage);
			else if (roll == 12) {
				SpawnFragments(6, 8f, bombletDamage);
				int idx = NPC.FindFirstNPC(ModContent.NPCType<WBoss>());
				if (idx >= 0 && Main.npc[idx].ModNPC is WBoss w) {
					w.JackpotPending = true;
					WBattleVisuals.Say("Jackpot");
				}
			}
		}

		/// <summary>向上方扇形甩出 count 颗集束子弹（落地小爆），仅服务端/单机端</summary>
		private void SpawnFragments(int count, float speed, int damage) {
			for (int i = 0; i < count; i++) {
				float angle = MathHelper.Lerp(-MathHelper.Pi * 0.85f, -MathHelper.Pi * 0.15f, (i + 0.5f) / count);
				Vector2 vel = angle.ToRotationVector2() * speed + Main.rand.NextVector2Circular(0.6f, 0.6f);
				Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, vel,
					ModContent.ProjectileType<WHEGrenade>(), damage, 1f, Main.myPlayer, ai0: 2f);
			}
		}

		/// <summary>爆点周围撒一圈余火（落地后烧约 1.7s），仅服务端/单机端</summary>
		private void SpawnEmberRing(int count, int damage) {
			for (int i = 0; i < count; i++) {
				float t = (i + 0.5f) / count * 2f - 1f; // -1..1
				Vector2 vel = new(t * 5.5f, -3f - (1f - Math.Abs(t)) * 1.5f);
				Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, vel,
					ModContent.ProjectileType<WEmber>(), damage, 0f, Main.myPlayer);
			}
		}

		/// <summary>
		/// 甩脱：从玩家身后弹开落地，引信继续走，之后不再追人（服务端/单机端调用）。<br/>
		/// 音效由发起甩脱的本地玩家播放（WD12Player），服务端播不出声。
		/// </summary>
		public static void Detach(Projectile projectile) {
			if (projectile.ModProjectile is not WD12)
				return;
			int carrierIdx = (int)projectile.ai[1];
			int flingDir = 1;
			if (carrierIdx >= 0 && carrierIdx < Main.maxPlayers && Main.player[carrierIdx].active)
				flingDir = -Main.player[carrierIdx].direction;
			projectile.ai[1] = DetachedMark;
			projectile.tileCollide = true;
			projectile.velocity = new Vector2(flingDir * 9f, -6f);
			projectile.netUpdate = true;
		}

		/// <summary>服务端处理甩脱请求：校验该弹确实黏在请求者身上，防误伤他人交互</summary>
		public static void TryDetachFromPacket(int projIndex, int requesterWhoAmI) {
			if (projIndex < 0 || projIndex >= Main.maxProjectiles)
				return;
			Projectile projectile = Main.projectile[projIndex];
			if (!projectile.active || projectile.ModProjectile is not WD12)
				return;
			if ((int)projectile.ai[1] != requesterWhoAmI)
				return;
			Detach(projectile);
		}
	}
}
