using ArknightsMod.Common.Particle;
using ArknightsMod.Common.VisualEffects;
using ArknightsMod.Content.Items;
using ArknightsMod.Content.Items.BattleRecords;
using ArknightsMod.Content.Projectiles.Bosses.W;
using InnoVault.StateMachines;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.W
{
	[AutoloadBossHead]
	public partial class WBoss : ModNPC, INpcStateContext
	{
		// ---- 帧表（0 基行号 = 策划案 1 基帧号 - 1；GIF 100ms/帧 = 6 tick/帧）----
		public const int RowSwingStart = 0, RowSwingEnd = 4;            //挥臂 1-5
		public const int RowJump = 5;                                   //跳跃 6
		public const int RowWalkStart = 6, RowWalkEnd = 19;             //行走 7-20
		public const int RowThrowStart = 20, RowThrowEnd = 27;          //抛雷引爆 21-28
		public const int RowSmokeStart = 28, RowSmokeEnd = 38;          //封烟跑路 29-39（37-39 帧自带渐隐，39 近全透明）
		public const int RowDownStart = 39, RowDownEnd = 45;            //倒地不起 40-46
		public const int RowCountdownStart = 46, RowCountdownEnd = 63;  //倒数计时 47-64（60-62 帧自带橙色闪光）
		public const int RowD12Start = 64, RowD12End = 75;              //D12 65-76
		public const int TicksPerFrame = 6;

		// ---- 冷却----
		// 节奏优先：演出招冷却压到 6~12s，让节奏调度器（WBoss.States.cs）随时有演出招可挑；
		// 策划案原值（红桃K 9s / 倒数 15s / 此面向敌 8s / D12 15s）在 2026-09-05 按「5 秒/10 秒原则」下调，见设计文档 v2
		public const int CdTeleport = 180, CdThrow = 180, CdKing = 360, CdCountdown = 600;
		public const int CdDodge = 600, CdShot = 180, CdClaymore = 360, CdD12 = 540;
		public const int CdHop = 240, CdTriple = 300, CdBurst = 240, CdMortar = 540;
		public const int CdDetonate = 720;
		public const int CdCluster = 360, CdJumpMine = 420;
		public const int CdMineRun = 720, CdDiceRain = 600, CdAllIn = 600, CdPerch = 720;
		public const int CdBlastJump = 480, CdAirStrafe = 600, CdFirework = 720, CdCarpet = 540;
		public const int CdNuke = 1080, CdIgnite = 900;

		/// <summary>二阶段 ≤40%：核爆解锁，且首次到达时必定作为下一招触发</summary>
		public const float NukeHpRatio = 0.40f;

		// ---- 血量解锁（只留少数几道，密度是第一原则）----
		public const float TripleTossHpRatio = 0.85f;  //一阶段 ≤85%：跳雷
		public const float DiceRainHpRatio = 0.45f;    //二阶段 ≤45%：骰子雨、烟带撒雷
		public const float MortarHpRatio = 0.35f;      //二阶段 ≤35%：曲射弹幕

		// ---- 伤害基准 ----
		// 敌对弹幕对玩家的实际伤害 ≈ damage 参数 ×2（经典），故 100% 攻击力(60) → 参数 30
		public const int DmgHEGrenade = 30;    //普攻手雷 100%
		public const int DmgKingRound = 60;    //红桃K 200%
		public const int DmgArcRound = 30;     //二阶段普攻 / 曲射弹 100%
		public const int DmgClaymore = 75;     //此面向敌 250%
		public const int DmgD12 = 90;          //D12 300%
		public const int DmgCarpetRound = 45;  //地毯弹 150%
		public const int DmgNuke = 90;         //核骰 300%（圈外零伤害，圈内全额）
		public const int DmgIgnite = 45;       //烟幕地狱 150%

		NPC INpcStateContext.Npc => NPC;

		/// <summary>状态机；ai[3] 为状态同步槽（服务端权威，客户端被动跟随）</summary>
		public VaultStateMachine<WBoss> Machine { get; private set; }

		/// <summary>阶段存 ai[2]（随原版 NPC 同步）：1=一阶段，2=二阶段</summary>
		public int Phase {
			get => (int)NPC.ai[2];
			set => NPC.ai[2] = value;
		}

		// 技能冷却与调度（只在服务端/单机端有决策意义，客户端本地递减仅作参考）
		public int CdTeleportLeft, CdThrowLeft, CdKingLeft, CdCountdownLeft;
		public int CdDodgeLeft, CdShotLeft, CdClaymoreLeft, CdD12Left;
		public int CdHopLeft, CdTripleLeft, CdBurstLeft, CdMortarLeft, CdDetonateLeft, CdClusterLeft, CdJumpMineLeft;
		public int CdMineRunLeft, CdDiceRainLeft, CdAllInLeft, CdPerchLeft;
		public int CdBlastJumpLeft, CdAirStrafeLeft, CdFireworkLeft, CdCarpetLeft, CdNukeLeft, CdIgniteLeft;
		public int PickDelay;           //技能间隙拍点 + 传送后不攻击的公平阀
		/// <summary>核爆锁定点（装定期间由 W 画圈，掷出后由核骰接着画）；null = 无</summary>
		public Vector2? NukeTarget;
		/// <summary>首次 ≤40% 置位：下一招必定是核爆</summary>
		public bool NukePending;
		/// <summary>核爆已解锁（二阶段到过 ≤40%）</summary>
		public bool NukeUnlocked { get; private set; }
		/// <summary>扔完核骰转身不看爆炸：剩余帧内朝向取反</summary>
		public int FaceAwayTicks;
		/// <summary>额外的瞄准线终点（世界坐标）：红桃K·满注用四条线画出 X；各端本地按目标位置填，PostDraw 画</summary>
		public readonly List<Vector2> ExtraLasers = new();
		/// <summary>地面 X 标记（世界坐标）：地毯轰炸的五个落点；PostDraw 加法批次里画</summary>
		public readonly List<Vector2> ExtraMarkers = new();
		/// <summary>D12 掷出 12 后置位：下一次调度无视冷却直接接红桃K（"JACKPOT"的兑现）</summary>
		public bool JackpotPending;

		// 红桃K 的翻牌演出：枪口上方一张 ♥K，前摇里慢慢转到侧立，开火瞬间翻出正面
		public float CardAlpha, CardFlip;
		/// <summary>发射器显隐的过渡量 0~1：跟随 ShowLauncher 渐入渐出，别"啪"地弹出来</summary>
		public float LauncherVis;
		/// <summary>受击白闪剩余帧</summary>
		public int HitFlash;
		/// <summary>残影强度 0~1：跃退/落地等有速度的动作打开，PreDraw 沿 oldPos 画拖影</summary>
		public float GhostTrail;
		public int LastAttackStateId = -1; //反连发：同技能不连续两次

		// 传送目标（服务端在状态 OnEnter 计算；客户端不使用，位置靠 NPC 同步）
		public Vector2 TeleportDest;
		public TeleportKind TeleportMode;

		// 动画：>=0 时 FindFrame 锁定该行（一次性动作由状态按 Timer 驱动）；-1 时走行走循环
		public int AnimOverrideRow = -1;
		// 二阶段/开火状态下绘制手持榴弹发射器
		public bool ShowLauncher;
		public float AimRotation;
		/// <summary>非空时发射器强制指向该角度（曲射弹幕朝天）</summary>
		public float? AimOverride;
		/// <summary>开火后坐：枪口上跳角，逐帧衰减</summary>
		public float AimRecoil;
		/// <summary>瞄准激光强度 0~1（各状态在前摇期间写入，开火瞬间归零）</summary>
		public float TelegraphLaser;
		/// <summary>枪口火光剩余帧</summary>
		public int MuzzleFlash;

		// 死亡演出结束后放行真死（CheckDead 用）
		public bool DeathRealKill;

		// 烟中冲刺（替代瞬移）：DashTicks 帧内从 DashFrom 直线滑到 TeleportDest（可穿墙），期间隐身、免伤、不碰撞
		public bool Dashing;
		public Vector2 DashFrom;
		public int DashTicks, DashTimer;
		public Vector2 DashDir => (TeleportDest - DashFrom).SafeNormalize(Vector2.UnitX * -NPC.spriteDirection);

		/// <summary>二阶段 ≤25%：天空全暗、红光、双管——"全都押上"</summary>
		public bool Desperate => Phase == 2 && NPC.life <= NPC.lifeMax * WBattleVisuals.DesperateHpRatio;
		private bool saidLowHp;

		/// <summary>由召唤骰子生成（ai[0]=1）：登场时已经站在烟团里，走出来即可；其他生成方式走"传到玩家身侧"的登场</summary>
		public bool SpawnedFromDie => NPC.ai[0] == 1f;

		public enum TeleportKind
		{
			Approach,   //一阶段：传送到与玩家之间的空地/平台
			DodgeAway,  //二阶段：向玩家反方向躲避
			Entrance,   //登场：玩家身侧 280~460px 的空地（原生刷怪点在屏外，靠这一跳把人带进画面）
			Perch,      //二阶段高台狙击：玩家上方 120~260px 的平台/台阶
			CrossOver,  //烟带撒雷：穿过玩家到对面 240~320px 的落点，一路撒雷
		}

		// ---- 难度参数 ----
		public float PhaseHpRatio => Main.masterMode ? 0.7f : 0.5f;          //大师 70% 血转二阶段
		public float SmokeDodgeChance => Main.masterMode ? 0.9f : 0.6f;      //烟内闪避
		public float SmokeCloudRadius => Main.masterMode ? 73f : 56f;        //烟团半径（大师 ×1.3）
		public float P2DamageScale => Main.masterMode ? 1.5f : 1f;           //大师二阶段伤害 ×1.5
		public static bool RollExpertPredict => Main.expertMode && Main.rand.NextBool(); //专家 50% 预判

		public Player Target => Main.player[NPC.target];

		public override void SetStaticDefaults() {
			Main.npcFrameCount[Type] = 76;
			NPCID.Sets.BossBestiaryPriority.Add(Type);
			NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
			NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
			// 残影用的位置缓存
			NPCID.Sets.TrailCacheLength[Type] = 6;
			NPCID.Sets.TrailingMode[Type] = 0;
			NPCID.Sets.NPCBestiaryDrawModifiers drawModifiers = new() {
				CustomTexturePath = "ArknightsMod/Content/NPCs/Enemy/W/WBoss_Preview",
				PortraitScale = 1f,
			};
			NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, drawModifiers);
		}

		public override void SetDefaults() {
			NPC.width = 30;
			NPC.height = 48;
			NPC.lifeMax = 2000;
			NPC.defense = 10;      //全难度通用
			NPC.damage = 60;
			NPC.knockBackResist = 0f;
			NPC.boss = true;
			NPC.aiStyle = -1;
			NPC.netAlways = true;
			NPC.npcSlots = 10f;
			NPC.value = Item.buyPrice(gold: 2);
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath1;
			Music = MusicID.Boss1; //占位曲目，待专属 BGM
			NPC.ai[2] = 1;         //初始一阶段
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) {
			bestiaryEntry.Info.AddRange(new List<IBestiaryInfoElement> {
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Surface,
				new FlavorTextBestiaryInfoElement("Mods.ArknightsMod.Bestiary.WBoss"),
			});
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot) {
			// 掉落占位：源石锭 + 作战记录；专属掉落与宝藏袋后续再定
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<OriginiumIngot>(), 1, 15, 25));
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<TacticalBattleRecord>(), 1, 2, 4));
		}

		/// <summary>把每次状态切换写进 client.log（权威端）：排查"她到底在切什么"用，稳定后可关</summary>
		public static bool TraceStates = true;

		private VaultStateMachine<WBoss> BuildMachine() {
			VaultStateMachine<WBoss> machine = VaultStateMachineBuilder.For<WBoss>(this)
				.WithNetSync(AiSlotNetSync<WBoss>.ForNpc(c => c.NPC, 3))
				// 血量阈值 → 二阶段转场（一次性；大师 70%，其余 50%）
				.Phase<WPhaseTransitState>(
					c => c.Phase == 1 && c.NPC.life <= c.NPC.lifeMax * c.PhaseHpRatio,
					label: "phase2")
				// 脱战/目标丢失（演出类状态除外）
				.AnyState().To<WDespawnState>().When(c => c.ShouldDespawn()).Priority(10).End()
				.Initial<WSpawnState>()
				.Build();
			if (TraceStates && IsAuthority) {
				machine.OnStateChanged += (prev, next, reason) => {
					Player t = Target;
					float dist = t.active ? Vector2.Distance(NPC.Center, t.Center) : -1f;
					Mod.Logger.Info($"[W] {prev?.StateName ?? "-"} -> {next.StateName} ({reason}) hp={NPC.life}/{NPC.lifeMax} phase={Phase} dist={dist:0} solidLos={(t.active && HasSolidLineOfSight(t))} platLos={(t.active && HasLineOfSight(t))} sinceSpec={SinceSpectacle} sinceAtk={SinceAttack}");
				};
			}
			return machine;
		}

		/// <summary>演出类状态：期间不脱战、不做贴地修正</summary>
		public bool InCinematic =>
			Machine?.CurrentState is WSpawnState or WDeathState or WDespawnState or WPhaseTransitState;

		/// <summary>传送类状态：期间位置由状态自己写，跳过贴地修正</summary>
		public bool InTeleport =>
			Machine?.CurrentState is WSmokeTeleportState or WDodgeTeleportState or WPhaseTransitState or WSpawnState or WDespawnState or WMineRunState;

		public bool ShouldDespawn() {
			if (InCinematic)
				return false;
			Player target = Target;
			if (target.dead || !target.active)
				return true;
			// 二阶段：玩家距离过远 → 脱战
			return Phase == 2 && Vector2.Distance(NPC.Center, target.Center) > 1800f;
		}

		public override void AI() {
			// 目标维护（先于状态机构建：登场状态的 OnEnter 要用目标位置算落点）
			if (NPC.target < 0 || NPC.target >= Main.maxPlayers || Main.player[NPC.target].dead || !Main.player[NPC.target].active)
				NPC.TargetClosest();
			Player target = Target;

			Machine ??= BuildMachine();

			// 脱战/死亡演出中不再续命，否则 EncourageDespawn 会被顶回去
			if (!target.dead && target.active && Machine.CurrentState is not (WDespawnState or WDeathState))
				NPC.timeLeft = 600;

			TickCooldowns();

			// 朝向：贴图朝右，目标在右 → -1（不翻转），与弑君者同款映射；行走态会在 OnUpdate 里按移动方向覆盖
			if (Machine.CurrentState is not (WDeathState or WDespawnState)) {
				NPC.spriteDirection = target.Center.X > NPC.Center.X ? -1 : 1;
				if (FaceAwayTicks > 0)
					NPC.spriteDirection = -NPC.spriteDirection; //扔完核骰背过身去，不看爆炸
			}

			// 核爆解锁：二阶段首次 ≤40% → 下一招必定是它
			if (IsAuthority && !NukeUnlocked && Phase == 2 && NPC.life <= NPC.lifeMax * NukeHpRatio) {
				NukeUnlocked = true;
				NukePending = true;
			}

			// 发射器指向：平滑追踪给一点重量感；站桩时枪口微晃；曲射时朝天
			float desiredAim = AimOverride ?? (target.Center - LauncherAnchor).ToRotation();
			if (AimOverride == null && Machine.CurrentState is WStandState)
				desiredAim += MathF.Sin(Main.GlobalTimeWrappedHourly * 2.3f) * 0.03f;
			// 发射器还没显出来时直接对准，免得刚亮出来先甩一圈
			AimRotation = LauncherVis <= 0.02f ? desiredAim : AimRotation.AngleLerp(desiredAim, 0.22f);
			AimRecoil *= 0.82f;
			if (MuzzleFlash > 0)
				MuzzleFlash--;
			if (HitFlash > 0)
				HitFlash--;
			// 背身不看爆炸的那几十帧把枪收着，否则枪口会"反手"指向背后的玩家
			LauncherVis = MathHelper.Clamp(LauncherVis + (ShowLauncher && FaceAwayTicks <= 0 ? 0.14f : -0.14f), 0f, 1f);
			GhostTrail = Math.Max(0f, GhostTrail - 0.1f);
			SinceSpectacle++;
			SinceAttack++;

			// 低血量只说一次
			if (IsAuthority && !saidLowHp && Desperate) {
				saidLowHp = true;
				WBattleVisuals.Say("LowHp");
			}

			Machine.Update();

			Move_PostState();
		}

		// ---- 烟中冲刺 ----

		/// <summary>开始冲刺：从当前位置到 TeleportDest，用时按距离 / pxPerTick 折算并夹在 [minTicks, maxTicks]</summary>
		public void Dash_Begin(int minTicks = 6, int maxTicks = 24, float pxPerTick = 40f) {
			DashFrom = NPC.Center;
			DashTimer = 0;
			DashTicks = (int)MathHelper.Clamp(Vector2.Distance(DashFrom, TeleportDest) / pxPerTick, minTicks, maxTicks);
			Dashing = true;
			NPC.noTileCollide = true;
			NPC.noGravity = true;
			NPC.velocity = Vector2.Zero;
			NPC.alpha = 255;
		}

		/// <summary>冲刺推进一帧；到点返回 true（已自动 Dash_End）两端各自推进，位置最终由服务端同步校正</summary>
		public bool Dash_Tick() {
			DashTimer++;
			float k = MathHelper.Clamp(DashTimer / (float)DashTicks, 0f, 1f);
			float eased = 1f - MathF.Pow(1f - k, 2.2f); //冲出去快，到点前刹
			NPC.Center = Vector2.Lerp(DashFrom, TeleportDest, eased);
			NPC.velocity = Vector2.Zero;
			NPC.alpha = 255;
			if (!Main.dedServ) {
				Vector2 dir = DashDir;
				for (int i = 0; i < 3; i++) {
					Dust d = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(14f, 18f) - dir * Main.rand.NextFloat(0f, 30f), DustID.Smoke,
						-dir * Main.rand.NextFloat(0.5f, 2f) + new Vector2(0f, -0.4f), 120, default, Main.rand.NextFloat(1.1f, 1.7f));
					d.color = Color.Lerp(WTelegraph.SmokeDark, WTelegraph.SmokeRed, Main.rand.NextFloat(0.2f, 0.7f));
					d.noGravity = true;
					d.fadeIn = 0.4f;
				}
				Lighting.AddLight(NPC.Center, 0.45f, 0.1f, 0.08f);
			}
			if (k >= 1f) {
				Dash_End();
				return true;
			}
			return false;
		}

		public void Dash_End() {
			Dashing = false;
			NPC.Center = TeleportDest;
			NPC.noTileCollide = false;
			NPC.noGravity = false;
			NPC.velocity = Vector2.Zero;
			if (IsAuthority)
				NPC.netUpdate = true;
		}

		// ---- 弹药登记（扫场上实现 IWOrdnance 的弹幕）----

		public int CountLiveOrdnance() {
			int count = 0;
			for (int i = 0; i < Main.maxProjectiles; i++) {
				Projectile p = Main.projectile[i];
				if (p.active && p.ModProjectile is IWOrdnance o && o.IsLive)
					count++;
			}
			return count;
		}

		/// <summary>
		/// 全场起爆：所有已布设弹药 ticks 帧后爆stagger &gt; 0 时按离 W 的水平距离排序，每颗多等 stagger 帧（顺序起爆）<br/>
		/// 仅权威端调用
		/// </summary>
		public void PrimeAllOrdnance(int ticks, int stagger = 0) {
			var live = new List<(float dist, IWOrdnance o)>();
			for (int i = 0; i < Main.maxProjectiles; i++) {
				Projectile p = Main.projectile[i];
				if (p.active && p.ModProjectile is IWOrdnance o && o.IsLive)
					live.Add((Math.Abs(p.Center.X - NPC.Center.X), o));
			}
			if (stagger > 0)
				live.Sort((a, b) => a.dist.CompareTo(b.dist));
			for (int i = 0; i < live.Count; i++)
				live[i].o.Prime(ticks + i * stagger);
		}

		/// <summary>
		/// 只起爆某一种弹药，按离 W 的水平距离排序逐颗错开（farthestFirst 为真时从最远的那颗开始，爆炸墙朝 W 滚过来）仅权威端
		/// </summary>
		public void PrimeOrdnanceOfType<T>(int firstTicks, int stagger, bool farthestFirst) where T : ModProjectile, IWOrdnance {
			var live = new List<(float dist, IWOrdnance o)>();
			for (int i = 0; i < Main.maxProjectiles; i++) {
				Projectile p = Main.projectile[i];
				if (p.active && p.ModProjectile is T t && t.IsLive)
					live.Add((Math.Abs(p.Center.X - NPC.Center.X), t));
			}
			live.Sort((a, b) => farthestFirst ? b.dist.CompareTo(a.dist) : a.dist.CompareTo(b.dist));
			for (int i = 0; i < live.Count; i++)
				live[i].o.Prime(firstTicks + i * stagger);
		}

		/// <summary>场上还没点火的烟团数</summary>
		public int CountSmokeClouds() {
			int type = ModContent.ProjectileType<WSmokeCloud>();
			int count = 0;
			for (int i = 0; i < Main.maxProjectiles; i++) {
				Projectile p = Main.projectile[i];
				if (p.active && p.type == type && p.ModProjectile is WSmokeCloud c && !c.Igniting)
					count++;
			}
			return count;
		}

		/// <summary>点燃全场烟团：按离 W 的距离由近到远逐个错开（仅权威端）</summary>
		public void IgniteAllSmoke(int firstTicks, int stagger, int damage) {
			int type = ModContent.ProjectileType<WSmokeCloud>();
			var clouds = new List<(float dist, WSmokeCloud c)>();
			for (int i = 0; i < Main.maxProjectiles; i++) {
				Projectile p = Main.projectile[i];
				if (p.active && p.type == type && p.ModProjectile is WSmokeCloud c)
					clouds.Add((Vector2.Distance(p.Center, NPC.Center), c));
			}
			clouds.Sort((a, b) => a.dist.CompareTo(b.dist));
			for (int i = 0; i < clouds.Count; i++)
				clouds[i].c.Ignite(firstTicks + i * stagger, damage);
		}

		/// <summary>在当前位置留一个假身（仅权威端）；朝向与本体一致</summary>
		public void SpawnDecoy() {
			int n = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Bottom.Y, ModContent.NPCType<WDecoy>(),
				0, ai0: 0f, ai1: NPC.spriteDirection);
			if (n >= 0 && n < Main.maxNPCs && Main.netMode == NetmodeID.Server)
				NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, n);
		}

		/// <summary>贴脸闪光弹：本地玩家在范围内才白屏并被推开（玩家速度归各自客户端所有，不能从服务端推）</summary>
		public void FlashbangNearby(float range, float push) {
			if (Main.dedServ)
				return;
			Player local = Main.LocalPlayer;
			if (!local.active || local.dead)
				return;
			float dist = Vector2.Distance(local.Center, NPC.Center);
			if (dist > range)
				return;
			float k = 1f - dist / range;
			WBattleVisuals.Flash(0.35f + 0.45f * k);
			if (dist < range * 0.4f) {
				Vector2 away = (local.Center - NPC.Center).SafeNormalize(Vector2.UnitX * -NPC.spriteDirection);
				local.velocity += away * push + new Vector2(0f, -push * 0.4f);
			}
		}

		private void TickCooldowns() {
			if (CdTeleportLeft > 0)
				CdTeleportLeft--;
			if (CdThrowLeft > 0)
				CdThrowLeft--;
			if (CdKingLeft > 0)
				CdKingLeft--;
			if (CdCountdownLeft > 0)
				CdCountdownLeft--;
			if (CdDodgeLeft > 0)
				CdDodgeLeft--;
			if (CdShotLeft > 0)
				CdShotLeft--;
			if (CdClaymoreLeft > 0)
				CdClaymoreLeft--;
			if (CdD12Left > 0)
				CdD12Left--;
			if (CdHopLeft > 0)
				CdHopLeft--;
			if (CdTripleLeft > 0)
				CdTripleLeft--;
			if (CdBurstLeft > 0)
				CdBurstLeft--;
			if (CdMortarLeft > 0)
				CdMortarLeft--;
			if (CdDetonateLeft > 0)
				CdDetonateLeft--;
			if (CdClusterLeft > 0)
				CdClusterLeft--;
			if (CdJumpMineLeft > 0)
				CdJumpMineLeft--;
			if (CdMineRunLeft > 0)
				CdMineRunLeft--;
			if (CdDiceRainLeft > 0)
				CdDiceRainLeft--;
			if (CdAllInLeft > 0)
				CdAllInLeft--;
			if (CdPerchLeft > 0)
				CdPerchLeft--;
			if (CdBlastJumpLeft > 0)
				CdBlastJumpLeft--;
			if (CdAirStrafeLeft > 0)
				CdAirStrafeLeft--;
			if (CdFireworkLeft > 0)
				CdFireworkLeft--;
			if (CdCarpetLeft > 0)
				CdCarpetLeft--;
			if (CdNukeLeft > 0)
				CdNukeLeft--;
			if (CdIgniteLeft > 0)
				CdIgniteLeft--;
			if (FaceAwayTicks > 0)
				FaceAwayTicks--;
			if (PickDelay > 0)
				PickDelay--;
			// 卡牌不在演出中时自然淡出
			if (CardAlpha > 0f && Machine?.CurrentState is not WKingHeartsState)
				CardAlpha = Math.Max(0f, CardAlpha - 0.08f);
		}

		// ---- 烟幕闪避（二阶段被动：W 处于任意烟团内时按概率闪避）----

		public bool InSmokeCloud() {
			int cloudType = ModContent.ProjectileType<WSmokeCloud>();
			for (int i = 0; i < Main.maxProjectiles; i++) {
				Projectile p = Main.projectile[i];
				if (p.active && p.type == cloudType && Vector2.Distance(p.Center, NPC.Center) <= p.ai[1])
					return true;
			}
			return false;
		}

		public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers) {
			if (Phase != 2 || !InSmokeCloud())
				return;
			if (Main.rand.NextFloat() >= SmokeDodgeChance)
				return;
			// 泰拉无闪避机制：压伤害至 1 + 免击退 + MISS 文本模拟
			modifiers.SetMaxDamage(1);
			modifiers.Knockback *= 0f;
			modifiers.DisableCrit();
			modifiers.HideCombatText();
			if (!Main.dedServ) {
				CombatText.NewText(NPC.Hitbox, Color.LightGray, Language.GetTextValue("Mods.ArknightsMod.WBossText.Dodge"));
				// 闪避时身形在烟里一晃：几缕烟从身上散开
				SmokeBurst(NPC.Center, 5, 1.6f);
			}
		}

		// ---- 额外同步：传送落点（客户端要在落点画预告烟）----

		public override void SendExtraAI(System.IO.BinaryWriter writer) {
			writer.WriteVector2(TeleportDest);
		}

		public override void ReceiveExtraAI(System.IO.BinaryReader reader) {
			TeleportDest = reader.ReadVector2();
		}

		// ---- 死亡演出：锁血 → 倒地动画 → 真死掉落（镜像 FrostNova 的 CheckDead 模式）----

		public override bool CheckDead() {
			if (DeathRealKill)
				return true;
			NPC.life = 1;
			NPC.dontTakeDamage = true;
			NPC.netUpdate = true;
			if (Main.netMode != NetmodeID.MultiplayerClient && Machine?.CurrentState is not WDeathState)
				Machine?.ChangeState(new WDeathState(), "death");
			return false;
		}

		public override void HitEffect(NPC.HitInfo hit) {
			if (Main.dedServ)
				return;
			// 受击：身体白闪一下 + 几粒火星，让每一下都"打到了"
			HitFlash = 6;
			for (int i = 0; i < 3; i++) {
				Dust d = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Blood, hit.HitDirection * 1.5f, -1f);
				d.scale = Main.rand.NextFloat(0.8f, 1.2f);
			}
			for (int i = 0; i < 4; i++) {
				Vector2 vel = new Vector2(hit.HitDirection * Main.rand.NextFloat(2f, 5f), Main.rand.NextFloat(-4f, -1f));
				var p = new DefaultParticle(NPC.Center, vel, Main.rand.Next(12, 20), Main.rand.NextFloat(0.7f, 1.1f), new Color(255, 200, 160), false) {
					Deformation = new Vector2(0.15f, 0.7f),
				};
				p.Spawn();
			}
		}

		/// <summary>贴图上本体的绘制中心（与原版 NPC 绘制公式一致：底边对齐 + 4px）</summary>
		private Vector2 BodyDrawCenter(Vector2 topLeft) =>
			new(topLeft.X + NPC.width * 0.5f, topLeft.Y + NPC.height - NPC.frame.Height * 0.5f + 4f + NPC.gfxOffY);

		private SpriteEffects BodyEffects => NPC.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

		public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
			if (NPC.IsABestiaryIconDummy || GhostTrail <= 0.01f || NPC.alpha > 200)
				return true;
			// 残影：沿位置缓存往后画几张越来越淡的本体
			Texture2D tex = TextureAssets.Npc[Type].Value;
			Vector2 origin = NPC.frame.Size() * 0.5f;
			for (int i = 1; i < NPC.oldPos.Length; i++) {
				float k = 1f - i / (float)NPC.oldPos.Length;
				Color c = new Color(200, 60, 60, 0) * (0.35f * k * GhostTrail);
				spriteBatch.Draw(tex, BodyDrawCenter(NPC.oldPos[i]) - screenPos, NPC.frame, c, NPC.rotation, origin, NPC.scale, BodyEffects, 0f);
			}
			return true;
		}

		// ---- 绘制 ----

		public Vector2 LauncherAnchor => NPC.Center + new Vector2(0f, 2f);

		/// <summary>枪口世界坐标（沿当前瞄准方向 30px）</summary>
		public Vector2 MuzzlePos => LauncherAnchor + AimRotation.ToRotationVector2() * 30f;

		public override void FindFrame(int frameHeight) {
			if (NPC.IsABestiaryIconDummy) {
				NPC.frame.Y = RowWalkStart * frameHeight;
				return;
			}
			// 一次性动作：状态按自身 Timer 锁行
			if (AnimOverrideRow >= 0) {
				NPC.frame.Y = AnimOverrideRow * frameHeight;
				NPC.frameCounter = 0;
				return;
			}
			// 行走循环 7-20：站定时锁首帧，步频随水平速度变化，避免原地滑步
			float speed = Math.Abs(NPC.velocity.X);
			if (speed < 0.25f) {
				NPC.frame.Y = RowWalkStart * frameHeight;
				NPC.frameCounter = 0;
				return;
			}
			NPC.frameCounter += 0.6 + speed * 0.35;
			if (NPC.frameCounter >= TicksPerFrame) {
				NPC.frameCounter -= TicksPerFrame;
				NPC.frame.Y += frameHeight;
			}
			if (NPC.frame.Y > RowWalkEnd * frameHeight || NPC.frame.Y < RowWalkStart * frameHeight)
				NPC.frame.Y = RowWalkStart * frameHeight;
		}

		public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
			if (NPC.IsABestiaryIconDummy)
				return;

			// 烟中冲刺：本体隐身，画一团拖尾的烟带代替她
			if (Dashing) {
				WTelegraph.DrawSmokeBank(spriteBatch, NPC.Center, DashDir, 1f);
				return;
			}

			// 世界层预警（不依赖发射器是否在手）：核爆锁定圈、地毯轰炸的落点 X
			if (NukeTarget.HasValue || ExtraMarkers.Count > 0) {
				WTelegraph.BeginAdditive(spriteBatch);
				if (NukeTarget.HasValue) {
					// 装定期间越来越急地跳
					float pulse = 0.75f + 0.25f * MathF.Sin(Main.GlobalTimeWrappedHourly * 14f);
					WTelegraph.DrawTargetRing(spriteBatch, NukeTarget.Value, Projectiles.Bosses.W.WNukeDie.BlastRadius, pulse);
				}
				for (int i = 0; i < ExtraMarkers.Count; i++) {
					float pulse = 0.6f + 0.4f * MathF.Sin(Main.GlobalTimeWrappedHourly * 10f - i * 0.9f);
					WTelegraph.DrawGroundX(spriteBatch, ExtraMarkers[i], 30f, pulse * (1f - 0.1f * i));
				}
				WTelegraph.EndAdditive(spriteBatch);
			}

			if (NPC.alpha > 200)
				return;

			// 受击白闪：本体再叠画一层加法白
			if (HitFlash > 0) {
				Texture2D body = TextureAssets.Npc[Type].Value;
				float f = HitFlash / 6f;
				spriteBatch.Draw(body, BodyDrawCenter(NPC.position) - screenPos, NPC.frame, new Color(255, 230, 220, 0) * (0.55f * f), NPC.rotation, NPC.frame.Size() * 0.5f, NPC.scale, BodyEffects, 0f);
			}

			if (LauncherVis <= 0.02f)
				return;
			// 手持榴弹发射器叠绘：贴图朝右，向左瞄准时垂直翻转防倒持；显隐走 LauncherVis 渐变（淡入 + 从握把处略微放大）
			Texture2D tex = ModContent.Request<Texture2D>("ArknightsMod/Content/NPCs/Enemy/W/WBossLauncher").Value;
			bool facingLeft = Math.Abs(MathHelper.WrapAngle(AimRotation)) > MathHelper.PiOver2;
			// 后坐：枪口向上跳（朝右为逆时针，朝左为顺时针）
			float rot = AimRotation + (facingLeft ? AimRecoil : -AimRecoil);
			SpriteEffects fx = SpriteEffects.None;
			Vector2 origin = new(12f, 20f); //握把锚点（右向贴图坐标，入游戏校准）
			if (facingLeft) {
				fx = SpriteEffects.FlipVertically;
				origin.Y = tex.Height - origin.Y;
			}
			float vis = LauncherVis;
			float scale = 0.8f + 0.2f * vis;
			Color launcherColor = drawColor * vis;
			// ≤25%：双管——同一张贴图在枪身法线方向上叠一根压暗的后管
			if (Desperate) {
				Vector2 up = rot.ToRotationVector2().RotatedBy(facingLeft ? MathHelper.PiOver2 : -MathHelper.PiOver2);
				spriteBatch.Draw(tex, LauncherAnchor + up * 5f - screenPos, null, launcherColor.MultiplyRGB(new Color(150, 150, 160)), rot, origin, scale, fx, 0f);
			}
			spriteBatch.Draw(tex, LauncherAnchor - screenPos, null, launcherColor, rot, origin, scale, fx, 0f);

			// 红桃K 的牌：悬在枪口上方，随枪口微微起伏
			if (CardAlpha > 0.01f) {
				Vector2 cardPos = LauncherAnchor + new Vector2(0f, -30f + MathF.Sin(Main.GlobalTimeWrappedHourly * 4f) * 2f);
				WTelegraph.DrawCard(spriteBatch, cardPos, MathF.Sin(Main.GlobalTimeWrappedHourly * 3f) * 0.12f, CardFlip, 1.3f, CardAlpha);
			}

			if (TelegraphLaser > 0.01f || MuzzleFlash > 0 || ExtraLasers.Count > 0) {
				Vector2 muzzle = MuzzlePos;
				WTelegraph.BeginAdditive(spriteBatch);
				// 满注的四条线：从枪口指向四个落点
				if (ExtraLasers.Count > 0) {
					float k = Math.Max(TelegraphLaser, 0.35f);
					foreach (Vector2 end in ExtraLasers) {
						Vector2 d = end - muzzle;
						WTelegraph.DrawLaser(spriteBatch, muzzle, d.ToRotation(), d.Length(), k);
					}
				}
				if (TelegraphLaser > 0.01f && ExtraLasers.Count == 0) {
					// 激光长度：扫到第一块实体砖为止（原版激光同款采样），再封顶在目标身上——红点落在人身上才像"被瞄了"
					float[] samples = new float[3];
					Collision.LaserScan(muzzle, AimRotation.ToRotationVector2(), 4f, 1400f, samples);
					float len = (samples[0] + samples[1] + samples[2]) / 3f;
					if (AimOverride == null && NPC.target >= 0 && NPC.target < Main.maxPlayers)
						len = Math.Min(len, Vector2.Distance(muzzle, Target.Center) + 20f);
					WTelegraph.DrawLaser(spriteBatch, muzzle, AimRotation, len, TelegraphLaser);
				}
				if (MuzzleFlash > 0)
					WTelegraph.DrawMuzzleFlash(spriteBatch, muzzle, AimRotation, MuzzleFlash / 8f);
				WTelegraph.EndAdditive(spriteBatch);
			}
		}

		// ---- 演出与手感辅助 ----

		/// <summary>给附近玩家一点震屏（爆点反馈；strength 为帧数）</summary>
		public static void ShakeNearby(Vector2 center, int strength, float range = 900f) {
			if (Main.dedServ)
				return;
			Player local = Main.LocalPlayer;
			if (local.active && !local.dead && Vector2.Distance(local.Center, center) <= range)
				local.GetModPlayer<ShakeEffectPlayer>().screenShakeTime = Math.Max(local.GetModPlayer<ShakeEffectPlayer>().screenShakeTime, strength);
		}

		/// <summary>封烟位置的烟雾爆点（两端各自播放）</summary>
		public static void SmokeBurst(Vector2 center, int count, float kick = 2.6f) {
			if (Main.dedServ)
				return;
			for (int i = 0; i < count; i++) {
				Vector2 vel = Main.rand.NextVector2Circular(kick, kick * 0.8f);
				Dust d = Dust.NewDustPerfect(center + Main.rand.NextVector2Circular(16f, 22f), DustID.Smoke, vel, 120, default, Main.rand.NextFloat(1.0f, 1.6f));
				d.color = Color.Lerp(new Color(90, 84, 84), new Color(190, 60, 50), Main.rand.NextFloat(0.1f, 0.75f));
				d.noGravity = Main.rand.NextBool(3);
				d.fadeIn = 0.25f;
			}
		}

		/// <summary>脚下起尘（起跳/落地）</summary>
		public static void FootDust(Vector2 bottom, int count, float kick = 2f) {
			if (Main.dedServ)
				return;
			for (int i = 0; i < count; i++) {
				Dust d = Dust.NewDustPerfect(bottom + new Vector2(Main.rand.NextFloat(-12f, 12f), -2f), DustID.Smoke,
					new Vector2(Main.rand.NextFloat(-kick, kick), Main.rand.NextFloat(-kick * 0.6f, -0.2f)), 130, default, Main.rand.NextFloat(0.8f, 1.2f));
				d.noGravity = false;
			}
		}

		/// <summary>
		/// 发射器开火的统一反馈：火光帧、后坐角、身体后坐、枪口烟与火星<br/>
		/// power 约 1 为普攻，2 为红桃K；各端各自播放
		/// </summary>
		public void MuzzleFire(float power) {
			MuzzleFlash = 8;
			AimRecoil = 0.22f * power;
			NPC.velocity.X += NPC.spriteDirection * 1.4f * power; //贴图朝右时 spriteDirection=-1，即向后
			if (Main.dedServ)
				return;
			Vector2 muzzle = MuzzlePos;
			Vector2 dir = AimRotation.ToRotationVector2();
			SmokeBurst(muzzle + dir * 6f, (int)(4 + 2 * power), 1.2f + 0.4f * power);
			int sparks = (int)(4 + 3 * power);
			for (int i = 0; i < sparks; i++) {
				Vector2 vel = dir.RotatedByRandom(0.35f) * Main.rand.NextFloat(6f, 11f + 3f * power);
				var p = new DefaultParticle(muzzle, vel, Main.rand.Next(14, 22), Main.rand.NextFloat(0.9f, 1.4f), new Color(255, 170, 90), true) {
					Deformation = new Vector2(0.12f, 0.75f) * Main.rand.NextFloat(0.8f, 1.3f),
				};
				p.Spawn();
			}
			Lighting.AddLight(muzzle, 1.0f, 0.55f, 0.2f);
		}

		/// <summary>专家模式 50% 概率预判目标轨迹的瞄准点；提前量按飞行时间估算并封顶</summary>
		public static Vector2 AimPoint(Player target, int flightTicks) {
			Vector2 point = target.Center;
			if (RollExpertPredict) {
				Vector2 lead = target.velocity * flightTicks;
				if (lead.Length() > 240f)
					lead = lead.SafeNormalize(Vector2.Zero) * 240f;
				point += lead;
			}
			return point;
		}

		/// <summary>
		/// 挥臂开火帧：开火前把 1-3 帧（举臂）均匀铺满前摇，开火帧 4 停 6 tick，随后 5 帧收势
		/// </summary>
		public static int SwingRow(int timer, int fireTick) {
			if (timer < fireTick) {
				int third = Math.Max(fireTick / 3, 1);
				return RowSwingStart + Math.Min(timer / third, 2);
			}
			return timer - fireTick < 6 ? RowSwingStart + 3 : RowSwingEnd;
		}

		/// <summary>封烟跑路帧：t 为该段动作内部计时（6 tick/帧），封顶在近全透明的末帧</summary>
		public static int SmokeRow(int t) => Math.Min(RowSmokeStart + t / TicksPerFrame, RowSmokeEnd);

		/// <summary>
		/// 封烟跑路的烟雾弹小演出：32-34 帧那颗飞向前上方的烟雾弹拖一条尘迹，落点起烟；<br/>
		/// t 为动作内部计时两端各自播放
		/// </summary>
		public void SmokeGrenadeBeat(int t) {
			if (Main.dedServ)
				return;
			Vector2 hand = NPC.Center + new Vector2(-NPC.spriteDirection * 6f, -14f);
			Vector2 land = NPC.Center + new Vector2(-NPC.spriteDirection * 24f, -38f);
			if (t >= 18 && t < 30) {
				Vector2 p = Vector2.Lerp(hand, land, (t - 18) / 12f);
				Dust d = Dust.NewDustPerfect(p, DustID.Smoke, new Vector2(0f, -0.4f), 150, default, 0.8f);
				d.noGravity = true;
			}
			if (t == 30) {
				SmokeBurst(land, 18, 3.0f);
				SmokeBurst(NPC.Center, 8, 1.8f);
				Terraria.Audio.SoundEngine.PlaySound(SoundID.Item66, NPC.Center);
			}
		}
	}
}
