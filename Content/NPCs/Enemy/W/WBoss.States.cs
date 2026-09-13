using ArknightsMod.Content.Projectiles.Bosses.W;
using InnoVault.StateMachines;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.W
{
	// ---------- 调度辅助（技能选择只发生在服务端/单机端） ----------
	public partial class WBoss
	{
		public bool IsAuthority => Main.netMode != NetmodeID.MultiplayerClient;

		public float CurrentDamageScale => Phase == 2 ? P2DamageScale : 1f;

		/// <summary>调度回归态：一阶段行走，二阶段站桩</summary>
		public IVaultState<WBoss> SchedulerState() =>
			Phase == 2 ? new WStandState() : new WWalkState();

		/// <summary>
		/// 实体视线：只看实体物块（直射弹——红桃K / 地毯 / 满注——需要它）。抛物线弹药与空中招不看视线，越过平台是它们的本职。
		/// </summary>
		public bool HasSolidLineOfSight(Player target) =>
			Collision.CanHitLine(NPC.position, NPC.width, NPC.height, target.position, target.width, target.height);

		/// <summary>
		/// 策划案口径的视线：实体物块**或平台**挡在连线中间都算阻挡。只用于「要不要传送去追」的判断，不再用来门控攻击——<br/>
		/// 否则平台竞技场里玩家一上台，整个池子只剩不要视线的招，她就只会传送和复读。
		/// </summary>
		public bool HasLineOfSight(Player target) {
			if (!HasSolidLineOfSight(target))
				return false;
			Vector2 from = NPC.Center;
			Vector2 to = target.Center;
			int footRow = (int)((NPC.Bottom.Y + 4f) / 16f); // 自己脚下那一排平台不算遮挡，否则站上高台就会立刻想传回去
			int steps = (int)(Vector2.Distance(from, to) / 8f);
			for (int i = 1; i < steps; i++) {
				Vector2 p = Vector2.Lerp(from, to, i / (float)steps);
				int tx = (int)(p.X / 16f), ty = (int)(p.Y / 16f);
				if (ty == footRow && Math.Abs(p.X - from.X) < 48f)
					continue;
				Tile tile = Framing.GetTileSafely(tx, ty);
				if (tile.HasUnactuatedTile && TileID.Sets.Platforms[tile.TileType])
					return false;
			}
			return true;
		}

		/// <summary>
		/// 该不该传送去追：被实体墙挡住；或距离过远；或玩家躲在上方平台（平台遮挡且比她高 80px）而她已经两秒没出招。
		/// 最后一条保证"追上平台"仍会发生，但不会每 3 秒打断一次出招。
		/// </summary>
		public bool WantsRepositionTeleport(Player target, float dist, float farDist) {
			if (!HasSolidLineOfSight(target))
				return true;
			if (dist > farDist)
				return true;
			bool platformHidden = !HasLineOfSight(target) && target.Bottom.Y < NPC.Bottom.Y - 80f;
			return platformHidden && SinceAttack >= 120;
		}

		/// <summary>头顶净空：曲射弹要先飞上去再落下来，低矮洞顶下不用</summary>
		public bool HasSkyClearance(float height) =>
			Collision.CanHitLine(NPC.Center, 1, 1, NPC.Center - new Vector2(0f, height), 1, 1);

		/// <summary>抛体初速求解：ticks 帧后命中目标点（带重力），水平/垂直分量封顶</summary>
		public static Vector2 SolveLob(Vector2 from, Vector2 to, float gravity, int ticks) {
			Vector2 d = to - from;
			float vx = MathHelper.Clamp(d.X / ticks, -14f, 14f);
			float vy = MathHelper.Clamp(d.Y / ticks - gravity * ticks * 0.5f, -18f, 12f);
			return new Vector2(vx, vy);
		}

		// ---------- 节奏调度器 ----------
		// 核心约束（5 秒 / 10 秒原则）：每一招都标成「演出招」或「衔接招」。
		//  · 衔接招永远不连续两次；只要池里有演出招，且距上一次演出招 ≥ SpectacleGap，就只从演出招里选；
		//  · 池子全空时不空转，而是用衔接招无视冷却把窗口填上——她永远在做事；
		//  · 同招不连发（候选 > 1 时剔除上一招）。
		// 结果：约每隔一招就是一次演出招，按 1~1.6 秒/招算，演出招间隔约 3 秒 → 5 秒内必有一次、10 秒内至少两次。
		private readonly struct AttackOption
		{
			public readonly int Id;
			public readonly int Weight;
			public readonly bool Spectacle;
			public readonly Func<IVaultState<WBoss>> Make;

			public AttackOption(int id, int weight, bool spectacle, Func<IVaultState<WBoss>> make) {
				Id = id;
				Weight = weight;
				Spectacle = spectacle;
				Make = make;
			}
		}

		private const int SpectacleGap = 150; // 2.5 秒没演出招就强制上演出招

		private readonly List<AttackOption> attackPool = new();

		/// <summary>距上一次演出招开始的帧数（AI 每帧 +1，选中演出招时归零）</summary>
		public int SinceSpectacle;
		/// <summary>距上一次任何攻击开始的帧数（调度层用来判断是否在空转）</summary>
		public int SinceAttack;
		private bool lastWasFiller;

		private void Add(int id, int weight, bool spectacle, Func<IVaultState<WBoss>> make) =>
			attackPool.Add(new AttackOption(id, weight, spectacle, make));

		private IVaultState<WBoss> RollAttackPool(Func<IVaultState<WBoss>> fallbackFiller) {
			// 反连发：候选大于 1 时剔除上一个技能
			if (attackPool.Count > 1)
				attackPool.RemoveAll(e => e.Id == LastAttackStateId);

			// 节奏门：有演出招可选时，衔接招不许连两次，也不许在演出欠账时插队
			bool anySpectacle = attackPool.Exists(e => e.Spectacle);
			if (anySpectacle && (lastWasFiller || SinceSpectacle >= SpectacleGap))
				attackPool.RemoveAll(e => !e.Spectacle);

			if (attackPool.Count == 0) {
				// 全在冷却：别站着——用衔接招填窗口（无视冷却），空转最多 20 帧
				if (SinceAttack >= 60 && fallbackFiller != null) {
					IVaultState<WBoss> filler = fallbackFiller();
					if (filler != null) {
						Commit(filler.StateId, spectacle: false);
						return filler;
					}
				}
				PickDelay = 8;
				return null;
			}

			int total = 0;
			foreach (AttackOption e in attackPool)
				total += e.Weight;
			int roll = Main.rand.Next(total);
			int acc = 0;
			foreach (AttackOption e in attackPool) {
				acc += e.Weight;
				if (roll < acc) {
					Commit(e.Id, e.Spectacle);
					return e.Make();
				}
			}
			return null;
		}

		private void Commit(int id, bool spectacle) {
			LastAttackStateId = id;
			lastWasFiller = !spectacle;
			SinceAttack = 0;
			if (spectacle)
				SinceSpectacle = 0;
		}

		/// <summary>状态之间直接连招（不经调度池）时登记一下，让节奏门知道刚刚打的是衔接招还是演出招</summary>
		public void NoteChained(int stateId, bool spectacle) => Commit(stateId, spectacle);

		/// <summary>D12 掷出 12 的兑现：无视冷却立刻接红桃K（有视线才兑现，否则留到下次）</summary>
		private IVaultState<WBoss> TryJackpot(bool los) {
			if (!JackpotPending || !los)
				return null;
			JackpotPending = false;
			Commit(4, spectacle: true);
			return new WKingHeartsState();
		}

		// 视线规则：只有直射弹（红桃K、地毯、满注）要实体视线；抛物线弹药、布雷、骰子、空中招一律不要——越过平台打人是它们的本职。
		// 净空：竞技场常有顶，空中招/烟火/曲射的净空要求压到 200~280px（12~17 格），有顶也能出。
		public IVaultState<WBoss> PickPhase1Attack(float dist, bool solidLos) {
			if (TryJackpot(solidLos) is IVaultState<WBoss> jackpot)
				return jackpot;
			attackPool.Clear();
			bool grounded = NPC.velocity.Y == 0f;
			// 演出招：从满血就有——密度是第一原则，扣押只留跳雷一项
			// 爆破跳：她全场最"人形"的一招，权重最高；要站在地上、头顶有空、玩家别贴脸也别太远
			if (CdBlastJumpLeft <= 0 && grounded && dist > 120f && dist < 600f && HasSkyClearance(200f))
				Add(24, 55, true, () => new WBlastJumpState());
			if (CdKingLeft <= 0 && solidLos)
				Add(4, 45, true, () => new WKingHeartsState());
			if (CdCountdownLeft <= 0 && dist < 520f)
				Add(5, 40, true, () => new WCountdownState());
			if (CdClusterLeft <= 0 && dist < 600f)
				Add(19, 45, true, () => new WClusterTossState());
			if (CdHopLeft <= 0 && grounded && dist > 200f && dist < 560f && (CanHopTo(1) || CanHopTo(-1)))
				Add(14, 40, true, () => new WHopTossState());
			if (CdJumpMineLeft <= 0 && dist < 600f && NPC.life <= NPC.lifeMax * TripleTossHpRatio)
				Add(20, 40, true, () => new WJumpMineTossState());
			// 烟火（手抛版）：一阶段就有满屏落雨
			if (CdFireworkLeft <= 0 && dist < 700f && HasSkyClearance(280f))
				Add(26, 45, true, () => new WFireworkState());
			// 衔接招：普攻手雷、三连投
			if (CdTripleLeft <= 0 && dist < 640f)
				Add(15, 25, false, () => new WTripleTossState());
			if (CdThrowLeft <= 0 && dist < 640f)
				Add(3, 30, false, () => new WThrowGrenadeState());
			return RollAttackPool(() => dist < 640f ? new WThrowGrenadeState() : null);
		}

		public IVaultState<WBoss> PickPhase2Attack(float dist, bool solidLos) {
			if (TryJackpot(solidLos) is IVaultState<WBoss> jackpot)
				return jackpot;
			// 核爆：首次 ≤40% 必定是下一招（只要玩家不在天边）；之后按冷却进池
			bool nukeReady = NukeUnlocked && CdNukeLeft <= 0 && dist < 900f;
			if (NukePending && nukeReady) {
				Commit(28, spectacle: true);
				return new WNukeState();
			}
			attackPool.Clear();
			if (nukeReady)
				Add(28, 65, true, () => new WNukeState());
			// 烟幕地狱：场上至少两团她自己的烟才值得点
			if (NPC.life <= NPC.lifeMax * DiceRainHpRatio && CdIgniteLeft <= 0 && CountSmokeClouds() >= 2)
				Add(29, 60, true, () => new WIgniteState());
			// 全场起爆：场上至少 2 颗已布设弹药才值得按这一下；雷越多权重越高
			int live = CdDetonateLeft <= 0 ? CountLiveOrdnance() : 0;
			if (live >= 2)
				Add(18, 45 + 20 * Math.Min(live, 4), true, () => new WDetonateAllState());
			// 满注（≤25%，双管）：全场最重的一招，直射弹要实体视线，不贴脸才押
			if (Desperate && CdAllInLeft <= 0 && solidLos && dist > 200f && dist < 760f)
				Add(23, 70, true, () => new WAllInKingState());
			bool grounded = NPC.velocity.Y == 0f;
			// 二阶段中段解锁：骰子雨、烟带撒雷、曲射、凌空扫射、烟火
			bool midP2 = NPC.life <= NPC.lifeMax * DiceRainHpRatio;
			if (midP2 && CdDiceRainLeft <= 0 && dist < 560f)
				Add(22, 45, true, () => new WDiceRainState());
			if (midP2 && CdMineRunLeft <= 0 && grounded && dist > 160f && dist < 600f)
				Add(21, 50, true, () => new WMineRunState());
			if (midP2 && CdAirStrafeLeft <= 0 && grounded && dist < 560f && HasSkyClearance(220f))
				Add(25, 55, true, () => new WAirStrafeState());
			if (midP2 && CdFireworkLeft <= 0 && dist < 700f && HasSkyClearance(280f))
				Add(26, 55, true, () => new WFireworkState());
			if (CdMortarLeft <= 0 && dist < 900f && NPC.life <= NPC.lifeMax * MortarHpRatio && HasSkyClearance(260f))
				Add(17, 50, true, () => new WMortarState());
			// 二阶段常驻演出招：爆破跳、地毯轰炸、此面向敌、D12、红桃K
			if (CdBlastJumpLeft <= 0 && grounded && dist > 120f && dist < 600f && HasSkyClearance(200f))
				Add(24, 55, true, () => new WBlastJumpState());
			if (CdCarpetLeft <= 0 && grounded && solidLos && dist > 250f && dist < 640f)
				Add(27, 50, true, () => new WCarpetState());
			if (CdClaymoreLeft <= 0 && dist < 600f)
				Add(10, 50, true, () => new WClaymoreState());
			if (CdD12Left <= 0 && dist < 700f)
				Add(11, 45, true, () => new WD12ThrowState());
			if (CdKingLeft <= 0 && solidLos)
				Add(4, 45, true, () => new WKingHeartsState());
			// 衔接招：三连发、单发（都是抛物线弹，不要视线）
			if (CdBurstLeft <= 0 && dist < 760f)
				Add(16, 30, false, () => new WBurstShotState());
			if (CdShotLeft <= 0 && dist < 760f)
				Add(9, 25, false, () => new WLauncherShotState());
			return RollAttackPool(() => dist < 760f ? new WBurstShotState() : null);
		}
	}

	// ---------- 0 登场 ----------
	// 骰子路径（召唤物）：W 已站在骰子炸出的烟团里 → 肩扛发射器走出来 → 站定看你 → 卡牌飘落 + 名牌 → 收枪 → 行走
	// 通用路径（其他生成方式）：原生刷怪点在屏外，开场传到玩家身侧，烟先到、人后到、再静场一拍
	[VaultState(0, typeof(WBoss))]
	public class WSpawnState : VaultState<WBoss>
	{
		// 通用路径
		private const int BurstTick = 30;   // 落点烟雾炸开
		private const int RevealEnd = 48;   // 显形完成，开始可被攻击
		private const int EndTick = 96;     // 静场结束
		// 骰子路径
		private const int DieWalkStart = 18, DieWalkEnd = 60, DieCardTick = 72, DieStowTick = 100, DieEndTick = 120;

		private int walkDir;

		public override void OnEnter(VaultStateMachine<WBoss> machine, WBoss ctx) {
			base.OnEnter(machine, ctx);
			NPC npc = ctx.NPC;
			ctx.ShowLauncher = false;
			npc.alpha = 255;
			npc.dontTakeDamage = true;
			npc.damage = 0;
			if (ctx.SpawnedFromDie) {
				// 从烟里朝玩家走出来；骰子扔得太近就退着出来
				Player target = ctx.Target;
				int toward = Math.Sign(target.Center.X - npc.Center.X);
				if (toward == 0)
					toward = 1;
				walkDir = Vector2.Distance(npc.Center, target.Center) < 160f ? -toward : toward;
				return;
			}
			if (ctx.IsAuthority) {
				ctx.TeleportDest = ctx.FindTeleportDest(ctx.Target, WBoss.TeleportKind.Entrance);
				npc.Center = ctx.TeleportDest;
				npc.velocity = Vector2.Zero;
				npc.netUpdate = true;
			}
		}

		public override IVaultState<WBoss> OnUpdate(VaultStateMachine<WBoss> machine, WBoss ctx) {
			base.OnUpdate(machine, ctx);
			NPC npc = ctx.NPC;
			npc.damage = 0;
			if (ctx.SpawnedFromDie)
				return UpdateFromDie(ctx, npc);

			npc.velocity.X *= 0.8f;
			if (Timer < BurstTick) {
				// 烟先到：落点慢慢冒烟、红光渐亮，玩家先看见"有东西要来"
				npc.alpha = 255;
				npc.dontTakeDamage = true;
				ctx.AnimOverrideRow = WBoss.RowSmokeEnd;
				if (!Main.dedServ) {
					if (Timer % 2 == 0) {
						Dust d = Dust.NewDustPerfect(npc.Bottom + new Vector2(Main.rand.NextFloat(-14f, 14f), 0f), DustID.Smoke,
							new Vector2(Main.rand.NextFloat(-0.3f, 0.3f), Main.rand.NextFloat(-1.6f, -0.6f)), 140, default, Main.rand.NextFloat(0.9f, 1.4f));
						d.color = Color.Lerp(new Color(90, 84, 84), new Color(190, 60, 50), Main.rand.NextFloat(0.2f, 0.8f));
						d.noGravity = true;
					}
					Lighting.AddLight(npc.Center, 0.7f * Timer / BurstTick, 0.12f, 0.08f);
				}
			}
			if (Timer == BurstTick) {
				WBoss.SmokeBurst(npc.Center, 24, 3.2f);
				WBoss.ShakeNearby(npc.Center, 5);
				SoundEngine.PlaySound(SoundID.Item66, npc.Center);
			}
			if (Timer >= BurstTick && Timer < RevealEnd) {
				// 从烟里显形：透明度回落 + 36→34 帧（抬眼看向玩家）
				float k = (Timer - BurstTick) / (float)(RevealEnd - BurstTick);
				npc.alpha = (int)MathHelper.Lerp(255f, 0f, k);
				ctx.AnimOverrideRow = WBoss.RowSmokeStart + 7 - Math.Min((Timer - BurstTick) / WBoss.TicksPerFrame, 2);
			}
			if (Timer >= RevealEnd) {
				// 静场：站着不动看你。威压不等于无敌，这段可以被打
				npc.alpha = 0;
				npc.dontTakeDamage = false;
				ctx.AnimOverrideRow = WBoss.RowWalkStart;
			}
			if (Timer == RevealEnd + 12)
				Introduce(ctx, npc);

			if (Timer >= EndTick && ctx.IsAuthority)
				return new WWalkState();
			return null;
		}

		private IVaultState<WBoss> UpdateFromDie(WBoss ctx, NPC npc) {
			if (Timer < DieWalkStart) {
				// 还在烟里
				npc.alpha = 255;
				npc.dontTakeDamage = true;
				npc.velocity.X = 0f;
				ctx.AnimOverrideRow = WBoss.RowWalkStart;
			}
			else if (Timer < DieWalkEnd) {
				// 肩扛发射器从烟里走出来：边走边显形
				npc.alpha = (int)MathHelper.Lerp(255f, 0f, MathHelper.Clamp((Timer - DieWalkStart) / 18f, 0f, 1f));
				npc.dontTakeDamage = Timer < DieWalkStart + 22;
				npc.velocity.X = walkDir * 1.5f;
				npc.spriteDirection = walkDir > 0 ? -1 : 1;
				ctx.ShowLauncher = true;
				ctx.AimOverride = -MathHelper.PiOver2 + npc.spriteDirection * 0.65f; // 扛在肩后
				ctx.AnimOverrideRow = -1; // 行走循环
			}
			else {
				// 站定，转头看你
				npc.alpha = 0;
				npc.dontTakeDamage = false;
				npc.velocity.X *= 0.7f;
				ctx.AnimOverrideRow = WBoss.RowWalkStart;
				if (Timer == DieCardTick)
					Introduce(ctx, npc);
				if (Timer == DieStowTick) {
					// 收枪
					ctx.ShowLauncher = false;
					ctx.AimOverride = null;
					WBoss.SmokeBurst(ctx.LauncherAnchor, 5, 1.2f);
					SoundEngine.PlaySound(SoundID.Unlock with { Volume = 0.5f, Pitch = -0.1f }, npc.Center);
				}
			}

			if (Timer >= DieEndTick && ctx.IsAuthority)
				return new WWalkState();
			return null;
		}

		/// <summary>亮相拍点：一张 ♥K 从指间弹出飘落 + 名牌 + 第一句台词</summary>
		private static void Introduce(WBoss ctx, NPC npc) {
			if (!Main.dedServ) {
				Vector2 hand = npc.Center + new Vector2(-npc.spriteDirection * 10f, -8f);
				new WCardParticle(hand, new Vector2(-npc.spriteDirection * 1.4f, -2.6f)).Spawn();
				SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.7f, Pitch = 0.4f }, npc.Center);
				WBattleVisuals.ShowNameplate();
			}
			if (ctx.IsAuthority)
				WBattleVisuals.Say("Spawn");
		}

		public override void OnExit(VaultStateMachine<WBoss> machine, WBoss ctx) {
			NPC npc = ctx.NPC;
			npc.alpha = 0;
			npc.dontTakeDamage = false;
			npc.damage = npc.defDamage;
			ctx.ShowLauncher = false;
			ctx.AimOverride = null;
			ctx.AnimOverrideRow = -1;
			ctx.PickDelay = 20;
		}
	}

	// ---------- 1 一阶段行走保距（类似哥布林弓箭手）+ 技能调度 ----------
	[VaultState(1, typeof(WBoss))]
	public class WWalkState : VaultState<WBoss>
	{
		private const float BandNear = 180f; // 近于此距离后拉开
		private const float BandFar = 420f;  // 远于此距离则接近

		public override IVaultState<WBoss> OnUpdate(VaultStateMachine<WBoss> machine, WBoss ctx) {
			base.OnUpdate(machine, ctx);
			ctx.NPC.damage = ctx.NPC.defDamage;
			ctx.ShowLauncher = false;
			Player target = ctx.Target;

			float dx = target.Center.X - ctx.NPC.Center.X;
			float absDx = Math.Abs(dx);
			int wantDir = 0;
			if (absDx > BandFar)
				wantDir = Math.Sign(dx);
			else if (absDx < BandNear)
				wantDir = -Math.Sign(dx);
			ctx.Move_WalkTick(target, wantDir);

			// 空中用跳跃帧，地面走行走循环（FindFrame 会在站定时锁首帧、按速度调步频）；跑起来带一点残影
			ctx.AnimOverrideRow = ctx.NPC.velocity.Y != 0f ? WBoss.RowJump : -1;
			if (Math.Abs(ctx.NPC.velocity.X) > 2.2f)
				ctx.GhostTrail = Math.Max(ctx.GhostTrail, 0.45f);

			if (!ctx.IsAuthority || ctx.PickDelay > 0)
				return null;

			// 卡地形看门狗：长时间原地打转 → 强制烟雾传送脱困（绕过冷却）
			if (ctx.StuckTicks > 60) {
				ctx.StuckTicks = 0;
				ctx.TeleportMode = WBoss.TeleportKind.Approach;
				return new WSmokeTeleportState();
			}

			float dist = Vector2.Distance(ctx.NPC.Center, target.Center);

			// 条件触发优先：被墙挡住 / 距离过远 / 玩家躲在上方平台且她两秒没出招 → 烟雾传送（CD 3s）
			if (ctx.CdTeleportLeft <= 0 && ctx.WantsRepositionTeleport(target, dist, 560f)) {
				ctx.TeleportMode = WBoss.TeleportKind.Approach;
				return new WSmokeTeleportState();
			}

			return ctx.PickPhase1Attack(dist, ctx.HasSolidLineOfSight(target));
		}

		public override void OnExit(VaultStateMachine<WBoss> machine, WBoss ctx) {
			ctx.AnimOverrideRow = -1;
		}
	}

	// ---------- 2 烟雾传送（封烟跑路 29-39：烟雾弹飞出→炸烟→人在烟里渐隐→一道烟带贴地冲到落点→现身） ----------
	[VaultState(2, typeof(WBoss))]
	public class WSmokeTeleportState : VaultState<WBoss>
	{
		private const int VanishTick = 60;   // 行 38（近全透明帧）起点：烟带出发
		private const int ReappearDelay = 6; // 烟带到点后停一拍再显形
		private const int HoldAfter = 18;

		private int arriveTick = -1;

		public override void OnEnter(VaultStateMachine<WBoss> machine, WBoss ctx) {
			base.OnEnter(machine, ctx);
			ctx.CdTeleportLeft = WBoss.CdTeleport;
			ctx.ShowLauncher = false;
			ctx.TelegraphLaser = 0f;
			arriveTick = -1;
			if (ctx.IsAuthority) {
				ctx.TeleportDest = ctx.FindTeleportDest(ctx.Target, ctx.TeleportMode);
				ctx.NPC.netUpdate = true; // 连带 SendExtraAI 同步 TeleportDest，客户端好画落点预告
			}
		}

		public override IVaultState<WBoss> OnUpdate(VaultStateMachine<WBoss> machine, WBoss ctx) {
			base.OnUpdate(machine, ctx);
			NPC npc = ctx.NPC;
			npc.damage = 0;

			if (Timer < VanishTick) {
				npc.velocity.X *= 0.8f;
				ctx.AnimOverrideRow = WBoss.SmokeRow(Timer);
				ctx.SmokeGrenadeBeat(Timer);
				// 落点预告：出发前在目的地起烟 + 红光（公平阀）
				if (Timer == VanishTick - 12)
					WBoss.SmokeBurst(ctx.TeleportDest, 12, 1.8f);
				// 隐身前：叠一层渐隐 + 一团遮掩烟，别让人"啪"一下没了
				if (Timer >= VanishTick - 8)
					npc.alpha = (int)MathHelper.Lerp(0f, 255f, (Timer - (VanishTick - 8)) / 8f);
				if (Timer == VanishTick - 2)
					WBoss.SmokeBurst(npc.Center, 14, 2.2f);
				return null;
			}
			if (!Main.dedServ && arriveTick < 0)
				Lighting.AddLight(ctx.TeleportDest, 0.5f, 0.1f, 0.08f);

			if (Timer == VanishTick) {
				// 二阶段的换位有四成机会留个假身
				if (ctx.IsAuthority && ctx.Phase == 2 && Main.rand.NextFloat() < 0.4f)
					ctx.SpawnDecoy();
				ctx.Dash_Begin();
			}
			if (ctx.Dashing) {
				ctx.AnimOverrideRow = WBoss.RowSmokeEnd;
				if (ctx.Dash_Tick())
					arriveTick = Timer;
				return null;
			}
			if (arriveTick < 0)
				arriveTick = Timer; // 兜底：客户端错过出发帧

			int since = Timer - arriveTick;
			if (since < ReappearDelay) {
				npc.alpha = 255;
				ctx.AnimOverrideRow = WBoss.RowSmokeEnd;
				if (since == 0)
					WBoss.SmokeBurst(npc.Center, 14);
				return null;
			}
			int shown = since - ReappearDelay;
			npc.alpha = (int)MathHelper.Lerp(255f, 0f, Math.Min(shown / 8f, 1f));
			// 从烟里出来先回头（35→34 帧），再落回站姿
			ctx.AnimOverrideRow = shown < 12 ? WBoss.RowSmokeStart + 6 - shown / WBoss.TicksPerFrame : WBoss.RowWalkStart;

			if (shown >= HoldAfter && ctx.IsAuthority) {
				// 到位即开火：换位本身就是前摇，落地就直接接一记衔接招（抛物线弹，不需要视线），别再站着等
				Player target = ctx.Target;
				if (!target.dead && target.active) {
					float dist = Vector2.Distance(npc.Center, target.Center);
					if (ctx.Phase == 2 && dist > 180f) {
						ctx.NoteChained(16, spectacle: false);
						return new WBurstShotState();
					}
					if (ctx.Phase == 1 && dist > 160f && dist < 620f) {
						ctx.NoteChained(3, spectacle: false);
						return new WThrowGrenadeState();
					}
				}
				return ctx.SchedulerState();
			}
			return null;
		}

		public override void OnExit(VaultStateMachine<WBoss> machine, WBoss ctx) {
			if (ctx.Dashing)
				ctx.Dash_End();
			ctx.NPC.alpha = 0;
			ctx.NPC.damage = ctx.NPC.defDamage;
			ctx.AnimOverrideRow = -1;
			ctx.PickDelay = 12;
		}
	}

	// ---------- 3 一阶段普攻：抛雷引爆（21-28，手雷 23~24 帧出手）；三连投复用此动作 ----------
	[VaultState(3, typeof(WBoss))]
	public class WThrowGrenadeState : VaultState<WBoss>
	{
		protected const int ReleaseTick = 18; // 行 23 起点：出手帧
		protected const int EndTick = 60;

		public override void OnEnter(VaultStateMachine<WBoss> machine, WBoss ctx) {
			base.OnEnter(machine, ctx);
			ArmCooldown(ctx);
			ctx.ShowLauncher = false;
		}

		protected virtual void ArmCooldown(WBoss ctx) => ctx.CdThrowLeft = WBoss.CdThrow;

		/// <summary>出手瞬间（仅服务端/单机端）</summary>
		protected virtual void Release(WBoss ctx, Vector2 hand) {
			Vector2 aim = WBoss.AimPoint(ctx.Target, 40);
			Vector2 vel = WBoss.SolveLob(hand, aim, 0.25f, 40);
			Projectile.NewProjectile(ctx.NPC.GetSource_FromAI(), hand, vel,
				ModContent.ProjectileType<WHEGrenade>(),
				(int)(WBoss.DmgHEGrenade * ctx.CurrentDamageScale), 1f, Main.myPlayer);
		}

		protected virtual float ReleasePitch => 0f;

		public override IVaultState<WBoss> OnUpdate(VaultStateMachine<WBoss> machine, WBoss ctx) {
			base.OnUpdate(machine, ctx);
			NPC npc = ctx.NPC;
			npc.velocity.X *= 0.85f;
			ctx.AnimOverrideRow = Math.Min(WBoss.RowThrowStart + Timer / WBoss.TicksPerFrame, WBoss.RowThrowEnd);

			if (Timer == ReleaseTick) {
				SoundEngine.PlaySound(SoundID.Item1 with { Pitch = ReleasePitch }, npc.Center);
				if (ctx.IsAuthority)
					Release(ctx, npc.Center + new Vector2(-npc.spriteDirection * 10f, -10f));
			}

			if (Timer >= EndTick && ctx.IsAuthority)
				return ctx.SchedulerState();
			return null;
		}

		public override void OnExit(VaultStateMachine<WBoss> machine, WBoss ctx) {
			ctx.AnimOverrideRow = -1;
			ctx.PickDelay = Main.rand.Next(8, 16);
		}
	}

	// ---------- 4 技能1 红桃K：举臂瞄准（激光压上来）→ 收线一拍 → 发射器快弹（雷管范围，200% + 混乱） ----------
	[VaultState(4, typeof(WBoss))]
	public class WKingHeartsState : VaultState<WBoss>
	{
		private const int FireTick = 30; // 前 30 tick 为可读前摇（激光 + 两声蜂鸣）
		private const int EndTick = 54;

		public override void OnEnter(VaultStateMachine<WBoss> machine, WBoss ctx) {
			base.OnEnter(machine, ctx);
			ctx.CdKingLeft = WBoss.CdKing;
			ctx.ShowLauncher = true;
			SoundEngine.PlaySound(SoundID.MaxMana with { Pitch = -0.4f }, ctx.NPC.Center);
		}

		public override IVaultState<WBoss> OnUpdate(VaultStateMachine<WBoss> machine, WBoss ctx) {
			base.OnUpdate(machine, ctx);
			NPC npc = ctx.NPC;
			npc.velocity.X *= 0.8f;
			ctx.AnimOverrideRow = WBoss.SwingRow(Timer, FireTick);

			// 激光按 1.6 次幂压上来；开火前 3 tick 收线——静默一拍再炸
			if (Timer < FireTick - 3)
				ctx.TelegraphLaser = Math.Max(ctx.TelegraphLaser * 0.9f, MathF.Pow(Timer / (float)FireTick, 1.6f));
			else
				ctx.TelegraphLaser = 0f;
			if (Timer == 12 || Timer == 22)
				SoundEngine.PlaySound(SoundID.MaxMana with { Pitch = Timer == 12 ? -0.1f : 0.25f, Volume = 0.7f }, npc.Center);

			// 翻牌：前摇里牌从背面慢慢转到侧立，开火那一帧"啪"地翻出红 K，随后淡出
			ctx.CardAlpha = Timer < FireTick + 14 ? Math.Min(Timer / 8f, 1f) : Math.Max(0f, 1f - (Timer - FireTick - 14) / 10f);
			ctx.CardFlip = Timer < FireTick ? 0.5f * Timer / FireTick : 0.5f + 0.5f * Math.Min((Timer - FireTick) / 5f, 1f);

			if (Timer == FireTick) {
				SoundEngine.PlaySound(SoundID.Item61, npc.Center);
				SoundEngine.PlaySound(SoundID.MenuTick with { Pitch = 0.6f }, npc.Center);
				ctx.MuzzleFire(2f);
				WBoss.ShakeNearby(npc.Center, 3, 600f);
				if (ctx.IsAuthority) {
					Vector2 muzzle = ctx.MuzzlePos;
					Vector2 aim = WBoss.AimPoint(ctx.Target, 24);
					Vector2 vel = (aim - muzzle).SafeNormalize(Vector2.UnitX * -npc.spriteDirection) * 16f;
					Projectile.NewProjectile(npc.GetSource_FromAI(), muzzle, vel,
						ModContent.ProjectileType<WLipstickRound>(),
						(int)(WBoss.DmgKingRound * ctx.CurrentDamageScale), 2f, Main.myPlayer,
						ai0: 0f);
					// 二阶段被动[烟幕射击]：每次红桃K后原地留烟（boss 战期间不消散）
					if (ctx.Phase == 2) {
						Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero,
							ModContent.ProjectileType<WSmokeCloud>(), 0, 0f, Main.myPlayer,
							ai0: npc.whoAmI, ai1: ctx.SmokeCloudRadius);
					}
				}
				if (ctx.Phase == 2)
					WBoss.SmokeBurst(npc.Center, 12, 2f); // 烟团落地的那一下
			}

			if (Timer >= EndTick && ctx.IsAuthority)
				return ctx.SchedulerState();
			return null;
		}

		public override void OnExit(VaultStateMachine<WBoss> machine, WBoss ctx) {
			ctx.ShowLauncher = ctx.Phase == 2;
			ctx.AnimOverrideRow = -1;
			ctx.TelegraphLaser = 0f;
			ctx.PickDelay = Main.rand.Next(8, 16);
		}
	}

	// ---------- 19 集束雷（一阶段 ≤70%）：一记抛雷，母弹在玩家头顶的抛物线顶点炸成五颗子弹落下 ----------
	[VaultState(19, typeof(WBoss))]
	public class WClusterTossState : WThrowGrenadeState
	{
		private const int ApexTicks = 40; // 顶点时间：抛得高，顶点正好落在玩家头顶

		protected override void ArmCooldown(WBoss ctx) {
			ctx.CdClusterLeft = WBoss.CdCluster;
			ctx.CdThrowLeft = Math.Max(ctx.CdThrowLeft, 60);
		}

		protected override float ReleasePitch => -0.3f;

		protected override void Release(WBoss ctx, Vector2 hand) {
			// 让顶点的横坐标落在玩家头顶：vy 决定顶点时间，vx 按同样时间走完横向距离
			Vector2 aim = WBoss.AimPoint(ctx.Target, ApexTicks);
			float vy = -0.25f * ApexTicks;
			float vx = MathHelper.Clamp((aim.X - hand.X) / ApexTicks, -14f, 14f);
			Projectile.NewProjectile(ctx.NPC.GetSource_FromAI(), hand, new Vector2(vx, vy),
				ModContent.ProjectileType<WHEGrenade>(),
				(int)(WBoss.DmgHEGrenade * ctx.CurrentDamageScale), 1f, Main.myPlayer, ai0: 1f);
		}
	}

	// ---------- 20 跳雷（一阶段 ≤70%）：一记抛雷，两颗跳雷落到玩家附近的空地布设 ----------
	[VaultState(20, typeof(WBoss))]
	public class WJumpMineTossState : WThrowGrenadeState
	{
		protected override void ArmCooldown(WBoss ctx) {
			ctx.CdJumpMineLeft = WBoss.CdJumpMine;
			ctx.CdThrowLeft = Math.Max(ctx.CdThrowLeft, 60);
		}

		protected override float ReleasePitch => -0.1f;

		protected override void Release(WBoss ctx, Vector2 hand) {
			foreach (Vector2 spot in ctx.FindClaymoreSpots(ctx.Target, 2)) {
				int ticks = (int)MathHelper.Clamp(Vector2.Distance(hand, spot) / 9f, 30f, 60f);
				Vector2 vel = WBoss.SolveLob(hand, spot, 0.3f, ticks);
				Projectile.NewProjectile(ctx.NPC.GetSource_FromAI(), hand, vel,
					ModContent.ProjectileType<WJumpMine>(),
					(int)(WBoss.DmgHEGrenade * ctx.CurrentDamageScale), 1f, Main.myPlayer,
					ai2: ctx.NPC.whoAmI + 1);
			}
		}
	}

	// ---------- 5 倒数计时（47-64 背身装定，60-62 帧闪光）→ 掷出一阶段 D12 ----------
	[VaultState(5, typeof(WBoss))]
	public class WCountdownState : VaultState<WBoss>
	{
		private const int FrameHold = 5;             // 快放一档：背身装定从 2s 压到 1.7s，仍然读得清
		private const int FlashStart = 13 * FrameHold; // 行 59（第一张闪光帧）= 65
		private const int ThrowTick = 17 * FrameHold;  // 行 63（转回身）= 85
		private const int EndTick = 100;

		public override void OnEnter(VaultStateMachine<WBoss> machine, WBoss ctx) {
			base.OnEnter(machine, ctx);
			ctx.CdCountdownLeft = WBoss.CdCountdown;
			ctx.ShowLauncher = false;
		}

		public override IVaultState<WBoss> OnUpdate(VaultStateMachine<WBoss> machine, WBoss ctx) {
			base.OnUpdate(machine, ctx);
			NPC npc = ctx.NPC;
			npc.velocity.X *= 0.8f;
			ctx.AnimOverrideRow = Math.Min(WBoss.RowCountdownStart + Timer / FrameHold, WBoss.RowCountdownEnd);

			// 52 帧：装定引信的一声机械咔哒
			if (Timer == 5 * FrameHold)
				SoundEngine.PlaySound(SoundID.Unlock with { Volume = 0.6f, Pitch = 0.2f }, npc.Center);

			// 60-62 帧（帧图自带橙色环闪）配升调蜂鸣 + 橙光
			if (Timer == FlashStart || Timer == FlashStart + FrameHold || Timer == FlashStart + 2 * FrameHold)
				SoundEngine.PlaySound(SoundID.MaxMana with { Pitch = (Timer - FlashStart) / (2f * FrameHold) * 0.8f - 0.2f }, npc.Center);
			if (Timer >= FlashStart && Timer < FlashStart + 3 * FrameHold && !Main.dedServ)
				Lighting.AddLight(npc.Center + new Vector2(-npc.spriteDirection * 6f, -4f), 1.0f, 0.55f, 0.15f);

			if (Timer == ThrowTick) {
				SoundEngine.PlaySound(SoundID.Item1 with { Pitch = -0.1f }, npc.Center);
				if (ctx.IsAuthority) {
					Vector2 from = npc.Center + new Vector2(-npc.spriteDirection * 8f, -12f);
					Vector2 aim = WBoss.AimPoint(ctx.Target, 50);
					Vector2 vel = WBoss.SolveLob(from, aim, 0.2f, 50);
					Projectile.NewProjectile(npc.GetSource_FromAI(), from, vel,
						ModContent.ProjectileType<WD12>(),
						(int)(WBoss.DmgD12 * ctx.CurrentDamageScale), 2f, Main.myPlayer,
						ai0: 0f, ai1: -1f);
				}
			}

			if (Timer >= EndTick && ctx.IsAuthority)
				return ctx.SchedulerState();
			return null;
		}

		public override void OnExit(VaultStateMachine<WBoss> machine, WBoss ctx) {
			ctx.AnimOverrideRow = -1;
			ctx.PickDelay = Main.rand.Next(10, 20);
		}
	}

	// ---------- 6 二阶段转场：怒吼定格一拍 → 封烟跑路 → 烟里现身时已持发射器 → 激光压上玩家 → 直接接红桃K ----------
	[VaultState(6, typeof(WBoss))]
	public class WPhaseTransitState : VaultState<WBoss>
	{
		private const int StillEnd = 24;                 // 定格
		private const int SmokeStart = StillEnd;         // 封烟动作起点（内部计时 t = Timer - SmokeStart）
		private const int VanishTick = SmokeStart + 60;  // 84：烟带出发
		private const int ReappearDelay = 8;
		private const int HoldAfter = 22;

		private int arriveTick = -1;

		public override void OnEnter(VaultStateMachine<WBoss> machine, WBoss ctx) {
			base.OnEnter(machine, ctx);
			NPC npc = ctx.NPC;
			ctx.Phase = 2;
			ctx.ShowLauncher = false;
			ctx.TelegraphLaser = 0f;
			arriveTick = -1;
			npc.dontTakeDamage = true; // 转场演出期间免伤（公平阀，双向）
			npc.damage = 0;
			if (ctx.IsAuthority) {
				ctx.TeleportMode = WBoss.TeleportKind.DodgeAway;
				ctx.TeleportDest = ctx.FindTeleportDest(ctx.Target, WBoss.TeleportKind.DodgeAway);
				npc.netUpdate = true;
				WBattleVisuals.Say("Phase2");
			}
			SoundEngine.PlaySound(SoundID.Roar with { Pitch = 0.35f }, npc.Center);
			WBoss.ShakeNearby(npc.Center, 6);
		}

		public override IVaultState<WBoss> OnUpdate(VaultStateMachine<WBoss> machine, WBoss ctx) {
			base.OnUpdate(machine, ctx);
			NPC npc = ctx.NPC;
			npc.damage = 0;

			if (Timer < StillEnd) {
				// 定格：怒吼后站着不动，红光在身上聚起来
				npc.velocity.X *= 0.6f;
				ctx.AnimOverrideRow = WBoss.RowWalkStart;
				if (Timer == 2)
					WBoss.SmokeBurst(npc.Center, 6, 1.2f);
				if (!Main.dedServ)
					Lighting.AddLight(npc.Center, 0.9f * Timer / StillEnd, 0.15f, 0.1f);
				return null;
			}
			if (Timer < VanishTick) {
				npc.velocity.X *= 0.6f;
				int t = Timer - SmokeStart;
				ctx.AnimOverrideRow = WBoss.SmokeRow(t);
				ctx.SmokeGrenadeBeat(t);
				if (t == 30) {
					// 转场的烟比平时大一号
					WBoss.SmokeBurst(npc.Center, 14, 3.2f);
					WBoss.ShakeNearby(npc.Center, 8);
				}
				if (t == 48)
					WBoss.SmokeBurst(ctx.TeleportDest, 12, 1.8f); // 落点预告
				if (t >= 52)
					npc.alpha = (int)MathHelper.Lerp(0f, 255f, (t - 52) / 8f);
				if (t == 58)
					WBoss.SmokeBurst(npc.Center, 16, 2.4f);
				return null;
			}

			if (Timer == VanishTick)
				ctx.Dash_Begin(8, 24);
			if (ctx.Dashing) {
				ctx.AnimOverrideRow = WBoss.RowSmokeEnd;
				if (ctx.Dash_Tick())
					arriveTick = Timer;
				return null;
			}
			if (arriveTick < 0)
				arriveTick = Timer;

			int since = Timer - arriveTick;
			if (since < ReappearDelay) {
				npc.alpha = 255;
				ctx.AnimOverrideRow = WBoss.RowSmokeEnd;
				return null;
			}
			int shown = since - ReappearDelay;
			if (shown == 0) {
				// 亮相：烟里走出来的人已经端着发射器——机械咔哒一声
				WBoss.SmokeBurst(npc.Center, 18, 2.8f);
				SoundEngine.PlaySound(SoundID.Unlock with { Pitch = -0.2f }, npc.Center);
				ctx.ShowLauncher = true;
			}
			npc.alpha = (int)MathHelper.Lerp(255f, 0f, Math.Min(shown / 8f, 1f));
			ctx.AnimOverrideRow = WBoss.RowWalkStart;
			// 激光缓缓压到玩家身上：二阶段的语言先教一遍，随后红桃K 的前摇会接着它继续亮
			ctx.TelegraphLaser = MathHelper.Clamp((shown - 6) / 16f, 0f, 0.6f);

			if (shown >= HoldAfter && ctx.IsAuthority) {
				ctx.NoteChained(4, spectacle: true);
				return new WKingHeartsState();
			}
			return null;
		}

		public override void OnExit(VaultStateMachine<WBoss> machine, WBoss ctx) {
			if (ctx.Dashing)
				ctx.Dash_End();
			NPC npc = ctx.NPC;
			npc.alpha = 0;
			npc.dontTakeDamage = false;
			// 大师：二阶段基础攻击力提升至 150%
			npc.damage = (int)(npc.defDamage * ctx.P2DamageScale);
			ctx.ShowLauncher = true;
			ctx.AnimOverrideRow = -1;
			ctx.PickDelay = 0; // 出场第一枪紧接着来，激光已经给足预告
		}
	}

	// ---------- 7 二阶段站桩（不再行走，常态持发射器，枪口跟着玩家微晃）+ 调度 ----------
	[VaultState(7, typeof(WBoss))]
	public class WStandState : VaultState<WBoss>
	{
		public override IVaultState<WBoss> OnUpdate(VaultStateMachine<WBoss> machine, WBoss ctx) {
			base.OnUpdate(machine, ctx);
			NPC npc = ctx.NPC;
			npc.velocity.X *= 0.85f;
			npc.damage = (int)(npc.defDamage * ctx.P2DamageScale);
			ctx.ShowLauncher = true;
			ctx.AnimOverrideRow = npc.velocity.Y != 0f ? WBoss.RowJump : WBoss.RowWalkStart;

			if (!ctx.IsAuthority || ctx.PickDelay > 0)
				return null;

			Player target = ctx.Target;
			float dist = Vector2.Distance(npc.Center, target.Center);

			// 躲避机制：玩家过近 → 原地封烟反向传送（CD 10s），落地顺手一梭子
			if (ctx.CdDodgeLeft <= 0 && dist < 140f)
				return new WDodgeTeleportState();

			// 扔完核骰背着身的那段不出招：核爆、蘑菇云、落尘本身就是这段的内容
			if (ctx.FaceAwayTicks > 0)
				return null;

			// 被墙挡住 / 过远 / 玩家躲在上方平台且她两秒没出招 → 烟雾传送拉回射程
			if (ctx.CdTeleportLeft <= 0 && ctx.WantsRepositionTeleport(target, dist, 700f)) {
				ctx.TeleportMode = WBoss.TeleportKind.Approach;
				return new WSmokeTeleportState();
			}

			// 高台狙击：不在玩家上方时，偶尔主动换到头顶的平台去打（只在她刚打完一招的间隙里做，不抢出招窗口）
			if (ctx.CdPerchLeft <= 0 && ctx.CdTeleportLeft <= 0 && ctx.SinceAttack < 30 && npc.Bottom.Y > target.Center.Y - 60f && Main.rand.NextBool(3)) {
				ctx.CdPerchLeft = WBoss.CdPerch;
				ctx.TeleportMode = WBoss.TeleportKind.Perch;
				return new WSmokeTeleportState();
			}

			return ctx.PickPhase2Attack(dist, ctx.HasSolidLineOfSight(target));
		}

		public override void OnExit(VaultStateMachine<WBoss> machine, WBoss ctx) {
			ctx.AnimOverrideRow = -1;
		}
	}

	// ---------- 8 二阶段躲避传送（快放封烟，烟带反方向窜出）→ 现身即三连发反击 ----------
	[VaultState(8, typeof(WBoss))]
	public class WDodgeTeleportState : VaultState<WBoss>
	{
		private const int FrameHold = 4;   // 快放：4 tick/帧
		private const int VanishTick = 40; // 烟带出发
		private const int ReappearDelay = 4;
		private const int HoldAfter = 10;

		private int arriveTick = -1;

		public override void OnEnter(VaultStateMachine<WBoss> machine, WBoss ctx) {
			base.OnEnter(machine, ctx);
			ctx.CdDodgeLeft = WBoss.CdDodge;
			ctx.ShowLauncher = false;
			ctx.TelegraphLaser = 0f;
			arriveTick = -1;
			if (ctx.IsAuthority) {
				ctx.TeleportMode = WBoss.TeleportKind.DodgeAway;
				ctx.TeleportDest = ctx.FindTeleportDest(ctx.Target, WBoss.TeleportKind.DodgeAway);
				ctx.NPC.netUpdate = true;
			}
		}

		public override IVaultState<WBoss> OnUpdate(VaultStateMachine<WBoss> machine, WBoss ctx) {
			base.OnUpdate(machine, ctx);
			NPC npc = ctx.NPC;
			npc.damage = 0;

			if (Timer < VanishTick) {
				npc.velocity.X *= 0.8f;
				ctx.AnimOverrideRow = Math.Min(WBoss.RowSmokeStart + Timer / FrameHold, WBoss.RowSmokeEnd);
				// 快放版烟雾弹：12-20 飞出，20 炸
				if (!Main.dedServ && Timer >= 12 && Timer < 20) {
					Vector2 hand = npc.Center + new Vector2(-npc.spriteDirection * 6f, -14f);
					Vector2 land = npc.Center + new Vector2(-npc.spriteDirection * 24f, -38f);
					Dust d = Dust.NewDustPerfect(Vector2.Lerp(hand, land, (Timer - 12) / 8f), DustID.Smoke, new Vector2(0f, -0.4f), 150, default, 0.8f);
					d.noGravity = true;
				}
				if (Timer == 8) {
					// 贴脸的代价：脚下一颗闪光弹——白屏 + 推开，不伤人
					SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.7f, Pitch = 0.6f }, npc.Center);
					SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.6f, Pitch = 0.2f }, npc.Center);
					ctx.FlashbangNearby(320f, 7f);
					WBoss.SmokeBurst(npc.Bottom, 10, 2.4f);
					if (!Main.dedServ)
						Lighting.AddLight(npc.Center, 2f, 1.9f, 1.6f);
				}
				if (Timer == 20) {
					WBoss.SmokeBurst(npc.Center + new Vector2(-npc.spriteDirection * 24f, -38f), 16, 3f);
					WBoss.SmokeBurst(npc.Center, 8, 1.8f);
					SoundEngine.PlaySound(SoundID.Item66, npc.Center);
				}
				if (Timer >= VanishTick - 6)
					npc.alpha = (int)MathHelper.Lerp(0f, 255f, (Timer - (VanishTick - 6)) / 6f);
				if (Timer == VanishTick - 2)
					WBoss.SmokeBurst(npc.Center, 12, 2.2f);
				return null;
			}

			if (Timer == VanishTick) {
				// 烟里留一个"她"，真身窜走
				if (ctx.IsAuthority)
					ctx.SpawnDecoy();
				ctx.Dash_Begin(4, 12); // 躲避距离短，烟带更快
			}
			if (ctx.Dashing) {
				ctx.AnimOverrideRow = WBoss.RowSmokeEnd;
				if (ctx.Dash_Tick())
					arriveTick = Timer;
				return null;
			}
			if (arriveTick < 0)
				arriveTick = Timer;

			int since = Timer - arriveTick;
			if (since < ReappearDelay) {
				npc.alpha = 255;
				ctx.AnimOverrideRow = WBoss.RowSmokeEnd;
				if (since == 0)
					WBoss.SmokeBurst(npc.Center, 10);
				return null;
			}
			int shown = since - ReappearDelay;
			npc.alpha = (int)MathHelper.Lerp(255f, 0f, Math.Min(shown / 6f, 1f));
			ctx.AnimOverrideRow = WBoss.RowWalkStart;

			if (shown >= HoldAfter && ctx.IsAuthority) {
				// 反击：拉开距离后立刻还一梭子（刺客式换位惩罚；抛物线弹不需要视线）
				Player target = ctx.Target;
				if (!target.dead && target.active && Vector2.Distance(npc.Center, target.Center) > 200f) {
					ctx.NoteChained(16, spectacle: false);
					return new WBurstShotState();
				}
				return new WStandState();
			}
			return null;
		}

		public override void OnExit(VaultStateMachine<WBoss> machine, WBoss ctx) {
			if (ctx.Dashing)
				ctx.Dash_End();
			NPC npc = ctx.NPC;
			npc.alpha = 0;
			npc.damage = (int)(npc.defDamage * ctx.P2DamageScale);
			ctx.ShowLauncher = true;
			ctx.AnimOverrideRow = -1;
			ctx.PickDelay = 18;
		}
	}

	// ---------- 9 二阶段普攻：发射器抛物线口红弹（短前摇，开火帧对齐挥臂第 4 帧） ----------
	[VaultState(9, typeof(WBoss))]
	public class WLauncherShotState : VaultState<WBoss>
	{
		private const int FireTick = 12;
		private const int EndTick = 36;

		public override void OnEnter(VaultStateMachine<WBoss> machine, WBoss ctx) {
			base.OnEnter(machine, ctx);
			ctx.CdShotLeft = WBoss.CdShot;
			ctx.ShowLauncher = true;
		}

		public override IVaultState<WBoss> OnUpdate(VaultStateMachine<WBoss> machine, WBoss ctx) {
			base.OnUpdate(machine, ctx);
			NPC npc = ctx.NPC;
			npc.velocity.X *= 0.85f;
			ctx.AnimOverrideRow = WBoss.SwingRow(Timer, FireTick);
			ctx.TelegraphLaser = Timer < FireTick - 2 ? 0.35f * Timer / FireTick : 0f;

			if (Timer == FireTick) {
				SoundEngine.PlaySound(SoundID.Item61 with { Pitch = 0.1f }, npc.Center);
				ctx.MuzzleFire(1f);
				if (ctx.IsAuthority) {
					Vector2 muzzle = ctx.MuzzlePos;
					Vector2 aim = WBoss.AimPoint(ctx.Target, 34);
					Vector2 vel = WBoss.SolveLob(muzzle, aim, 0.2f, 34);
					Projectile.NewProjectile(npc.GetSource_FromAI(), muzzle, vel,
						ModContent.ProjectileType<WLipstickRound>(),
						(int)(WBoss.DmgArcRound * ctx.CurrentDamageScale), 1f, Main.myPlayer,
						ai0: 1f);
				}
			}

			if (Timer >= EndTick && ctx.IsAuthority)
				return new WStandState();
			return null;
		}

		public override void OnExit(VaultStateMachine<WBoss> machine, WBoss ctx) {
			ctx.AnimOverrideRow = -1;
			ctx.TelegraphLaser = 0f;
			ctx.PickDelay = Main.rand.Next(8, 16);
		}
	}

	// ---------- 10 技能2 此面向敌：从手里把 3 颗阔剑雷抛向玩家附近的空地（29-36 那颗飞出去的东西就是它） ----------
	[VaultState(10, typeof(WBoss))]
	public class WClaymoreState : VaultState<WBoss>
	{
		private const int DeployTick = 24;
		private const int EndTick = 60;

		public override void OnEnter(VaultStateMachine<WBoss> machine, WBoss ctx) {
			base.OnEnter(machine, ctx);
			ctx.CdClaymoreLeft = WBoss.CdClaymore;
			ctx.ShowLauncher = false;
			ctx.TelegraphLaser = 0f;
		}

		public override IVaultState<WBoss> OnUpdate(VaultStateMachine<WBoss> machine, WBoss ctx) {
			base.OnUpdate(machine, ctx);
			NPC npc = ctx.NPC;
			npc.velocity.X *= 0.85f;
			// 策划帧序：29-36 接 27-28（0 基 28..35 → 26..27）
			ctx.AnimOverrideRow = Timer < 48
				? Math.Min(WBoss.RowSmokeStart + Timer / WBoss.TicksPerFrame, 35)
				: 26 + Math.Min((Timer - 48) / WBoss.TicksPerFrame, 1);

			if (Timer == DeployTick) {
				SoundEngine.PlaySound(SoundID.Item1 with { Pitch = -0.15f }, npc.Center);
				if (ctx.IsAuthority) {
					Vector2 hand = npc.Center + new Vector2(-npc.spriteDirection * 8f, -16f);
					List<Vector2> spots = ctx.FindClaymoreSpots(ctx.Target, 3);
					foreach (Vector2 spot in spots) {
						// 真抛出去：飞行时间随距离 30~60 tick，重力与 WClaymore 的下落重力一致
						int ticks = (int)MathHelper.Clamp(Vector2.Distance(hand, spot) / 9f, 30f, 60f);
						Vector2 vel = WBoss.SolveLob(hand, spot, 0.3f, ticks);
						// 雷面朝布设时玩家所在的那一侧（符号编码进 ai2，绝对值是归属）
						int facing = Math.Sign(ctx.Target.Center.X - spot.X);
						if (facing == 0)
							facing = -npc.spriteDirection;
						Projectile.NewProjectile(npc.GetSource_FromAI(), hand, vel,
							ModContent.ProjectileType<WClaymore>(),
							(int)(WBoss.DmgClaymore * ctx.CurrentDamageScale), 2f, Main.myPlayer,
							ai2: (npc.whoAmI + 1) * facing);
					}
				}
			}

			if (Timer >= EndTick && ctx.IsAuthority)
				return new WStandState();
			return null;
		}

		public override void OnExit(VaultStateMachine<WBoss> machine, WBoss ctx) {
			ctx.ShowLauncher = ctx.Phase == 2;
			ctx.AnimOverrideRow = -1;
			ctx.PickDelay = Main.rand.Next(10, 18);
		}
	}

	// ---------- 11 技能3 D12：切向加速大骰子（更远黏附/落地成雷）；71 帧手里的骰子亮起来再掷出 ----------
	[VaultState(11, typeof(WBoss))]
	public class WD12ThrowState : VaultState<WBoss>
	{
		private const int ThrowTick = 48; // 行 72 出手（71 帧手持白骰）
		private const int EndTick = 84;

		public override void OnEnter(VaultStateMachine<WBoss> machine, WBoss ctx) {
			base.OnEnter(machine, ctx);
			ctx.CdD12Left = WBoss.CdD12;
			ctx.ShowLauncher = true;
			ctx.TelegraphLaser = 0f;
		}

		public override IVaultState<WBoss> OnUpdate(VaultStateMachine<WBoss> machine, WBoss ctx) {
			base.OnUpdate(machine, ctx);
			NPC npc = ctx.NPC;
			npc.velocity.X *= 0.85f;
			ctx.AnimOverrideRow = Math.Min(WBoss.RowD12Start + Timer / WBoss.TicksPerFrame, WBoss.RowD12End);

			// 骰子在手里亮起：一声上扬的蜂鸣 + 橙光渐强
			if (Timer == 36)
				SoundEngine.PlaySound(SoundID.MaxMana with { Pitch = 0.3f, Volume = 0.7f }, npc.Center);
			if (Timer >= 36 && Timer < ThrowTick && !Main.dedServ)
				Lighting.AddLight(npc.Center + new Vector2(-npc.spriteDirection * 12f, -10f), 0.9f * (Timer - 36) / 12f, 0.5f * (Timer - 36) / 12f, 0.1f);

			if (Timer == ThrowTick) {
				SoundEngine.PlaySound(SoundID.Item1 with { Pitch = -0.2f }, npc.Center);
				if (ctx.IsAuthority) {
					Vector2 from = npc.Center + new Vector2(-npc.spriteDirection * 12f, -10f);
					Vector2 aim = WBoss.AimPoint(ctx.Target, 60);
					Vector2 vel = (aim - from).SafeNormalize(Vector2.UnitX * -npc.spriteDirection) * 9f + new Vector2(0f, -3f);
					Projectile.NewProjectile(npc.GetSource_FromAI(), from, vel,
						ModContent.ProjectileType<WD12>(),
						(int)(WBoss.DmgD12 * ctx.CurrentDamageScale), 2f, Main.myPlayer,
						ai0: 1f, ai1: -1f);
				}
			}

			if (Timer >= EndTick && ctx.IsAuthority)
				return new WStandState();
			return null;
		}

		public override void OnExit(VaultStateMachine<WBoss> machine, WBoss ctx) {
			ctx.AnimOverrideRow = -1;
			ctx.PickDelay = Main.rand.Next(10, 20);
		}
	}

	// ---------- 12 死亡演出：倒地不起 → 发射器脱手落地 → 最后一掷（必是 12，哑弹）→ 静场淡出 → 真死掉落 + 一张卡牌 ----------
	[VaultState(12, typeof(WBoss))]
	public class WDeathState : VaultState<WBoss>
	{
		private const int DownEnd = 42;       // 倒地动画 7 帧 ×6
		private const int LauncherTick = 48;  // 发射器脱手
		private const int DieTick = 56;       // 最后一颗骰子从手里滚出去
		private const int FadeStart = 100;
		private const int QuipTick = 140;     // 骰子停在 12 又哑火的那一刻，她说话
		private const int RealKillTick = 150;

		public override void OnEnter(VaultStateMachine<WBoss> machine, WBoss ctx) {
			base.OnEnter(machine, ctx);
			NPC npc = ctx.NPC;
			npc.dontTakeDamage = true;
			npc.damage = 0;
			npc.alpha = 0; // 若在封烟隐身瞬间被击杀，防止隐形谢幕
			if (ctx.Dashing)
				ctx.Dash_End();
			ctx.ShowLauncher = true; // 倒下的前几帧手里还端着枪，48 tick 时脱手
			ctx.TelegraphLaser = 0f;
			ctx.MuzzleFlash = 0;
			ctx.AimOverride = null;
			ctx.ExtraLasers.Clear();
			ctx.ExtraMarkers.Clear();
			ctx.CardAlpha = 0f;
			npc.noGravity = false; // 空中被打死也得掉下来
		}

		public override IVaultState<WBoss> OnUpdate(VaultStateMachine<WBoss> machine, WBoss ctx) {
			base.OnUpdate(machine, ctx);
			NPC npc = ctx.NPC;
			npc.velocity.X *= 0.85f;
			npc.dontTakeDamage = true;
			npc.damage = 0;
			ctx.AnimOverrideRow = Math.Min(WBoss.RowDownStart + Timer / WBoss.TicksPerFrame, WBoss.RowDownEnd);
			// 倒下过程中枪口垂下去
			ctx.AimOverride = npc.spriteDirection < 0 ? 0.9f : MathHelper.Pi - 0.9f;

			if (Timer == DownEnd) {
				// 全场最大的一次震屏留给谢幕
				WBoss.ShakeNearby(npc.Center, 18);
				WBoss.SmokeBurst(npc.Center, 24, 3.2f);
				WBoss.FootDust(npc.Bottom, 10, 2.5f);
				SoundEngine.PlaySound(SoundID.NPCDeath6, npc.Center);
			}
			if (Timer == LauncherTick) {
				// 发射器脱手：从手里翻出去，落地弹两下
				ctx.ShowLauncher = false;
				if (ctx.IsAuthority) {
					Projectile.NewProjectile(npc.GetSource_FromAI(), ctx.LauncherAnchor, new Vector2(-npc.spriteDirection * 2.2f, -3.5f),
						ModContent.ProjectileType<WLauncherDrop>(), 0, 0f, Main.myPlayer);
				}
			}
			if (Timer == DieTick && ctx.IsAuthority) {
				// 最后一掷：哑弹模式的召唤骰——照样停在 12
				Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center + new Vector2(-npc.spriteDirection * 8f, 0f),
					new Vector2(-npc.spriteDirection * 1.6f, -2.2f),
					ModContent.ProjectileType<WSummonDie>(), 0, 0f, Main.myPlayer, ai0: 0f, ai1: 1f);
			}
			if (Timer == QuipTick && ctx.IsAuthority)
				WBattleVisuals.Say("Death");
			if (Timer == QuipTick + 4 && !Main.dedServ)
				new WCardParticle(npc.Center + new Vector2(0f, -20f), new Vector2(-npc.spriteDirection * 0.8f, -1.6f), 0.5f).Spawn();

			// 静场淡出
			if (Timer > FadeStart)
				npc.alpha = (int)MathHelper.Lerp(0f, 220f, (Timer - FadeStart) / (float)(RealKillTick - FadeStart));
			if (!Main.dedServ && Timer > DownEnd && Timer % 6 == 0) {
				Dust d = Dust.NewDustDirect(npc.position, npc.width, npc.height, DustID.Smoke, 0f, -0.8f, 140, default, 1.1f);
				d.noGravity = true;
			}

			if (Timer >= RealKillTick && ctx.IsAuthority) {
				ctx.DeathRealKill = true;
				npc.life = 0;
				npc.checkDead(); // 触发真死：掉落 + DownedBossSystem 标记
			}
			return null;
		}
	}

	// ---------- 13 脱战：封烟跑路离场 ----------
	[VaultState(13, typeof(WBoss))]
	public class WDespawnState : VaultState<WBoss>
	{
		private const int VanishTick = 60;
		private const int EndTick = 78;

		public override void OnEnter(VaultStateMachine<WBoss> machine, WBoss ctx) {
			base.OnEnter(machine, ctx);
			if (ctx.Dashing)
				ctx.Dash_End();
			ctx.NPC.dontTakeDamage = true;
			ctx.NPC.damage = 0;
			ctx.NPC.alpha = 0;
			ctx.ShowLauncher = false;
			ctx.TelegraphLaser = 0f;
			if (ctx.IsAuthority)
				WBattleVisuals.Say("Despawn");
		}

		public override IVaultState<WBoss> OnUpdate(VaultStateMachine<WBoss> machine, WBoss ctx) {
			base.OnUpdate(machine, ctx);
			NPC npc = ctx.NPC;
			npc.velocity.X *= 0.8f;
			ctx.AnimOverrideRow = WBoss.SmokeRow(Timer);
			ctx.SmokeGrenadeBeat(Timer);

			if (Timer >= VanishTick - 8 && Timer < VanishTick)
				npc.alpha = (int)MathHelper.Lerp(0f, 255f, (Timer - (VanishTick - 8)) / 8f);
			if (Timer == VanishTick - 2)
				WBoss.SmokeBurst(npc.Center, 14, 2.2f);
			if (Timer >= VanishTick)
				npc.alpha = 255;

			if (Timer >= EndTick && ctx.IsAuthority) {
				// Boss 不能靠 EncourageDespawn 走人：CheckActive 对 boss 只在 timeLeft 归零时失活，
				// 而任何玩家在屏内都会把 timeLeft 刷回 activeTime——隐身的 W 会带着血条无限站在原地。
				// 这里镜像原版 CheckActive 的失活路径：直接失活并广播。
				npc.active = false;
				if (Main.netMode == NetmodeID.Server) {
					npc.life = 0;
					npc.netSkip = -1;
					NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);
				}
			}
			return null;
		}
	}

	// ---------- 14 跃退投雷（一阶段新增）：蓄势半步 → 后跃/前跃 → 顶点出手 → 落地起尘 ----------
	[VaultState(14, typeof(WBoss))]
	public class WHopTossState : VaultState<WBoss>
	{
		private const int BraceEnd = 10;
		private const int TimeoutTick = 100;

		private int hopDir;
		private bool airborne, released, landed; // airborne：确实离地过——客户端要等服务端的起跳速度同步到才算
		private int releaseTimer, landTimer;

		public override void OnEnter(VaultStateMachine<WBoss> machine, WBoss ctx) {
			base.OnEnter(machine, ctx);
			ctx.CdHopLeft = WBoss.CdHop;
			ctx.ShowLauncher = false;
			airborne = released = landed = false;
			if (ctx.IsAuthority) {
				NPC npc = ctx.NPC;
				int away = Math.Sign(npc.Center.X - ctx.Target.Center.X);
				if (away == 0)
					away = 1;
				// 太近就往后跳拉开，太远就往前跳压上去；落点不行就换边
				hopDir = Vector2.Distance(npc.Center, ctx.Target.Center) < 330f ? away : -away;
				if (!ctx.CanHopTo(hopDir))
					hopDir = -hopDir;
			}
		}

		public override IVaultState<WBoss> OnUpdate(VaultStateMachine<WBoss> machine, WBoss ctx) {
			base.OnUpdate(machine, ctx);
			NPC npc = ctx.NPC;

			if (Timer < BraceEnd) {
				// 蓄势：站定、手臂后引（抛雷首帧）
				npc.velocity.X *= 0.7f;
				ctx.AnimOverrideRow = WBoss.RowThrowStart;
			}
			if (Timer == BraceEnd) {
				if (ctx.IsAuthority) {
					npc.velocity = new Vector2(hopDir * 6f, -9.5f);
					npc.netUpdate = true;
				}
				WBoss.FootDust(npc.Bottom, 8, 2.5f);
			}
			if (Timer > BraceEnd) {
				if (!landed) {
					ctx.AnimOverrideRow = WBoss.RowJump;
					ctx.GhostTrail = 1f; // 空中拖残影，卖速度
					if (npc.velocity.Y < -3f)
						airborne = true;
					// 起跳被头顶物块顶回（服务端视角 4 tick 内都没离地）→ 这次不跳了，别在原地摆 90 tick 姿势
					if (!airborne && Timer > BraceEnd + 4 && ctx.IsAuthority)
						return new WWalkState();
					// 抛物线顶点出手（上升速度衰减到近零那一帧）
					if (airborne && !released && npc.velocity.Y > -1.5f) {
						released = true;
						releaseTimer = Timer;
						SoundEngine.PlaySound(SoundID.Item1, npc.Center);
						if (ctx.IsAuthority) {
							Vector2 hand = npc.Center + new Vector2(-npc.spriteDirection * 8f, -12f);
							Vector2 aim = WBoss.AimPoint(ctx.Target, 36);
							Vector2 vel = WBoss.SolveLob(hand, aim, 0.25f, 36);
							Projectile.NewProjectile(npc.GetSource_FromAI(), hand, vel,
								ModContent.ProjectileType<WHEGrenade>(),
								(int)(WBoss.DmgHEGrenade * ctx.CurrentDamageScale), 1f, Main.myPlayer);
						}
					}
					if (released && Timer - releaseTimer < 8)
						ctx.AnimOverrideRow = Timer - releaseTimer < 4 ? WBoss.RowThrowStart + 2 : WBoss.RowThrowStart + 3;
					// 落地：离地过之后再看碰地。撞到天花板时 velocity.Y 会被置成 0.01，只有踩实地面才精确为 0
					if (airborne && Timer > BraceEnd + 6 && npc.collideY && npc.velocity.Y == 0f) {
						landed = true;
						landTimer = Timer;
						WBoss.FootDust(npc.Bottom, 10, 2.8f);
						WBoss.ShakeNearby(npc.Center, 3, 500f);
					}
				}
				else {
					npc.velocity.X *= 0.6f;
					ctx.AnimOverrideRow = WBoss.RowWalkStart;
					if (Timer - landTimer >= 10 && ctx.IsAuthority)
						return new WWalkState();
				}
			}

			if (Timer >= TimeoutTick && ctx.IsAuthority)
				return new WWalkState();
			return null;
		}

		public override void OnExit(VaultStateMachine<WBoss> machine, WBoss ctx) {
			ctx.AnimOverrideRow = -1;
			ctx.PickDelay = Main.rand.Next(10, 20);
		}
	}

	// ---------- 15 三连投（一阶段 ≤85% 解锁）：同一记抛雷动作，一把撒出三颗，左中右三个落点 ----------
	[VaultState(15, typeof(WBoss))]
	public class WTripleTossState : WThrowGrenadeState
	{
		protected override void ArmCooldown(WBoss ctx) {
			ctx.CdTripleLeft = WBoss.CdTriple;
			ctx.CdThrowLeft = Math.Max(ctx.CdThrowLeft, 60); // 别紧接着再来一颗普攻雷
		}

		protected override float ReleasePitch => -0.2f;

		protected override void Release(WBoss ctx, Vector2 hand) {
			Vector2 aim = WBoss.AimPoint(ctx.Target, 40);
			for (int i = -1; i <= 1; i++) {
				Vector2 to = aim + new Vector2(i * 110f + Main.rand.NextFloat(-10f, 10f), 0f);
				Vector2 vel = WBoss.SolveLob(hand, to, 0.25f, 40 - i * 2);
				Projectile.NewProjectile(ctx.NPC.GetSource_FromAI(), hand, vel,
					ModContent.ProjectileType<WHEGrenade>(),
					(int)(WBoss.DmgHEGrenade * ctx.CurrentDamageScale), 1f, Main.myPlayer);
			}
		}
	}

	// ---------- 16 三连发（二阶段新增普攻变体）：短前摇后三发抛物线弹连打，手臂随每发上下顿 ----------
	[VaultState(16, typeof(WBoss))]
	public class WBurstShotState : VaultState<WBoss>
	{
		private const int FirstFire = 14;
		private const int Interval = 8;
		private const int Shots = 3;
		private const int LastFire = FirstFire + Interval * (Shots - 1); // 30
		private const int EndTick = 52;

		public override void OnEnter(VaultStateMachine<WBoss> machine, WBoss ctx) {
			base.OnEnter(machine, ctx);
			ctx.CdBurstLeft = WBoss.CdBurst;
			ctx.ShowLauncher = true;
			SoundEngine.PlaySound(SoundID.MaxMana with { Pitch = -0.2f, Volume = 0.6f }, ctx.NPC.Center);
		}

		public override IVaultState<WBoss> OnUpdate(VaultStateMachine<WBoss> machine, WBoss ctx) {
			base.OnUpdate(machine, ctx);
			NPC npc = ctx.NPC;
			npc.velocity.X *= 0.85f;

			if (Timer < FirstFire) {
				ctx.AnimOverrideRow = WBoss.SwingRow(Timer, FirstFire);
				ctx.TelegraphLaser = Timer < FirstFire - 2 ? 0.35f * Timer / FirstFire : 0f;
			}
			else if (Timer < LastFire + Interval) {
				// 连发期间手臂一顿一顿：每发开火帧停 4 tick，再回举臂帧
				ctx.AnimOverrideRow = (Timer - FirstFire) % Interval < 4 ? WBoss.RowSwingStart + 3 : WBoss.RowSwingStart + 2;
				ctx.TelegraphLaser = 0f;
			}
			else {
				ctx.AnimOverrideRow = WBoss.RowSwingEnd;
			}

			if (Timer >= FirstFire && Timer <= LastFire && (Timer - FirstFire) % Interval == 0) {
				int i = (Timer - FirstFire) / Interval;
				SoundEngine.PlaySound(SoundID.Item61 with { Pitch = -0.15f + 0.12f * i }, npc.Center);
				ctx.MuzzleFire(1f);
				if (ctx.IsAuthority) {
					Vector2 muzzle = ctx.MuzzlePos;
					// 三发落点左、中、右错开，逼玩家横向挪
					Vector2 aim = WBoss.AimPoint(ctx.Target, 34) + new Vector2((i - 1) * 70f, 0f);
					Vector2 vel = WBoss.SolveLob(muzzle, aim, 0.2f, 34);
					Projectile.NewProjectile(npc.GetSource_FromAI(), muzzle, vel,
						ModContent.ProjectileType<WLipstickRound>(),
						(int)(WBoss.DmgArcRound * ctx.CurrentDamageScale), 1f, Main.myPlayer,
						ai0: 1f);
				}
			}

			if (Timer >= EndTick && ctx.IsAuthority)
				return new WStandState();
			return null;
		}

		public override void OnExit(VaultStateMachine<WBoss> machine, WBoss ctx) {
			ctx.AnimOverrideRow = -1;
			ctx.TelegraphLaser = 0f;
			ctx.PickDelay = Main.rand.Next(8, 16);
		}
	}

	// ---------- 17 曲射弹幕（二阶段 ≤35% 解锁）：发射器抬向天顶，六发高抛弹在玩家周围落成一片 ----------
	[VaultState(17, typeof(WBoss))]
	public class WMortarState : VaultState<WBoss>
	{
		private const int FirstFire = 24;
		private const int Interval = 6;
		private const int Shots = 6;
		private const int LastFire = FirstFire + Interval * (Shots - 1); // 54
		private const int EndTick = 84;
		private const int FlightTicks = 100; // 高抛：约 1.7s 后落地，弹道全程可见

		// 落点横向散布：左右交错，覆盖玩家两侧 ±150px
		private static readonly float[] Offsets = { -150f, 90f, -30f, 150f, 30f, -90f };

		public override void OnEnter(VaultStateMachine<WBoss> machine, WBoss ctx) {
			base.OnEnter(machine, ctx);
			NPC npc = ctx.NPC;
			ctx.CdMortarLeft = WBoss.CdMortar;
			ctx.ShowLauncher = true;
			ctx.TelegraphLaser = 0f;
			// 枪口抬向天顶，略偏玩家一侧（贴图朝右时 spriteDirection=-1，-spriteDirection 即面向）
			ctx.AimOverride = -MathHelper.PiOver2 + -npc.spriteDirection * 0.22f;
			SoundEngine.PlaySound(SoundID.MaxMana with { Pitch = -0.5f, Volume = 0.8f }, npc.Center);
		}

		public override IVaultState<WBoss> OnUpdate(VaultStateMachine<WBoss> machine, WBoss ctx) {
			base.OnUpdate(machine, ctx);
			NPC npc = ctx.NPC;
			npc.velocity.X *= 0.85f;

			if (Timer < FirstFire) {
				// 举臂 + 发射器抬起（AimRotation 平滑追到朝天角），三声递升的蜂鸣
				ctx.AnimOverrideRow = WBoss.SwingRow(Timer, FirstFire);
				if (Timer == 8 || Timer == 16 || Timer == 22)
					SoundEngine.PlaySound(SoundID.MaxMana with { Pitch = -0.3f + (Timer - 8) / 14f * 0.6f, Volume = 0.5f }, npc.Center);
			}
			else if (Timer < LastFire + Interval) {
				ctx.AnimOverrideRow = (Timer - FirstFire) % Interval < 3 ? WBoss.RowSwingStart + 3 : WBoss.RowSwingStart + 2;
			}
			else {
				ctx.AnimOverrideRow = WBoss.RowSwingEnd;
			}

			if (Timer >= FirstFire && Timer <= LastFire && (Timer - FirstFire) % Interval == 0) {
				int i = (Timer - FirstFire) / Interval;
				SoundEngine.PlaySound(SoundID.Item61 with { Pitch = 0.3f, Volume = 0.9f }, npc.Center);
				ctx.MuzzleFire(0.8f);
				if (ctx.IsAuthority) {
					Vector2 muzzle = ctx.MuzzlePos;
					// 落点取玩家脚下那一排地面（找不到就用玩家脚底），X 标记画在这里
					Vector2 land = ctx.Target.Bottom + new Vector2(Offsets[i], 0f);
					land = ctx.GroundBelow(land, 12);
					Vector2 vel = WBoss.SolveLob(muzzle, land, 0.25f, FlightTicks);
					Projectile.NewProjectile(npc.GetSource_FromAI(), muzzle, vel,
						ModContent.ProjectileType<WLipstickRound>(),
						(int)(WBoss.DmgArcRound * ctx.CurrentDamageScale), 1f, Main.myPlayer,
						ai0: 2f, ai1: land.X, ai2: land.Y);
				}
			}

			if (Timer >= EndTick && ctx.IsAuthority)
				return new WStandState();
			return null;
		}

		public override void OnExit(VaultStateMachine<WBoss> machine, WBoss ctx) {
			ctx.AimOverride = null;
			ctx.AnimOverrideRow = -1;
			ctx.TelegraphLaser = 0f;
			ctx.PickDelay = 26; // 弹还在天上，喘半口气再接下一招
		}
	}

	// ---------- 18 倒数计时→全场起爆（二阶段 set-piece）：背身按下起爆器，场上所有已布设的雷/骰子同时进入 2 秒倒数 ----------
	// 复用 47-64 帧：转身背对 → 装定 → 60-62 帧闪光（按下那一刻）→ 转回。没有出手动作，与一阶段的 D12 掷出区分。
	[VaultState(18, typeof(WBoss))]
	public class WDetonateAllState : VaultState<WBoss>
	{
		private const int FrameHold = 5;
		private const int PressTick = 13 * FrameHold; // 行 59（第一张闪光帧）：按下 = 65
		private const int FuseTicks = 120;            // 全场倒数 2 秒——足够离开所有红圈
		private const int EndTick = 95;

		public override void OnEnter(VaultStateMachine<WBoss> machine, WBoss ctx) {
			base.OnEnter(machine, ctx);
			ctx.CdDetonateLeft = WBoss.CdDetonate;
			ctx.ShowLauncher = false;
			ctx.TelegraphLaser = 0f;
		}

		public override IVaultState<WBoss> OnUpdate(VaultStateMachine<WBoss> machine, WBoss ctx) {
			base.OnUpdate(machine, ctx);
			NPC npc = ctx.NPC;
			npc.velocity.X *= 0.8f;
			ctx.AnimOverrideRow = Math.Min(WBoss.RowCountdownStart + Timer / FrameHold, WBoss.RowCountdownEnd);

			// 装定咔哒 → 按下前两声递升
			if (Timer == 5 * FrameHold)
				SoundEngine.PlaySound(SoundID.Unlock with { Volume = 0.6f, Pitch = 0.2f }, npc.Center);
			if (Timer == 9 * FrameHold || Timer == 11 * FrameHold)
				SoundEngine.PlaySound(SoundID.MaxMana with { Pitch = Timer == 9 * FrameHold ? -0.2f : 0.1f, Volume = 0.7f }, npc.Center);

			if (Timer == PressTick) {
				SoundEngine.PlaySound(SoundID.Unlock with { Volume = 0.9f, Pitch = -0.4f }, npc.Center);
				WBoss.ShakeNearby(npc.Center, 4);
				WBattleVisuals.Flash(0.18f, new Color(255, 120, 90));
				if (ctx.IsAuthority) {
					ctx.PrimeAllOrdnance(FuseTicks);
					WBattleVisuals.Say("DetonateAll");
				}
			}
			if (Timer >= PressTick && Timer < PressTick + 3 * FrameHold && !Main.dedServ)
				Lighting.AddLight(npc.Center + new Vector2(-npc.spriteDirection * 6f, -4f), 1.0f, 0.55f, 0.15f);

			if (Timer >= EndTick && ctx.IsAuthority)
				return new WStandState();
			return null;
		}

		public override void OnExit(VaultStateMachine<WBoss> machine, WBoss ctx) {
			ctx.ShowLauncher = true;
			ctx.AnimOverrideRow = -1;
			ctx.PickDelay = 20;
		}
	}

	// ---------- 21 烟带撒雷 + 顺序起爆（二阶段 ≤45%）：慢速烟带穿过玩家到对面，一路落雷；落地后背身按起爆器，雷从最远端依次滚爆过来 ----------
	[VaultState(21, typeof(WBoss))]
	public class WMineRunState : VaultState<WBoss>
	{
		private const int FrameHold = 4;
		private const int VanishTick = 32;       // 烟带出发
		private const int DropInterval = 6;      // 每 6 tick 落一颗
		private const int ReappearDelay = 6;
		private const int PressDelay = 26;       // 现身后到按下起爆器
		private const int EndAfterPress = 18;

		private int arriveTick = -1;
		private bool pressed;

		public override void OnEnter(VaultStateMachine<WBoss> machine, WBoss ctx) {
			base.OnEnter(machine, ctx);
			ctx.CdMineRunLeft = WBoss.CdMineRun;
			ctx.CdTeleportLeft = Math.Max(ctx.CdTeleportLeft, 120);
			ctx.ShowLauncher = false;
			ctx.TelegraphLaser = 0f;
			arriveTick = -1;
			pressed = false;
			if (ctx.IsAuthority) {
				ctx.TeleportMode = WBoss.TeleportKind.CrossOver;
				ctx.TeleportDest = ctx.FindTeleportDest(ctx.Target, WBoss.TeleportKind.CrossOver);
				ctx.NPC.netUpdate = true;
			}
		}

		public override IVaultState<WBoss> OnUpdate(VaultStateMachine<WBoss> machine, WBoss ctx) {
			base.OnUpdate(machine, ctx);
			NPC npc = ctx.NPC;
			npc.damage = 0;

			if (Timer < VanishTick) {
				npc.velocity.X *= 0.8f;
				ctx.AnimOverrideRow = Math.Min(WBoss.RowSmokeStart + Timer / FrameHold, WBoss.RowSmokeEnd);
				if (Timer == 16) {
					WBoss.SmokeBurst(npc.Center + new Vector2(-npc.spriteDirection * 24f, -38f), 14, 3f);
					WBoss.SmokeBurst(npc.Center, 8, 1.8f);
					SoundEngine.PlaySound(SoundID.Item66, npc.Center);
				}
				if (Timer >= VanishTick - 6)
					npc.alpha = (int)MathHelper.Lerp(0f, 255f, (Timer - (VanishTick - 6)) / 6f);
				if (Timer == VanishTick - 2)
					WBoss.SmokeBurst(npc.Center, 12, 2.2f);
				return null;
			}

			if (Timer == VanishTick)
				ctx.Dash_Begin(24, 48, 12f); // 慢速烟带：看得清它一路在撒东西
			if (ctx.Dashing) {
				ctx.AnimOverrideRow = WBoss.RowSmokeEnd;
				// 每 6 tick 从烟里掉一颗线雷
				if (ctx.IsAuthority && ctx.DashTimer % DropInterval == 0) {
					Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center + new Vector2(0f, 6f), new Vector2(0f, 1.5f),
						ModContent.ProjectileType<WLineCharge>(),
						(int)(WBoss.DmgHEGrenade * ctx.CurrentDamageScale), 1f, Main.myPlayer,
						ai2: npc.whoAmI + 1);
				}
				if (ctx.Dash_Tick())
					arriveTick = Timer;
				return null;
			}
			if (arriveTick < 0)
				arriveTick = Timer;

			int since = Timer - arriveTick;
			if (since < ReappearDelay) {
				npc.alpha = 255;
				ctx.AnimOverrideRow = WBoss.RowSmokeEnd;
				if (since == 0)
					WBoss.SmokeBurst(npc.Center, 14);
				return null;
			}
			int shown = since - ReappearDelay;
			npc.alpha = (int)MathHelper.Lerp(255f, 0f, Math.Min(shown / 8f, 1f));
			// 现身即背身装定：47-64 帧的前半段，按下那一刻停在闪光帧
			ctx.AnimOverrideRow = Math.Min(WBoss.RowCountdownStart + 1 + shown / 4, WBoss.RowCountdownStart + 14);
			if (shown == 10)
				SoundEngine.PlaySound(SoundID.Unlock with { Volume = 0.6f, Pitch = 0.2f }, npc.Center);

			if (shown == PressDelay && !pressed) {
				pressed = true;
				SoundEngine.PlaySound(SoundID.Unlock with { Volume = 0.9f, Pitch = -0.4f }, npc.Center);
				WBoss.ShakeNearby(npc.Center, 3);
				WBattleVisuals.Flash(0.12f, new Color(255, 120, 90));
				// 从最远那颗开始，每颗错开 8 tick：一道爆炸墙朝 W 这边滚过来（雷间距约 72px，墙速约 9px/tick，可以起跳跨过）
				if (ctx.IsAuthority)
					ctx.PrimeOrdnanceOfType<WLineCharge>(14, 8, farthestFirst: true);
			}
			if (pressed && !Main.dedServ)
				Lighting.AddLight(npc.Center + new Vector2(-npc.spriteDirection * 6f, -4f), 1.0f, 0.55f, 0.15f);

			if (shown >= PressDelay + EndAfterPress && ctx.IsAuthority)
				return new WStandState();
			return null;
		}

		public override void OnExit(VaultStateMachine<WBoss> machine, WBoss ctx) {
			if (ctx.Dashing)
				ctx.Dash_End();
			NPC npc = ctx.NPC;
			npc.alpha = 0;
			npc.damage = (int)(npc.defDamage * ctx.P2DamageScale);
			ctx.ShowLauncher = true;
			ctx.AnimOverrideRow = -1;
			ctx.PickDelay = 30;
		}
	}

	// ---------- 22 骰子雨（二阶段 ≤45%）：D12 动作，一把掷出三颗会弹跳的骰子，各自滚出点数、各自倒数 ----------
	[VaultState(22, typeof(WBoss))]
	public class WDiceRainState : VaultState<WBoss>
	{
		private const int FirstThrow = 48;  // 行 72 出手
		private const int EndTick = 90;

		public override void OnEnter(VaultStateMachine<WBoss> machine, WBoss ctx) {
			base.OnEnter(machine, ctx);
			ctx.CdDiceRainLeft = WBoss.CdDiceRain;
			ctx.CdD12Left = Math.Max(ctx.CdD12Left, 300);
			ctx.ShowLauncher = true;
			ctx.TelegraphLaser = 0f;
		}

		public override IVaultState<WBoss> OnUpdate(VaultStateMachine<WBoss> machine, WBoss ctx) {
			base.OnUpdate(machine, ctx);
			NPC npc = ctx.NPC;
			npc.velocity.X *= 0.85f;
			ctx.AnimOverrideRow = Math.Min(WBoss.RowD12Start + Timer / WBoss.TicksPerFrame, WBoss.RowD12End);

			if (Timer == 36)
				SoundEngine.PlaySound(SoundID.MaxMana with { Pitch = 0.3f, Volume = 0.7f }, npc.Center);
			if (Timer >= 36 && Timer < FirstThrow && !Main.dedServ)
				Lighting.AddLight(npc.Center + new Vector2(-npc.spriteDirection * 12f, -10f), 0.9f * (Timer - 36) / 12f, 0.5f * (Timer - 36) / 12f, 0.1f);

			// 三颗错开 4 tick 出手，扇形撒向玩家左中右
			if (Timer == FirstThrow || Timer == FirstThrow + 4 || Timer == FirstThrow + 8) {
				int i = (Timer - FirstThrow) / 4 - 1; // -1, 0, 1
				SoundEngine.PlaySound(SoundID.Item1 with { Pitch = -0.2f + 0.1f * i }, npc.Center);
				if (ctx.IsAuthority) {
					Vector2 from = npc.Center + new Vector2(-npc.spriteDirection * 12f, -10f);
					Vector2 aim = WBoss.AimPoint(ctx.Target, 45) + new Vector2(i * 110f, 0f);
					Vector2 vel = WBoss.SolveLob(from, aim, 0.2f, 45);
					// 一阶段模式的骰子：会弹跳、滚停/黏附才点火，混乱 5s
					Projectile.NewProjectile(npc.GetSource_FromAI(), from, vel,
						ModContent.ProjectileType<WD12>(),
						(int)(WBoss.DmgD12 * ctx.CurrentDamageScale), 2f, Main.myPlayer,
						ai0: 0f, ai1: -1f);
				}
			}

			if (Timer >= EndTick && ctx.IsAuthority)
				return new WStandState();
			return null;
		}

		public override void OnExit(VaultStateMachine<WBoss> machine, WBoss ctx) {
			ctx.AnimOverrideRow = -1;
			ctx.PickDelay = Main.rand.Next(12, 20);
		}
	}

	// ---------- 23 红桃K·满注（二阶段 ≤25%，双管）：四道激光从枪口画向玩家四角的落点 → 四发红桃K 连射，爆点呈 X，安全区是四个楔形 ----------
	[VaultState(23, typeof(WBoss))]
	public class WAllInKingState : VaultState<WBoss>
	{
		private const int FirstFire = 60;
		private const int Interval = 6;
		private const int Shots = 4;
		private const int LastFire = FirstFire + Interval * (Shots - 1); // 78
		private const int EndTick = 108;
		private const float RoundSpeed = 17f;
		private const float Spread = 110f; // 四个落点离玩家的横/纵偏移：到中心 155 > 雷管半径 125，站在正中不动是"押中"；往任意一角跑就是"押错"

		private static readonly Vector2[] Corners = {
			new(-Spread, -Spread), new(Spread, Spread), new(Spread, -Spread), new(-Spread, Spread),
		};

		public override void OnEnter(VaultStateMachine<WBoss> machine, WBoss ctx) {
			base.OnEnter(machine, ctx);
			ctx.CdAllInLeft = WBoss.CdAllIn;
			ctx.CdKingLeft = Math.Max(ctx.CdKingLeft, 300);
			ctx.ShowLauncher = true;
			ctx.TelegraphLaser = 0f;
			SoundEngine.PlaySound(SoundID.MaxMana with { Pitch = -0.5f, Volume = 0.9f }, ctx.NPC.Center);
		}

		public override IVaultState<WBoss> OnUpdate(VaultStateMachine<WBoss> machine, WBoss ctx) {
			base.OnUpdate(machine, ctx);
			NPC npc = ctx.NPC;
			npc.velocity.X *= 0.8f;
			Player target = ctx.Target;

			if (Timer < FirstFire) {
				ctx.AnimOverrideRow = WBoss.SwingRow(Timer, FirstFire);
				// 四条线慢慢亮起来压到落点上；开火前 4 tick 收线
				float k = Timer < FirstFire - 4 ? MathF.Pow(Timer / (float)FirstFire, 1.4f) : 0f;
				ctx.TelegraphLaser = k;
				ctx.ExtraLasers.Clear();
				if (k > 0f)
					foreach (Vector2 c in Corners)
						ctx.ExtraLasers.Add(target.Center + c);
				// 翻牌：这次牌翻得更慢更郑重
				ctx.CardAlpha = Math.Min(Timer / 10f, 1f);
				ctx.CardFlip = 0.5f * Timer / FirstFire;
				if (Timer == 20 || Timer == 36 || Timer == 50)
					SoundEngine.PlaySound(SoundID.MaxMana with { Pitch = -0.2f + (Timer - 20) / 30f * 0.5f, Volume = 0.7f }, npc.Center);
			}
			else {
				ctx.ExtraLasers.Clear();
				ctx.TelegraphLaser = 0f;
				ctx.AnimOverrideRow = Timer < LastFire + Interval
					? ((Timer - FirstFire) % Interval < 3 ? WBoss.RowSwingStart + 3 : WBoss.RowSwingStart + 2)
					: WBoss.RowSwingEnd;
				ctx.CardFlip = 0.5f + 0.5f * Math.Min((Timer - FirstFire) / 5f, 1f);
				ctx.CardAlpha = Timer < FirstFire + 20 ? 1f : Math.Max(0f, 1f - (Timer - FirstFire - 20) / 10f);
			}

			if (Timer >= FirstFire && Timer <= LastFire && (Timer - FirstFire) % Interval == 0) {
				int i = (Timer - FirstFire) / Interval;
				SoundEngine.PlaySound(SoundID.Item61 with { Pitch = -0.1f + 0.08f * i }, npc.Center);
				if (i == 0)
					SoundEngine.PlaySound(SoundID.MenuTick with { Pitch = 0.6f }, npc.Center);
				ctx.MuzzleFire(1.6f);
				WBoss.ShakeNearby(npc.Center, 3, 600f);
				if (ctx.IsAuthority) {
					Vector2 muzzle = ctx.MuzzlePos;
					Vector2 aim = target.Center + Corners[i];
					Vector2 delta = aim - muzzle;
					Vector2 vel = delta.SafeNormalize(Vector2.UnitX * -npc.spriteDirection) * RoundSpeed;
					// 定时引信：飞到落点就炸，朝天的两发才能在空中开花
					int fuse = Math.Max(4, (int)(delta.Length() / RoundSpeed));
					Projectile.NewProjectile(npc.GetSource_FromAI(), muzzle, vel,
						ModContent.ProjectileType<WLipstickRound>(),
						(int)(WBoss.DmgKingRound * ctx.CurrentDamageScale), 2f, Main.myPlayer,
						ai0: 0f, ai1: -fuse);
					// 烟幕射击被动只留一团，别把场地糊满
					if (i == 0) {
						Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero,
							ModContent.ProjectileType<WSmokeCloud>(), 0, 0f, Main.myPlayer,
							ai0: npc.whoAmI, ai1: ctx.SmokeCloudRadius);
					}
				}
				if (i == 0)
					WBoss.SmokeBurst(npc.Center, 12, 2f);
			}

			if (Timer >= EndTick && ctx.IsAuthority)
				return new WStandState();
			return null;
		}

		public override void OnExit(VaultStateMachine<WBoss> machine, WBoss ctx) {
			ctx.ExtraLasers.Clear();
			ctx.TelegraphLaser = 0f;
			ctx.AnimOverrideRow = -1;
			ctx.PickDelay = 30;
		}
	}

	// ---------- 空中动作骨架：蓄势 → 起跳 → 顶点悬停（关重力若干帧）→ 下落 → 重落地。子类在钩子里塞动作与弹幕 ----------
	// 人形 boss 的记忆点大半来自纵向：这套骨架保证每个空中招都有"腾空—定格—落地"三拍，残影与落地起尘由骨架统一给。
	public abstract class WAerialState : VaultState<WBoss>
	{
		protected enum AirPhase { Brace, Rising, Hang, Falling, Landed }

		protected AirPhase Phase = AirPhase.Brace;
		protected int PhaseTimer;
		protected bool airborne;

		protected virtual int BraceTicks => 12;
		protected virtual int HangTicks => 14;
		protected virtual float JumpSpeed => -15f;
		protected virtual int Timeout => 170;

		/// <summary>蓄势每帧（动画/瞄准）</summary>
		protected abstract void OnBrace(WBoss ctx, int t);
		/// <summary>起跳瞬间（两端都调；服务端此时速度已设好）</summary>
		protected abstract void OnLaunch(WBoss ctx);
		/// <summary>悬停每帧</summary>
		protected abstract void OnHang(WBoss ctx, int t);
		/// <summary>落地瞬间（两端都调）</summary>
		protected virtual void OnLanded(WBoss ctx) { }
		/// <summary>起跳的横向初速</summary>
		protected virtual float LaunchVx(WBoss ctx) => 0f;
		protected virtual IVaultState<WBoss> Next(WBoss ctx) => ctx.SchedulerState();

		public override void OnEnter(VaultStateMachine<WBoss> machine, WBoss ctx) {
			base.OnEnter(machine, ctx);
			Phase = AirPhase.Brace;
			PhaseTimer = 0;
			airborne = false;
		}

		public override IVaultState<WBoss> OnUpdate(VaultStateMachine<WBoss> machine, WBoss ctx) {
			base.OnUpdate(machine, ctx);
			NPC npc = ctx.NPC;
			PhaseTimer++;

			switch (Phase) {
				case AirPhase.Brace:
					npc.velocity.X *= 0.7f;
					OnBrace(ctx, PhaseTimer);
					if (PhaseTimer >= BraceTicks) {
						if (ctx.IsAuthority) {
							npc.velocity = new Vector2(LaunchVx(ctx), JumpSpeed);
							npc.netUpdate = true;
						}
						WBoss.FootDust(npc.Bottom, 10, 3f);
						OnLaunch(ctx);
						Phase = AirPhase.Rising;
						PhaseTimer = 0;
					}
					break;

				case AirPhase.Rising:
					ctx.AnimOverrideRow = WBoss.RowJump;
					ctx.GhostTrail = 1f;
					if (npc.velocity.Y < -3f)
						airborne = true;
					// 起跳被头顶物块顶回（服务端 4 tick 内没离地）→ 放弃这次
					if (!airborne && PhaseTimer > 4 && ctx.IsAuthority)
						return Next(ctx);
					if (airborne && npc.velocity.Y > -1f) {
						Phase = AirPhase.Hang;
						PhaseTimer = 0;
						npc.noGravity = true;
						npc.velocity.Y = 0f;
					}
					break;

				case AirPhase.Hang:
					npc.noGravity = true;
					npc.velocity.Y = 0f;
					npc.velocity.X *= 0.88f;
					ctx.GhostTrail = Math.Max(ctx.GhostTrail, 0.35f);
					OnHang(ctx, PhaseTimer);
					if (PhaseTimer >= HangTicks) {
						npc.noGravity = false;
						Phase = AirPhase.Falling;
						PhaseTimer = 0;
					}
					break;

				case AirPhase.Falling:
					ctx.AnimOverrideRow = WBoss.RowJump;
					ctx.GhostTrail = Math.Max(ctx.GhostTrail, 0.6f);
					// 只有踩实地面 velocity.Y 才精确为 0（撞天花板是 0.01）
					if (PhaseTimer > 2 && npc.collideY && npc.velocity.Y == 0f) {
						Phase = AirPhase.Landed;
						PhaseTimer = 0;
						WBoss.FootDust(npc.Bottom, 14, 3.2f);
						WBoss.ShakeNearby(npc.Center, 5, 700f);
						SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.8f, Pitch = -0.3f }, npc.Center);
						OnLanded(ctx);
					}
					break;

				case AirPhase.Landed:
					npc.velocity.X *= 0.6f;
					ctx.AnimOverrideRow = WBoss.RowWalkStart;
					if (PhaseTimer >= 8 && ctx.IsAuthority)
						return Next(ctx);
					break;
			}

			if (Timer >= Timeout && ctx.IsAuthority)
				return Next(ctx);
			return null;
		}

		public override void OnExit(VaultStateMachine<WBoss> machine, WBoss ctx) {
			ctx.NPC.noGravity = false;
			ctx.AimOverride = null;
			ctx.TelegraphLaser = 0f;
			ctx.AnimOverrideRow = -1;
			ctx.PickDelay = Main.rand.Next(8, 16);
		}
	}

	// ---------- 24 爆破跳（两阶段）：朝自己脚下打一发/丢一颗，用爆炸把自己炸上天；顶点悬停，朝下方玩家扇形撒三颗雷/三发榴弹；重落地 ----------
	[VaultState(24, typeof(WBoss))]
	public class WBlastJumpState : WAerialState
	{
		protected override int BraceTicks => 14;
		protected override int HangTicks => 16;
		protected override float JumpSpeed => -13f; // 顶点约 280px（17 格），与净空检查一致，不会跳出屏幕

		public override void OnEnter(VaultStateMachine<WBoss> machine, WBoss ctx) {
			base.OnEnter(machine, ctx);
			ctx.CdBlastJumpLeft = WBoss.CdBlastJump;
			ctx.ShowLauncher = ctx.Phase == 2;
		}

		protected override float LaunchVx(WBoss ctx) {
			// 离得远就顺势朝玩家那边跃，近了就原地起跳
			float dx = ctx.Target.Center.X - ctx.NPC.Center.X;
			return Math.Abs(dx) > 300f ? Math.Sign(dx) * 3.5f : 0f;
		}

		protected override void OnBrace(WBoss ctx, int t) {
			NPC npc = ctx.NPC;
			if (ctx.Phase == 2) {
				// 枪口压向脚下，激光打在地上
				ctx.AimOverride = MathHelper.PiOver2 + -npc.spriteDirection * 0.35f;
				ctx.AnimOverrideRow = WBoss.SwingRow(t, BraceTicks);
				ctx.TelegraphLaser = 0.5f * t / BraceTicks;
			}
			else {
				ctx.AnimOverrideRow = WBoss.RowThrowStart + Math.Min(t / 5, 2);
			}
			if (t == 6)
				SoundEngine.PlaySound(SoundID.MaxMana with { Pitch = -0.3f, Volume = 0.7f }, npc.Center);
		}

		protected override void OnLaunch(WBoss ctx) {
			NPC npc = ctx.NPC;
			ctx.TelegraphLaser = 0f;
			ctx.AimOverride = null;
			if (ctx.Phase == 2)
				ctx.MuzzleFire(1.6f);
			else
				SoundEngine.PlaySound(SoundID.Item1, npc.Center);
			WBoss.ShakeNearby(npc.Center, 6, 700f);
			if (ctx.IsAuthority) {
				// 脚下真炸：贴脸的玩家会挨这一下
				Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Bottom + new Vector2(0f, -6f), Vector2.Zero,
					ModContent.ProjectileType<WExplosion>(), (int)(WBoss.DmgHEGrenade * ctx.CurrentDamageScale), 6f, Main.myPlayer,
					ai0: 72f, ai1: 0f, ai2: 0f);
			}
		}

		protected override void OnHang(WBoss ctx, int t) {
			NPC npc = ctx.NPC;
			if (ctx.Phase == 2) {
				ctx.AnimOverrideRow = t < 5 ? WBoss.RowSwingStart + 2 : (t < 10 ? WBoss.RowSwingStart + 3 : WBoss.RowSwingEnd);
				ctx.TelegraphLaser = t < 4 ? 0.4f : 0f;
			}
			else {
				ctx.AnimOverrideRow = t < 5 ? WBoss.RowThrowStart + 2 : (t < 10 ? WBoss.RowThrowStart + 3 : WBoss.RowThrowStart + 5);
			}
			if (t != 5)
				return;
			// 顶点出手：三发/三颗朝下方玩家左中右扇形
			if (ctx.Phase == 2)
				ctx.MuzzleFire(1.2f);
			else
				SoundEngine.PlaySound(SoundID.Item1 with { Pitch = -0.15f }, npc.Center);
			if (!ctx.IsAuthority)
				return;
			Vector2 from = ctx.Phase == 2 ? ctx.MuzzlePos : npc.Center + new Vector2(-npc.spriteDirection * 8f, 6f);
			for (int i = -1; i <= 1; i++) {
				Vector2 aim = WBoss.AimPoint(ctx.Target, 30) + new Vector2(i * 90f, 0f);
				Vector2 vel = WBoss.SolveLob(from, aim, ctx.Phase == 2 ? 0.2f : 0.25f, 30);
				if (ctx.Phase == 2) {
					Projectile.NewProjectile(npc.GetSource_FromAI(), from, vel, ModContent.ProjectileType<WLipstickRound>(),
						(int)(WBoss.DmgArcRound * ctx.CurrentDamageScale), 1f, Main.myPlayer, ai0: 1f);
				}
				else {
					Projectile.NewProjectile(npc.GetSource_FromAI(), from, vel, ModContent.ProjectileType<WHEGrenade>(),
						(int)(WBoss.DmgHEGrenade * ctx.CurrentDamageScale), 1f, Main.myPlayer);
				}
			}
		}

		protected override void OnLanded(WBoss ctx) {
			// 重落地：脚下一圈不伤人的冲击波，把"分量"画出来
			if (ctx.IsAuthority) {
				Projectile.NewProjectile(ctx.NPC.GetSource_FromAI(), ctx.NPC.Bottom, Vector2.Zero,
					ModContent.ProjectileType<WExplosion>(), 1, 0f, Main.myPlayer, ai0: 56f, ai1: -1f, ai2: 0f);
			}
		}

		public override void OnExit(VaultStateMachine<WBoss> machine, WBoss ctx) {
			base.OnExit(machine, ctx);
			ctx.ShowLauncher = ctx.Phase == 2;
		}
	}

	// ---------- 25 凌空扫射（二阶段 ≤45%）：跃到玩家头顶悬停 40 帧，激光从左到右扫过地面，五发榴弹沿地面连爆 ----------
	[VaultState(25, typeof(WBoss))]
	public class WAirStrafeState : WAerialState
	{
		private const int Shots = 5;
		private const int ShotInterval = 7;
		private const float Sweep = 200f;

		protected override int BraceTicks => 12;
		protected override int HangTicks => 8 + Shots * ShotInterval; // 43
		protected override float JumpSpeed => -12f; // 顶点约 240px：压在玩家头顶上方开火

		private int sweepDir = 1;

		public override void OnEnter(VaultStateMachine<WBoss> machine, WBoss ctx) {
			base.OnEnter(machine, ctx);
			ctx.CdAirStrafeLeft = WBoss.CdAirStrafe;
			ctx.ShowLauncher = true;
			sweepDir = Main.rand.NextBool() ? 1 : -1;
		}

		protected override float LaunchVx(WBoss ctx) =>
			MathHelper.Clamp((ctx.Target.Center.X - ctx.NPC.Center.X) / 34f, -7f, 7f);

		protected override void OnBrace(WBoss ctx, int t) {
			ctx.AnimOverrideRow = WBoss.SwingRow(t, BraceTicks);
			if (t == 4)
				SoundEngine.PlaySound(SoundID.MaxMana with { Pitch = 0.1f, Volume = 0.7f }, ctx.NPC.Center);
		}

		protected override void OnLaunch(WBoss ctx) {
			SoundEngine.PlaySound(SoundID.Item66 with { Volume = 0.5f, Pitch = 0.3f }, ctx.NPC.Center);
		}

		/// <summary>当前扫射点：玩家脚下地面，从一侧扫到另一侧</summary>
		private Vector2 SweepPoint(WBoss ctx, float progress) {
			Vector2 ground = ctx.GroundBelow(ctx.Target.Bottom, 12);
			return ground + new Vector2(sweepDir * MathHelper.Lerp(-Sweep, Sweep, progress), 0f);
		}

		protected override void OnHang(WBoss ctx, int t) {
			NPC npc = ctx.NPC;
			int shotPhase = t - 8; // 前 8 tick 只瞄准
			float progress = MathHelper.Clamp(shotPhase / (float)(Shots * ShotInterval), 0f, 1f);
			Vector2 aimPoint = SweepPoint(ctx, progress);
			ctx.AimOverride = (aimPoint - ctx.LauncherAnchor).ToRotation();
			ctx.TelegraphLaser = shotPhase < 0 ? 0.45f * (t / 8f) : 0.45f;
			ctx.AnimOverrideRow = shotPhase >= 0 && shotPhase % ShotInterval < 3 ? WBoss.RowSwingStart + 3 : WBoss.RowSwingStart + 2;

			if (shotPhase < 0 || shotPhase % ShotInterval != 0 || shotPhase / ShotInterval >= Shots)
				return;
			int i = shotPhase / ShotInterval;
			SoundEngine.PlaySound(SoundID.Item61 with { Pitch = -0.1f + 0.08f * i }, npc.Center);
			ctx.MuzzleFire(1f);
			if (!ctx.IsAuthority)
				return;
			Vector2 target = SweepPoint(ctx, (i + 0.5f) / Shots);
			Vector2 muzzle = ctx.MuzzlePos;
			Vector2 vel = WBoss.SolveLob(muzzle, target, 0.2f, 26);
			Projectile.NewProjectile(npc.GetSource_FromAI(), muzzle, vel, ModContent.ProjectileType<WLipstickRound>(),
				(int)(WBoss.DmgArcRound * ctx.CurrentDamageScale), 1f, Main.myPlayer, ai0: 1f);
		}

		public override void OnExit(VaultStateMachine<WBoss> machine, WBoss ctx) {
			base.OnExit(machine, ctx);
			ctx.ShowLauncher = true;
		}
	}

	// ---------- 26 烟火（一阶段手抛 / 二阶段 ≤45% 发射器朝天）：一发大弹升到顶点金光炸开成 12 颗子弹环形洒下 ----------
	[VaultState(26, typeof(WBoss))]
	public class WFireworkState : VaultState<WBoss>
	{
		private const int LauncherFireTick = 30;
		private const int HandReleaseTick = 18; // 抛雷 23 帧出手
		private const int EndTick = 64;

		private bool ByHand(WBoss ctx) => ctx.Phase == 1;

		public override void OnEnter(VaultStateMachine<WBoss> machine, WBoss ctx) {
			base.OnEnter(machine, ctx);
			ctx.CdFireworkLeft = WBoss.CdFirework;
			ctx.TelegraphLaser = 0f;
			if (ByHand(ctx)) {
				ctx.ShowLauncher = false;
				ctx.CdThrowLeft = Math.Max(ctx.CdThrowLeft, 60);
			}
			else {
				ctx.ShowLauncher = true;
				ctx.AimOverride = -MathHelper.PiOver2 + -ctx.NPC.spriteDirection * 0.08f;
			}
			SoundEngine.PlaySound(SoundID.MaxMana with { Pitch = -0.5f, Volume = 0.8f }, ctx.NPC.Center);
		}

		public override IVaultState<WBoss> OnUpdate(VaultStateMachine<WBoss> machine, WBoss ctx) {
			base.OnUpdate(machine, ctx);
			NPC npc = ctx.NPC;
			npc.velocity.X *= 0.85f;
			bool byHand = ByHand(ctx);
			int fireTick = byHand ? HandReleaseTick : LauncherFireTick;
			ctx.AnimOverrideRow = byHand
				? Math.Min(WBoss.RowThrowStart + Timer / WBoss.TicksPerFrame, WBoss.RowThrowEnd)
				: WBoss.SwingRow(Timer, LauncherFireTick);
			if (Timer == fireTick / 3 || Timer == fireTick * 2 / 3)
				SoundEngine.PlaySound(SoundID.MaxMana with { Pitch = Timer == fireTick / 3 ? -0.2f : 0.2f, Volume = 0.6f }, npc.Center);

			if (Timer == fireTick) {
				Vector2 from;
				if (byHand) {
					SoundEngine.PlaySound(SoundID.Item1 with { Pitch = -0.3f }, npc.Center);
					from = npc.Center + new Vector2(-npc.spriteDirection * 10f, -12f);
				}
				else {
					SoundEngine.PlaySound(SoundID.Item61 with { Pitch = -0.3f, Volume = 1f }, npc.Center);
					ctx.MuzzleFire(2.2f);
					from = ctx.MuzzlePos;
				}
				WBoss.ShakeNearby(npc.Center, 5, 700f);
				if (ctx.IsAuthority) {
					// 顶点约 52 tick 后、340px 高；略偏玩家一侧
					Vector2 vel = new(MathHelper.Clamp((ctx.Target.Center.X - from.X) / 60f, -3f, 3f), -13f);
					Projectile.NewProjectile(npc.GetSource_FromAI(), from, vel, ModContent.ProjectileType<WHEGrenade>(),
						(int)(WBoss.DmgHEGrenade * ctx.CurrentDamageScale), 1f, Main.myPlayer, ai0: 3f);
				}
			}

			if (Timer >= EndTick && ctx.IsAuthority)
				return ctx.SchedulerState();
			return null;
		}

		public override void OnExit(VaultStateMachine<WBoss> machine, WBoss ctx) {
			ctx.AimOverride = null;
			ctx.AnimOverrideRow = -1;
			ctx.PickDelay = 14;
		}
	}

	// ---------- 28 核爆「W 的大礼」（二阶段 ≤40%，首次到达必触发）：背身装定 + 警报加速 + 地面锁定圈 → 抛出黑红核骰 → 转身不看爆炸 ----------
	[VaultState(28, typeof(WBoss))]
	public class WNukeState : VaultState<WBoss>
	{
		private const int FrameHold = 5;
		private const int ArmTick = 5 * FrameHold;    // 装定咔哒
		private const int PressTick = 13 * FrameHold; // 第一张闪光帧：核骰上膛
		private const int ThrowTick = 17 * FrameHold; // 转回身出手 = 85
		private const int FlightTicks = 60;
		private const int EndTick = 100;

		public override void OnEnter(VaultStateMachine<WBoss> machine, WBoss ctx) {
			base.OnEnter(machine, ctx);
			NPC npc = ctx.NPC;
			ctx.CdNukeLeft = WBoss.CdNuke;
			ctx.NukePending = false;
			ctx.ShowLauncher = false;
			ctx.TelegraphLaser = 0f;
			// 锁定玩家此刻脚下的地面：圈从这一刻起就不再动，给足离开的时间
			ctx.NukeTarget = ctx.GroundBelow(ctx.Target.Bottom, 16);
			SoundEngine.PlaySound(SoundID.Roar with { Pitch = -0.3f, Volume = 0.7f }, npc.Center);
			if (ctx.IsAuthority)
				WBattleVisuals.Say("Nuke");
		}

		public override IVaultState<WBoss> OnUpdate(VaultStateMachine<WBoss> machine, WBoss ctx) {
			base.OnUpdate(machine, ctx);
			NPC npc = ctx.NPC;
			npc.velocity.X *= 0.8f;
			ctx.AnimOverrideRow = Math.Min(WBoss.RowCountdownStart + Timer / FrameHold, WBoss.RowCountdownEnd);

			if (Timer <= ThrowTick) {
				// 警报：蜂鸣间隔 10→3 tick 收紧、音高上扬；低频震屏一直压着
				int interval = (int)MathHelper.Clamp(10f - Timer / 9f, 3f, 10f);
				if (Timer % interval == 0)
					SoundEngine.PlaySound(SoundID.MaxMana with { Pitch = MathHelper.Lerp(-0.4f, 0.9f, Timer / (float)ThrowTick), Volume = 0.8f }, npc.Center);
				if (Timer % 10 == 0)
					WBoss.ShakeNearby(npc.Center, 2, 1400f);
				if (!Main.dedServ)
					Lighting.AddLight(npc.Center + new Vector2(-npc.spriteDirection * 6f, -4f), 1.2f * Timer / ThrowTick, 0.2f, 0.1f);
			}
			if (Timer == ArmTick)
				SoundEngine.PlaySound(SoundID.Unlock with { Volume = 0.8f, Pitch = -0.2f }, npc.Center);
			if (Timer == PressTick) {
				SoundEngine.PlaySound(SoundID.Unlock with { Volume = 1f, Pitch = -0.5f }, npc.Center);
				WBattleVisuals.Flash(0.2f, new Color(255, 90, 70));
			}

			if (Timer == ThrowTick) {
				SoundEngine.PlaySound(SoundID.Item1 with { Pitch = -0.4f, Volume = 1f }, npc.Center);
				if (ctx.IsAuthority && ctx.NukeTarget.HasValue) {
					Vector2 hand = npc.Center + new Vector2(-npc.spriteDirection * 10f, -14f);
					Vector2 target = ctx.NukeTarget.Value;
					Vector2 vel = WBoss.SolveLob(hand, target + new Vector2(0f, -12f), 0.2f, FlightTicks);
					Projectile.NewProjectile(npc.GetSource_FromAI(), hand, vel, ModContent.ProjectileType<WNukeDie>(),
						(int)(WBoss.DmgNuke * ctx.CurrentDamageScale), 10f, Main.myPlayer,
						ai0: 0f, ai1: target.X, ai2: target.Y);
				}
				// 圈交给骰子画；她自己转过身去
				ctx.NukeTarget = null;
				ctx.FaceAwayTicks = 110;
			}

			if (Timer >= EndTick && ctx.IsAuthority)
				return new WStandState();
			return null;
		}

		public override void OnExit(VaultStateMachine<WBoss> machine, WBoss ctx) {
			ctx.NukeTarget = null;
			ctx.ShowLauncher = true;
			ctx.AnimOverrideRow = -1;
			ctx.PickDelay = 24;
		}
	}

	// ---------- 29 烟幕地狱（二阶段 ≤45%，场上 ≥2 团烟）：举枪点火，全场烟团由灰转橙、噼啪作响，由近到远依次炸成火球 ----------
	[VaultState(29, typeof(WBoss))]
	public class WIgniteState : VaultState<WBoss>
	{
		private const int FireTick = 24;
		private const int EndTick = 52;

		public override void OnEnter(VaultStateMachine<WBoss> machine, WBoss ctx) {
			base.OnEnter(machine, ctx);
			ctx.CdIgniteLeft = WBoss.CdIgnite;
			ctx.ShowLauncher = true;
			ctx.TelegraphLaser = 0f;
			SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.8f, Pitch = -0.3f }, ctx.NPC.Center);
			if (ctx.IsAuthority) {
				// 烟团从现在开始转橙，第一团 84 tick 后炸，其余由近到远每 6 tick 一团
				ctx.IgniteAllSmoke(84, 6, (int)(WBoss.DmgIgnite * ctx.CurrentDamageScale));
				WBattleVisuals.Say("Ignite");
			}
		}

		public override IVaultState<WBoss> OnUpdate(VaultStateMachine<WBoss> machine, WBoss ctx) {
			base.OnUpdate(machine, ctx);
			NPC npc = ctx.NPC;
			npc.velocity.X *= 0.85f;
			ctx.AnimOverrideRow = WBoss.SwingRow(Timer, FireTick);
			ctx.TelegraphLaser = Timer < FireTick - 2 ? 0.4f * Timer / FireTick : 0f;

			if (Timer == FireTick) {
				// 点火弹只是演出：一道亮线射向天空，真正的火由烟团自己倒数
				SoundEngine.PlaySound(SoundID.Item61 with { Pitch = 0.4f, Volume = 0.8f }, npc.Center);
				ctx.MuzzleFire(1.3f);
				if (!Main.dedServ) {
					Vector2 dir = ctx.AimRotation.ToRotationVector2();
					for (int i = 0; i < 6; i++) {
						var p = new Common.Particle.DefaultParticle(ctx.MuzzlePos + dir * i * 14f, dir * 18f, 20, 1.6f, new Color(255, 170, 80), true) {
							Deformation = new Vector2(0.15f, 1.2f),
						};
						p.Spawn();
					}
				}
			}

			if (Timer >= EndTick && ctx.IsAuthority)
				return new WStandState();
			return null;
		}

		public override void OnExit(VaultStateMachine<WBoss> machine, WBoss ctx) {
			ctx.TelegraphLaser = 0f;
			ctx.AnimOverrideRow = -1;
			ctx.PickDelay = 14;
		}
	}

	// ---------- 27 地毯轰炸（二阶段）：地面上依次亮起五个 X，激光沿地面推进，五发定时红桃K 弹沿线连爆成一道朝玩家推进的墙 ----------
	[VaultState(27, typeof(WBoss))]
	public class WCarpetState : VaultState<WBoss>
	{
		private const int Markers = 5;
		private const int MarkerGap = 100;      // 落点间距 px
		private const int FirstMarkerTick = 8;
		private const int MarkerInterval = 8;   // 每 8 tick 亮一个 X
		private const int FirstFire = 52;
		private const int FireInterval = 8;
		private const int EndTick = 104;
		private const float RoundSpeed = 20f;

		private readonly Vector2[] points = new Vector2[Markers];
		private bool planned;

		public override void OnEnter(VaultStateMachine<WBoss> machine, WBoss ctx) {
			base.OnEnter(machine, ctx);
			ctx.CdCarpetLeft = WBoss.CdCarpet;
			ctx.CdKingLeft = Math.Max(ctx.CdKingLeft, 240);
			ctx.ShowLauncher = true;
			ctx.TelegraphLaser = 0f;
			ctx.ExtraMarkers.Clear();
			planned = false;
			SoundEngine.PlaySound(SoundID.MaxMana with { Pitch = -0.4f, Volume = 0.8f }, ctx.NPC.Center);
		}

		/// <summary>各端本地按同样规则算落点：从 W 前方 120px 起，沿地面每 100px 一个，朝玩家方向</summary>
		private void Plan(WBoss ctx) {
			NPC npc = ctx.NPC;
			float dir = -npc.spriteDirection; // 面向
			for (int i = 0; i < Markers; i++) {
				Vector2 p = npc.Bottom + new Vector2(dir * (120f + i * MarkerGap), -8f);
				points[i] = ctx.GroundBelow(p, 14);
			}
			planned = true;
		}

		public override IVaultState<WBoss> OnUpdate(VaultStateMachine<WBoss> machine, WBoss ctx) {
			base.OnUpdate(machine, ctx);
			NPC npc = ctx.NPC;
			npc.velocity.X *= 0.8f;
			if (!planned)
				Plan(ctx);

			if (Timer < FirstFire) {
				ctx.AnimOverrideRow = WBoss.SwingRow(Timer, FirstFire);
				// X 一个个亮起来，激光跟着最新亮起的那个走
				int lit = Math.Min(Markers, Math.Max(0, (Timer - FirstMarkerTick) / MarkerInterval + 1));
				if (Timer >= FirstMarkerTick && lit > ctx.ExtraMarkers.Count) {
					ctx.ExtraMarkers.Add(points[lit - 1]);
					SoundEngine.PlaySound(SoundID.MaxMana with { Pitch = -0.3f + 0.15f * lit, Volume = 0.55f }, points[lit - 1]);
				}
				if (ctx.ExtraMarkers.Count > 0) {
					Vector2 aim = ctx.ExtraMarkers[^1] + new Vector2(0f, -10f);
					ctx.AimOverride = (aim - ctx.LauncherAnchor).ToRotation();
					ctx.TelegraphLaser = Timer < FirstFire - 4 ? 0.3f + 0.5f * (Timer / (float)FirstFire) : 0f;
				}
			}
			else {
				ctx.TelegraphLaser = 0f;
				int sinceFire = Timer - FirstFire;
				ctx.AnimOverrideRow = sinceFire < Markers * FireInterval
					? (sinceFire % FireInterval < 3 ? WBoss.RowSwingStart + 3 : WBoss.RowSwingStart + 2)
					: WBoss.RowSwingEnd;
				if (sinceFire % FireInterval == 0 && sinceFire / FireInterval < Markers) {
					int i = sinceFire / FireInterval;
					Vector2 aim = points[i] + new Vector2(0f, -14f);
					ctx.AimOverride = (aim - ctx.LauncherAnchor).ToRotation();
					SoundEngine.PlaySound(SoundID.Item61 with { Pitch = -0.2f + 0.08f * i }, npc.Center);
					ctx.MuzzleFire(1.4f);
					if (ctx.IsAuthority) {
						Vector2 muzzle = ctx.MuzzlePos;
						Vector2 delta = aim - muzzle;
						int fuse = Math.Max(3, (int)(delta.Length() / RoundSpeed));
						Projectile.NewProjectile(npc.GetSource_FromAI(), muzzle, delta.SafeNormalize(Vector2.UnitX) * RoundSpeed,
							ModContent.ProjectileType<WLipstickRound>(), (int)(WBoss.DmgCarpetRound * ctx.CurrentDamageScale), 2f, Main.myPlayer,
							ai0: 3f, ai1: -fuse);
					}
					// 打掉一个就熄掉一个 X
					if (ctx.ExtraMarkers.Count > 0)
						ctx.ExtraMarkers.RemoveAt(0);
				}
			}

			if (Timer >= EndTick && ctx.IsAuthority)
				return new WStandState();
			return null;
		}

		public override void OnExit(VaultStateMachine<WBoss> machine, WBoss ctx) {
			ctx.ExtraMarkers.Clear();
			ctx.AimOverride = null;
			ctx.TelegraphLaser = 0f;
			ctx.AnimOverrideRow = -1;
			ctx.PickDelay = 24;
		}
	}
}
