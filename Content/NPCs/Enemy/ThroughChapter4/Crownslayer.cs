using ArknightsMod.Common.GlobalNPCs;
using ArknightsMod.Common.VisualEffects;
using ArknightsMod.Content.Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.ThroughChapter4
{
	[AutoloadBossHead]
	public partial class Crownslayer : ModNPC
	{
		public AIState LastSkill = AIState.Idle; // 记录上一个技能
		public int FogSkillCooldown = 0;
		public int ExSkillCooldown = 0;
		public int UtSkillCooldown = 0;
		public int DaggerSkillCooldown = 0;
		// 记录已经触发过的阶段：0=未触发, 1=75%, 2=40%, 3=10%
		public int PhaseLevel = 0;
		private Vector2 phaseOffset; // 用于记录相对于玩家的偏移点位
									 // 记录当前召唤的小怪 ID 列表，用于判断是否全部消灭
		private System.Collections.Generic.List<int> MinionWhoAmIs = new System.Collections.Generic.List<int>();
		public float grayScaleIntensity = 0f;

		// Skill_3 三飞刀时间轴（ExecuteSkill3）
		internal const int Skill3_VanishEnd = 12;
		internal const int Skill3_FirstAppearEnd = 48;
		internal const int Skill3_FirstRetreatEnd = 90;
		internal const int Skill3_DashEnd = 124;
		internal const int Skill3_SecondHoverEnd = 160;
		internal const int Skill3_End = 196;
		private const float Skill3CornerOffset = 220f;
		private const int Skill3RetreatFrameHold = 6;

		private Vector2 skill3DashStart;
		private int skill3RetreatVisualStep = -1;
		private int skill3RetreatTick;
		private int suppressSkill1AfterSkill3;
		private bool skill6Weakened; // 本次 Skill_6 是否为「一阶段远距离削弱版」
		private int skill6FarRangeTimer;  // 一阶段「连续超距」累计（只在 Idle 帧累加）
		private AIState pendingSkill = AIState.Idle; // 一阶段前摇结束后要切换到的技能
		private int skillWindupTimer;                // 前摇剩余帧
		private int spawnIntroTimer;                 // 出生演出剩余帧

		private Vector2 skill9DashStart;
		private Vector2 skill9DashGoal;
		private Vector2 skill7VApex;
		private Vector2 skill7SecondCorner;
		private int skill7CycleStep;
		private int skill7StepTimer;
		private bool skill7WillThrowDagger;
		private const int Skill9WindupEnd = 22;
		private const int Skill9DashEnd = 54;
		private const int Skill9RecoverEnd = 62;
		private const float Skill9DashOvershoot = 96f;
		private const float Skill7CornerOffsetX = 300f;
		private const float Skill7CornerOffsetY = 280f;
		private const float Skill7Leg1Span = 32f;
		private const int Skill7Leg1End = 34;
		private const float Skill7Leg2Span = 28f;
		private const int Skill7Leg2End = 30;

		// 技能6「飞刀雨」固定伤害：刻意不再从 NPC.damage 派生。
		// 敌方弹幕打玩家在原版要 ×2（Projectile.Damage），tModLoader 又会再乘一次难度倍率，
		// 派生写法在大师 + getfixedboi 下单发实伤可达 700+，故此处写死。
		private const int Skill6DaggerDamage = 20;
		private const int Skill6DaggerCount = 15;            // 满编飞刀雨：第 0/6/.../84 帧各一枚
		private const int Skill6DaggerInterval = 6;          // 每枚飞刀的发射间隔（帧）
		private const float Skill6FarRangeTiles = 22f;       // 一阶段超过该距离视为「够不着」，改用削弱版飞刀雨
		private const float Skill6TeleportRadiusTiles = 10f; // 削弱版收招后传送落点半径（格）
		private const int Skill6FarRangeDelay = 120;         // 连续超出射程 2 秒（120 帧）后才允许削弱版飞刀雨
		private const int SkillWindupFrames = 30;            // 一阶段所有技能的前摇（帧）
		private const int SpawnIntroFrames = 120;           // 出生演出时长：2 秒（隐身 + 免疫 + 原地烟雾）

		private static int daggerPredictLineType = -1;
		private static int DaggerPredictLineType =>
			daggerPredictLineType >= 0 ? daggerPredictLineType : daggerPredictLineType = ModContent.ProjectileType<DaggerPredictLine>();

		public override void SetStaticDefaults() {
			Main.npcFrameCount[Type] = 56;
			// 脱战保护：mod NPC 不在原版 DoesntDespawnToInactivity 的写死名单里，
			// 所以玩家被援军引离她一屏以上、持续 activeTime（750 帧 = 12.5 秒）后，
			// 原版 CheckActive 会静默把她 active = false（无烟雾、无渐隐）。
			// 该 set 让 CheckActive 直接跳过这段逻辑；代价是她会计入 npcSlots（对 boss 是期望行为）。
			NPCID.Sets.DoesntDespawnToInactivityAndCountsNPCSlots[Type] = true;
			NPCID.Sets.TrailCacheLength[Type] = 22;
			NPCID.Sets.TrailingMode[Type] = 0;
		}
		public override void SetDefaults() {
			NPC.ai[0] = (float)AIState.Idle; // 强制初始状态为 Idle
			StateTimer = 60;                // 给它 1 秒的出生缓冲时间，防止瞬间发动技能
			Main.npcFrameCount[NPC.type] = 56;
			NPC.lifeMax = 4200;
			NPC.boss = true;
			NPC.damage = 40;
			NPC.defense = 10;
			NPC.width = MoveDefaultHitboxWidth;
			NPC.height = 64;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath3;
			NPC.value = 100f;
			NPC.knockBackResist = 0f;
			NPC.aiStyle = -1; // 重要：不使用任何预设AI
			NPCID.Sets.BossBestiaryPriority.Add(Type);

			// 出生演出：初始完全透明（alpha 最高）+ 免疫伤害，2 秒后瞬间现身（见 ExecuteSpawnIntro）。
			// 注意：这里不能改 NPC.damage，否则 vanilla 的难度缩放会把 defDamage 一起算成 0。
			spawnIntroTimer = SpawnIntroFrames;
			NPC.alpha = 255;
			NPC.dontTakeDamage = true;
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot) {
			// 弑君者掉落：8 源石锭 & 600 合成玉
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<OriginiumIngot>(), 1, 8, 8));
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Orundum>(), 1, 600, 600));
		}
		// 定义状态枚举
		private int damage = 40; //为某些情况设置的伤害，记得跟初始（经典）伤害同步。
								 // 在 NPC 类中添加
		public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
			float healthPercent = (float)NPC.life / NPC.lifeMax;

			// 只更新二阶段特效强度，不在这里改写 NPC.alpha，避免把本体画成半透明。
			if (healthPercent <= 0.5f) {
				float fogTarget = 0.4f + (0.5f - healthPercent) * 0.8f;
				grayScaleIntensity = MathHelper.Lerp(grayScaleIntensity, fogTarget, 0.028f);
			}
			else {
				grayScaleIntensity = MathHelper.Lerp(grayScaleIntensity, 0f, 0.06f);
			}

			CrownslayerTrailEffects.DrawBossDashAfterimages(spriteBatch, NPC, drawColor, CurrentAnimation, CurrentAIState);
			CrownslayerTrailEffects.DrawBossDashTrail(spriteBatch, NPC, CurrentAnimation, CurrentAIState);

			return true;
		}
		public enum NPCState
		{
			Walk,
			Attack1,
			Attack2,
			JumpIn,
			Lurk,
			TeleportDown,
			Blank,
			JumpOut,
			Dodge
		}

		// NPC 类中引用
		public NPCState CurrentAnimation = NPCState.Walk; // 默认为走路

		public override void FindFrame(int frameHeight) {
			Move_RestoreCombatHitbox();

			// 动画播放速度：数值越小越快
			int frameSpeed = 6;

			if (CurrentAIState == AIState.Skill_3 && skill3RetreatVisualStep >= 0) {
				int pinnedRow = skill3RetreatVisualStep <= 0 ? 19 : 50;
				NPC.frame.Y = pinnedRow * frameHeight;
				NPC.frameCounter = 0;
				return;
			}

			NPC.frameCounter++;

			// 每一帧对应的起始位置和结束位置
			int startFrame = 0;
			int endFrame = 0;

			// 根据当前状态决定循环哪几帧
			switch (CurrentAnimation) {
				case NPCState.Walk:
					startFrame = 0;
					endFrame = 13;
					break;
				case NPCState.Attack1:
					startFrame = 14;
					endFrame = 19;
					break;
				case NPCState.Attack2:
					startFrame = 20;
					endFrame = 25;
					break;
				case NPCState.JumpIn:
					startFrame = 26;
					endFrame = 29;
					break;
				case NPCState.Lurk:
					startFrame = 30;
					endFrame = 39;
					break;
				case NPCState.TeleportDown:
					startFrame = 40;
					endFrame = 47;
					break;
				case NPCState.Blank:
					startFrame = 52;
					endFrame = 52;
					break;
				case NPCState.JumpOut:
					startFrame = 48;
					endFrame = 51;
					break;
				case NPCState.Dodge:
					startFrame = 53;
					endFrame = 55;
					break;
			}

			// 核心循环逻辑
			if (NPC.frameCounter >= frameSpeed) {
				NPC.frameCounter = 0;
				NPC.frame.Y += frameHeight;

				// 如果超过了当前状态的最后一帧，跳回该状态的第一帧
				if (NPC.frame.Y >= (endFrame + 1) * frameHeight || NPC.frame.Y < startFrame * frameHeight) {
					NPC.frame.Y = startFrame * frameHeight;
				}
			}
		}
		public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
			if (CurrentAnimation == NPCState.Blank || NPC.alpha >= 250)
				return;

			if (CurrentAIState == AIState.Skill_3 && skill3RetreatVisualStep == 2) {
				Texture2D texture = TextureAssets.Npc[NPC.type].Value;
				Rectangle frame = NPC.frame;
				Vector2 origin = frame.Size() * 0.5f;
				SpriteEffects effects = NPC.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
				Color ghostColor = drawColor * 0.5f;
				ghostColor.A = (byte)(drawColor.A / 2);
				spriteBatch.Draw(texture, NPC.Center - screenPos, frame, ghostColor, NPC.rotation, origin, NPC.scale, effects, 0f);
			}

			if (grayScaleIntensity > 0.02f) {
				CrownslayerTrailEffects.DrawBossOrbitLines(spriteBatch, NPC, grayScaleIntensity);
			}
		}
		public enum AIState
		{
			Idle,           // 常态（等待/冷却中）
			Skill_1,        // 一阶段横向斩击
			Skill_2,        // 投掷飞刀（无贴近冲刺）
			Skill_3,        // 技能3
			Skill_4,        // 技能4
			Skill_5,        // 技能5
			Skill_6,
			Skill_7,
			Skill_8,        // 斩击
			Skill_9,        // 突刺
			Recover,
			Summoning,
			Leaving         // 脱战：无玩家时定身冒烟渐隐，40 帧后清场离场（追加在末尾，保持既有枚举值不变）

		}

		// 2. 核心变量
		public AIState CurrentAIState {
			get => (AIState)NPC.ai[0];
			set => NPC.ai[0] = (float)value;
		}

		public float StateTimer {
			get => NPC.ai[1];
			set => NPC.ai[1] = value;
		}

		public override void AI() {
			// 正常 AI 始终使用完整战斗碰撞箱；这是 FindFrame 未执行时的保险恢复。
			Move_RestoreCombatHitbox();

			// ---- 出生演出：这两秒内原地冒烟、完全透明、不承受伤害，其余 AI 全部暂停 ----
			if (spawnIntroTimer > 0) {
				ExecuteSpawnIntro();
				return;
			}

			// 确保有目标，否则清空状态
			// 获取当前到目标的距离
			float healthPercent = (float)NPC.life / NPC.lifeMax;



			// 仅计算强度，不直接修改颜色
			if (healthPercent <= 0.5f) {
				float fogTarget = 0.4f + (0.5f - healthPercent) * 0.8f;
				grayScaleIntensity = MathHelper.Lerp(grayScaleIntensity, fogTarget, 0.028f);
			}
			else {
				grayScaleIntensity = MathHelper.Lerp(grayScaleIntensity, 0f, 0.06f);
			}
			if (grayScaleIntensity > 0.1f && NPC.alpha < 200) { // 隐身时不产生粒子
				if (Main.rand.NextBool(2)) {
					int dustType = Main.rand.NextBool() ? DustID.Smoke : DustID.Cloud;
					Dust d = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, dustType, 0, 0, 200, default, Main.rand.NextFloat(1f, 2f));

					d.noGravity = true;
					d.velocity = new Vector2(Main.rand.NextFloat(-0.35f, 0.35f), -Main.rand.NextFloat(0.35f, 1.2f));
					d.color = Color.Lerp(new Color(88, 78, 40), new Color(186, 154, 72), Main.rand.NextFloat(0.2f, 0.8f));
					d.fadeIn = 0.4f;
				}
			}
			if (NPC.life > NPC.lifeMax * 0.5f) {
				Music = MusicLoader.GetMusicSlot("ArknightsMod/Music/Crownslayer1");
			}
			if (NPC.life < NPC.lifeMax * 0.5f) {
				Music = MusicLoader.GetMusicSlot("ArknightsMod/Music/Crownslayer2");
			}
			if (suppressSkill1AfterSkill3 > 0)
				suppressSkill1AfterSkill3--;
			if (DaggerSkillCooldown > 0)
				DaggerSkillCooldown--;
			Move_BeginFrame();
			NPC.TargetClosest(true);
			Player target = Main.player[NPC.target];
			float distanceToTarget = Vector2.Distance(NPC.Center, target.Center);
			if (target.Center.X > NPC.Center.X) {
				NPC.spriteDirection = -1;
			}
			else {
				NPC.spriteDirection = 1;
			}
			// ---- 脱战：周围已无可作战玩家（全员死亡 / 离线 / 幽灵）----
			// 规格：速度立即归零 → 原地释放烟雾 → 40 帧内 alpha 渐增至 255（完全透明）
			//       → 清空全部弹幕与援军 → 脱离战斗。
			if (!TryGetNearestCombatPlayer(out Player combatTarget)) {
				EnterLeaving();
				ExecuteLeaving();
				return;
			}

			// 有人可打时永远以“最近的存活玩家”为准：多人局一人阵亡会自动转火，状态机不会卡死
			target = combatTarget;
			NPC.target = combatTarget.whoAmI;
			distanceToTarget = Vector2.Distance(NPC.Center, target.Center);


			// --- 阶段转场监测 ---
			if ((healthPercent <= 0.75f && PhaseLevel == 0) ||
				(healthPercent <= 0.40f && PhaseLevel == 1) ||
				(healthPercent <= 0.10f && PhaseLevel == 2)) {
				PhaseLevel++;
				CurrentAIState = AIState.Summoning;
				StateTimer = 0; // 重置计时器
				NPC.netUpdate = true;
			}
			// 3. 状态机逻辑
			switch (CurrentAIState) {
				case AIState.Idle:
					HandleIdle(target, distanceToTarget);
					break;
				case AIState.Skill_1:
					ExecuteSkill1(target);
					break;
				case AIState.Skill_2:
					ExecuteSkill2(target);
					break;
				case AIState.Skill_3:
					ExecuteSkill3(target);
					break;
				case AIState.Skill_4:
					ExecuteSkill4(target);
					break;
				case AIState.Skill_5:
					ExecuteSkill5(target);
					break;
				case AIState.Skill_6:
					ExecuteSkill6(target);
					break;
				case AIState.Skill_7:
					ExecuteSkill7(target);
					break;
				case AIState.Skill_8:
					ExecuteSkill8(target);
					break;
				case AIState.Skill_9:
					ExecuteSkill9(target);
					break;
				case AIState.Recover:
					ExecuteRecover();
					break;
				case AIState.Summoning:
					ExecuteSummoning(target);
					break;
				case AIState.Leaving:
					// 渐隐途中若有玩家复活或加入，继续把离场演完（而不是僵在原地隐身）
					ExecuteLeaving();
					break;
			}

			Move_AfterStateMachine(target);
			EmitPhaseOneSmokeDecor();
			Move_PrepareSlopeCollisionHitbox();
		}

		// 一阶段：瞬移/现身、路径位移与冲刺上的烟雾修饰（仅客户端）。
		private void EmitPhaseOneSmokeDecor() {
			if (Main.dedServ)
				return;

			float hpRatio = (float)NPC.life / NPC.lifeMax;
			if (hpRatio <= 0.5f)
				return;

			if (CurrentAIState != AIState.Skill_1 && CurrentAIState != AIState.Skill_2 && CurrentAIState != AIState.Skill_3
				&& CurrentAIState != AIState.Skill_5 && CurrentAIState != AIState.Skill_6 && CurrentAIState != AIState.Skill_7)
				return;

			if (NPC.alpha > 250)
				return;

			TryEmitPhaseOneSmokeFromPositionJump();
			TryEmitPhaseOneSmokeSkillPunctuation();
			TryEmitPhaseOneSmokeAlongPathAndVelocity();
		}

		private void TryEmitPhaseOneSmokeFromPositionJump() {
			if (NPC.oldPos == null || NPC.oldPos.Length == 0 || NPC.oldPos[0] == Vector2.Zero)
				return;

			Vector2 prevCenter = NPC.oldPos[0] + NPC.Size * 0.5f;
			float dist = Vector2.Distance(prevCenter, NPC.Center);
			if (dist < 48f || dist > 2000f)
				return;

			int count = (int)MathHelper.Clamp((dist / 70f) + 6f, 8f, 28f);
			EmitPhaseOneSmokeBurst(Vector2.Lerp(prevCenter, NPC.Center, 0.4f), count, 2.8f);
			EmitPhaseOneSmokeBurst(NPC.Center, Math.Max(4, count / 3), 1.8f);
		}

		private void TryEmitPhaseOneSmokeSkillPunctuation() {
			if (CurrentAIState == AIState.Skill_1 && (StateTimer == 16 || StateTimer == 30))
				EmitPhaseOneSmokeBurst(NPC.Center, StateTimer == 16 ? 16 : 14, StateTimer == 16 ? 2.6f : 2.4f);

			if (CurrentAIState == AIState.Skill_2 && StateTimer == 48)
				EmitPhaseOneSmokeBurst(NPC.Center, 12, 2.2f);

			if (CurrentAIState == AIState.Skill_3) {
				if (StateTimer == Skill3_VanishEnd)
					EmitPhaseOneSmokeBurst(NPC.Center, 18, 2.8f);
				if (StateTimer == 20)
					EmitPhaseOneSmokeBurst(NPC.Center, 12, 2.2f);
				if (StateTimer == Skill3_DashEnd - 34)
					EmitPhaseOneSmokeBurst(NPC.Center, 14, 2.4f);
				if (StateTimer == Skill3_DashEnd + 8)
					EmitPhaseOneSmokeBurst(NPC.Center, 12, 2.2f);
			}

			if (CurrentAIState == AIState.Skill_5 && (StateTimer == 96 || StateTimer == 162 || StateTimer == 228))
				EmitPhaseOneSmokeBurst(NPC.Center, 14, 2.6f);
		}

		private void TryEmitPhaseOneSmokeAlongPathAndVelocity() {
			if (NPC.alpha > 220)
				return;

			float v2 = NPC.velocity.LengthSquared();

			if (v2 >= 36f) {
				if (!Main.rand.NextBool(2))
					return;

				Vector2 back = -NPC.velocity.SafeNormalize(Vector2.Zero);
				if (back.LengthSquared() < 0.0001f)
					return;

				SpawnPhaseOneTrailSmoke(NPC.Center + Main.rand.NextVector2Circular(8f, 14f) - back * Main.rand.NextFloat(16f, 36f), back * Main.rand.NextFloat(0.9f, 2.4f), Main.rand.NextFloat(0.95f, 1.5f));
				return;
			}

			if (v2 >= 4f) {
				if (!Main.rand.NextBool(4))
					return;

				Vector2 dir = NPC.velocity.SafeNormalize(Main.rand.NextVector2Unit());
				SpawnPhaseOneTrailSmoke(NPC.Center + Main.rand.NextVector2Circular(6f, 10f), dir * Main.rand.NextFloat(0.35f, 1.1f), Main.rand.NextFloat(0.75f, 1.15f));
				return;
			}

			if (CurrentAIState == AIState.Skill_3 && StateTimer >= Skill3_FirstRetreatEnd && StateTimer < Skill3_DashEnd && NPC.alpha < 200) {
				if (!Main.rand.NextBool(3))
					return;

				SpawnPhaseOneTrailSmoke(NPC.Center + Main.rand.NextVector2Circular(10f, 16f), Main.rand.NextVector2Circular(0.2f, 0.45f), Main.rand.NextFloat(0.7f, 1.05f));
				return;
			}

			if (CurrentAIState == AIState.Skill_3
				&& ((StateTimer >= Skill3_FirstRetreatEnd - 36 && StateTimer < Skill3_FirstRetreatEnd)
					|| (StateTimer >= Skill3_SecondHoverEnd && StateTimer < Skill3_End))
				&& NPC.alpha < 200) {
				if (!Main.rand.NextBool(4))
					return;

				SpawnPhaseOneTrailSmoke(NPC.Center + Main.rand.NextVector2Circular(12f, 20f), Main.rand.NextVector2Circular(0.25f, 0.55f), Main.rand.NextFloat(0.65f, 0.95f));
			}
		}

		private static void EmitPhaseOneSmokeBurst(Vector2 center, int count, float maxKick) {
			for (int i = 0; i < count; i++) {
				Vector2 vel = Main.rand.NextVector2Circular(maxKick, maxKick * 0.85f);
				Dust d = Dust.NewDustPerfect(center + Main.rand.NextVector2Circular(20f, 26f), DustID.Smoke, vel, 130, default, Main.rand.NextFloat(0.9f, 1.45f));
				d.color = Color.Lerp(new Color(102, 94, 90), new Color(198, 64, 52), Main.rand.NextFloat(0.15f, 0.95f));
				d.noGravity = Main.rand.NextBool(3);
				d.fadeIn = 0.22f;
			}
		}

		private static void SpawnPhaseOneTrailSmoke(Vector2 spawn, Vector2 velocity, float scale) {
			Dust d = Dust.NewDustPerfect(spawn, DustID.Smoke, velocity, 140, default, scale);
			d.color = Color.Lerp(new Color(96, 86, 82), new Color(188, 58, 48), Main.rand.NextFloat(0.25f, 0.92f));
			d.noGravity = Main.rand.NextBool(3);
			d.fadeIn = 0.2f;
		}

		// --- 核心方法：处理常态模式与技能切换 ---
		private void HandleIdle(Player target, float distance) {
			SetPhysics(true, true);
			NPC.damage = NPC.defDamage;
			CurrentAnimation = NPCState.Walk;

			float maxSpeed = 2.4f;
			float acceleration = 0.2f;
			float friction = 0.15f;
			float deadzone = 12f;

			float diffX = target.Center.X - NPC.Center.X;
			bool wantsMoveX = Math.Abs(diffX) >= deadzone;

			if (wantsMoveX) {
				if (diffX > 0) {
					if (!NPC.collideX || Move_AllowHorizontalAccel(1))
						NPC.velocity.X += acceleration;
					else if (NPC.velocity.X > 0f)
						NPC.velocity.X = 0f;
				}
				else {
					if (!NPC.collideX || Move_AllowHorizontalAccel(-1))
						NPC.velocity.X -= acceleration;
					else if (NPC.velocity.X < 0f)
						NPC.velocity.X = 0f;
				}
			}
			else {
				if (NPC.velocity.X > 0f) {
					NPC.velocity.X -= friction;
					if (NPC.velocity.X < 0f)
						NPC.velocity.X = 0f;
				}
				else if (NPC.velocity.X < 0f) {
					NPC.velocity.X += friction;
					if (NPC.velocity.X > 0f)
						NPC.velocity.X = 0f;
				}
			}

			NPC.velocity.X = MathHelper.Clamp(NPC.velocity.X, -maxSpeed, maxSpeed);
			Move_TickIdleChase(target, wantsMoveX, diffX);

			if (target.Center.Y > NPC.Center.Y + 32f && Main.tile[(int)(NPC.Center.X / 16), (int)((NPC.position.Y + NPC.height + 8) / 16)].TileType == TileID.Platforms) {
				NPC.position.Y += 1f;
			}

			if (distance >= 32f)
				CurrentAnimation = NPCState.Walk;

			if (NPC.localAI[0] > 0)
				NPC.localAI[0]--;
			if (FogSkillCooldown > 0)
				FogSkillCooldown--;
			if (ExSkillCooldown > 0)
				ExSkillCooldown--;
			if (UtSkillCooldown > 0)
				UtSkillCooldown--;
			StateTimer--;

			// 一阶段远距离保底需要「连续超距」计时：只有持续脱离射程 2 秒才会放削弱版飞刀雨，
			// 避免玩家只是短暂拉开距离就被反复飞刀雨 + 传送追打。
			if (distance / 16f > Skill6FarRangeTiles)
				skill6FarRangeTimer++;
			else
				skill6FarRangeTimer = 0;

			// 一阶段前摇：技能已选好但还没开始，等 30 帧给玩家反应时间；前摇期间不再重新选技能。
			if (pendingSkill != AIState.Idle) {
				if (--skillWindupTimer <= 0) {
					CurrentAIState = pendingSkill;
					LastSkill = pendingSkill;
					StateTimer = 0;
					pendingSkill = AIState.Idle;
				}

				return;
			}

			if (Move_BlockSkillPick()) {
				StateTimer = Math.Max(StateTimer, 16f);
				return;
			}

			if (StateTimer <= 0) {
				float distanceInTiles = distance / 16f;
				float healthPercent = (float)NPC.life / NPC.lifeMax;
				var weightedSkills = BuildWeightedSkillPool(distanceInTiles, healthPercent, respectLastSkill: true);

				if (!TryChooseWeightedSkill(weightedSkills, out AIState chosen)) {
					weightedSkills = BuildWeightedSkillPool(distanceInTiles, healthPercent, respectLastSkill: false);
					TryChooseWeightedSkill(weightedSkills, out chosen);
				}

				if (weightedSkills.Count > 0 && chosen != AIState.Idle) {
					if (chosen == AIState.Skill_4)
						FogSkillCooldown = 20 * 60;
					if (chosen == AIState.Skill_6) {
						ExSkillCooldown = 10 * 60;
						skill6FarRangeTimer = 0; // 放完重新累计，避免连续刷
					}
					if (chosen == AIState.Skill_7)
						UtSkillCooldown = 8 * 60;

					if (healthPercent > 0.5f) {
						// 一阶段：先进 30 帧前摇，前摇结束才真正切换到技能状态
						pendingSkill = chosen;
						skillWindupTimer = SkillWindupFrames;
					}
					else {
						CurrentAIState = chosen;
						LastSkill = chosen;
						StateTimer = 0;
					}
				}
				else {
					StateTimer = healthPercent > 0.5f ? 12 : 24;
				}
			}
		}

		private System.Collections.Generic.List<(AIState skill, int weight)> BuildWeightedSkillPool(
			float distanceInTiles, float healthPercent, bool respectLastSkill)
		{
			var pool = new System.Collections.Generic.List<(AIState skill, int weight)>();
			bool skip(AIState skill) => respectLastSkill && LastSkill == skill;

			if (healthPercent > 0.5f) {
				if (distanceInTiles <= 10f && !skip(AIState.Skill_8))
					pool.Add((AIState.Skill_8, 48));

				if (distanceInTiles <= 14f && !skip(AIState.Skill_9))
					pool.Add((AIState.Skill_9, 44));

				if (suppressSkill1AfterSkill3 <= 0 && distanceInTiles >= 3f && distanceInTiles <= 22f && !skip(AIState.Skill_1))
					pool.Add((AIState.Skill_1, 40));

				if (distanceInTiles <= 18f && DaggerSkillCooldown <= 0 && !skip(AIState.Skill_2)
					&& LastSkill != AIState.Skill_2 && LastSkill != AIState.Skill_3)
					pool.Add((AIState.Skill_2, 14));

				if (DaggerSkillCooldown <= 0 && !skip(AIState.Skill_3)
					&& LastSkill != AIState.Skill_2 && LastSkill != AIState.Skill_3)
					pool.Add((AIState.Skill_3, 12));

				// 远距离保底：一阶段所有技能都够不着（超过 22 格）且已经连续超距 2 秒时，
				// 才用削弱版飞刀雨拉近身位——否则技能池会为空，她只能原地走路等玩家靠近。
				if (skill6FarRangeTimer >= Skill6FarRangeDelay
					&& distanceInTiles > Skill6FarRangeTiles && !skip(AIState.Skill_6))
					pool.Add((AIState.Skill_6, 60));
			}
			else {
				if (distanceInTiles <= 8f && !skip(AIState.Skill_8))
					pool.Add((AIState.Skill_8, 32));

				if (distanceInTiles <= 12f && !skip(AIState.Skill_9))
					pool.Add((AIState.Skill_9, 28));

				if (FogSkillCooldown <= 0 && !skip(AIState.Skill_4))
					pool.Add((AIState.Skill_4, 22));

				if (!skip(AIState.Skill_5))
					pool.Add((AIState.Skill_5, 22));

				if (ExSkillCooldown <= 0 && !skip(AIState.Skill_6))
					pool.Add((AIState.Skill_6, 24));

				if (UtSkillCooldown <= 0 && !skip(AIState.Skill_7))
					pool.Add((AIState.Skill_7, 52));
				else if (!skip(AIState.Skill_7))
					pool.Add((AIState.Skill_7, 28));
			}

			return pool;
		}
		public override void PostAI() {
			if (NPC.ai[2] > 0) {
				NPC.ai[2] -= 1f; // 逐渐衰减
			}
		}
		private static bool TryChooseWeightedSkill(System.Collections.Generic.List<(AIState skill, int weight)> pool, out AIState chosen)
		{
			chosen = AIState.Idle;
			if (pool == null || pool.Count == 0)
				return false;

			int totalWeight = 0;
			foreach (var entry in pool)
				totalWeight += entry.weight;

			if (totalWeight <= 0)
				return false;

			int roll = Main.rand.Next(totalWeight);
			int acc = 0;
			chosen = pool[0].skill;
			foreach (var entry in pool) {
				acc += entry.weight;
				if (roll < acc) {
					chosen = entry.skill;
					return true;
				}
			}

			return true;
		}

		private void SetPhysics(bool useGravity, bool collideWithTiles) {
			NPC.noGravity = !useGravity;
			NPC.noTileCollide = !collideWithTiles;
		}

		private void FaceTargetHorizontal(Player target) {
			NPC.spriteDirection = (target.Center.X > NPC.Center.X) ? -1 : 1;
		}

		private float GetSkill3RetreatDirX(Player target, bool secondRetreat) {
			float retreatDirX = Math.Sign(NPC.Center.X - target.Center.X);
			if (retreatDirX == 0)
				retreatDirX = secondRetreat ? target.direction : -target.direction;
			return retreatDirX;
		}

		private void BeginSkill3Retreat(Player target, bool secondRetreat) {
			float retreatDirX = GetSkill3RetreatDirX(target, secondRetreat);
			NPC.velocity = new Vector2(retreatDirX * 11f, -5f);
			SetPhysics(false, false);
			NPC.dontTakeDamage = false;
			NPC.damage = NPC.defDamage;
			skill3RetreatVisualStep = 0;
			skill3RetreatTick = 0;
		}

		private void UpdateSkill3RetreatMotion(Player target, bool secondRetreat) {
			NPC.velocity.Y += 0.28f;
			NPC.velocity.X *= 0.92f;
			FaceTargetHorizontal(target);
			CurrentAnimation = NPCState.Dodge;

			skill3RetreatTick++;
			if (skill3RetreatVisualStep < 0)
				skill3RetreatVisualStep = 0;
			if (skill3RetreatTick >= Skill3RetreatFrameHold) {
				skill3RetreatTick = 0;
				skill3RetreatVisualStep++;
				if (skill3RetreatVisualStep > 2)
					skill3RetreatVisualStep = 2;
			}
		}

		private void FinishDaggerSkill() {
			suppressSkill1AfterSkill3 = 90;
			DaggerSkillCooldown = Main.rand.Next(480, 661);
			ResetToIdle();
		}

		// 出生演出：生成点冒烟，本体 alpha 保持最高（完全不可见）且免疫伤害；
		// 两秒结束时 alpha 直接归零（瞬间现身，不做任何渐变），随后一切行为与原来完全一致。
		private void ExecuteSpawnIntro() {
			spawnIntroTimer--;

			// 出生即播放 Boss 音乐（演出期间 AI 提前 return，所以这里补上）
			if (NPC.life > NPC.lifeMax * 0.5f)
				Music = MusicLoader.GetMusicSlot("ArknightsMod/Music/Crownslayer1");
			else
				Music = MusicLoader.GetMusicSlot("ArknightsMod/Music/Crownslayer2");

			NPC.alpha = 255;            // 透明度保持最高（完全不可见）
			NPC.dontTakeDamage = true;  // 这两秒内不承受伤害
			NPC.damage = 0;             // 隐身期间也不撞伤玩家（与脱战/召唤隐身时一致）
			NPC.velocity *= 0.85f;

			// 生成处烟雾：起手三大团气团 + 浓烟，随后每 4 帧补一小股
			int elapsed = SpawnIntroFrames - spawnIntroTimer;
			if (elapsed == 1) {
				SpawnLeaveGasClouds();
				EmitLeaveSmoke(34, 3.2f, 26f);
			}
			else if (elapsed == 13) {
				SpawnLeaveGasCloud(0.7f, 1.2f);
				EmitLeaveSmoke(16, 2.4f, 34f);
			}
			else if (elapsed < SpawnIntroFrames && elapsed % 4 == 0) {
				EmitLeaveSmoke(6, 1.6f, 20f);
			}

			// 两秒到：透明度瞬间归零 + 恢复承受伤害，然后交回原有 AI
			if (spawnIntroTimer <= 0) {
				NPC.alpha = 0;
				NPC.dontTakeDamage = false;
				NPC.damage = NPC.defDamage;
				NPC.netUpdate = true;
				EmitLeaveSmoke(18, 2.4f, 30f);   // 现身瞬间补一小团烟
			}
		}

		private void ResetToIdle() {
			CurrentAIState = AIState.Idle;
			CurrentAnimation = NPCState.Walk;
			skill3RetreatVisualStep = -1;
			skill3RetreatTick = 0;
			NPC.alpha = 0;
			NPC.dontTakeDamage = false;
			SetPhysics(true, true);
			Move_ResetWatch();
			Move_SoftenLandingOnIdle();

			float lifeRatio = (float)NPC.life / NPC.lifeMax;
			float minTime;
			float maxTime;
			if (lifeRatio > 0.5f) {
				minTime = 0.2f;
				maxTime = 0.65f;
			}
			else {
				minTime = MathHelper.Lerp(0.45f, 1.2f, lifeRatio * 2f);
				maxTime = MathHelper.Lerp(1.2f, 2.0f, lifeRatio * 2f);
			}

			StateTimer = Main.rand.NextFloat(minTime, maxTime) * 60f;
		}
		// 削弱版飞刀雨的落点：玩家「上半圆」（sin <= 0，即高度不低于玩家）、半径 10 格的随机一点。
		private Vector2 PickTeleportPointAbove(Player target) {
			float angle = Main.rand.NextFloat(MathHelper.Pi, MathHelper.TwoPi);
			return FindSafeSpot(target.Center + angle.ToRotationVector2() * (Skill6TeleportRadiusTiles * 16f));
		}

		// 真正执行一次传送（清速度、清位置历史，避免旧轨迹被渲染成冲刺拖尾，同 Move_ForceUnstuck）。
		private void TeleportTo(Vector2 destination) {
			NPC.Center = destination;
			NPC.velocity = Vector2.Zero;
			NPC.netUpdate = true;

			if (NPC.oldPos != null)
				for (int i = 0; i < NPC.oldPos.Length; i++)
					NPC.oldPos[i] = Vector2.Zero;

			Move_ResetWatch();
		}

		private Vector2 FindSafeSpot(Vector2 currentPos) =>
			Move_FindOpen(currentPos, requireGround: false);

		// 冲刺时背后少量向后飘散的粒子（客户端）。
		private Vector2 GetBladeSlashSpawnOffset() => new Vector2(NPC.spriteDirection * -28f, -8f);

		private void SpawnCrownslayerSwordSlash(bool thrust, float rotation, Vector2? offset = null) {
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			Vector2 pos = NPC.Center;
			if (offset.HasValue)
				pos += offset.Value;

			int projIndex = Projectile.NewProjectile(
				NPC.GetSource_FromAI(),
				pos,
				Vector2.Zero,
				ModContent.ProjectileType<SwordSlashEffect>(),
				0, 0f, Main.myPlayer,
				thrust ? 1f : 0f,
				NPC.spriteDirection);

			if (projIndex < 0 || projIndex >= Main.maxProjectiles)
				return;

			Projectile slash = Main.projectile[projIndex];
			slash.rotation = rotation;
			slash.ai[2] = NPC.whoAmI;
			Vector2 relOffset = pos - NPC.Center;
			slash.localAI[1] = relOffset.X;
			slash.localAI[2] = relOffset.Y;
			slash.netUpdate = true;
		}

		private static float GetSlashRotationToward(Player target, NPC npc) {
			Vector2 toTarget = target.Center - npc.Center;
			if (toTarget.LengthSquared() < 1f)
				toTarget = new Vector2(-npc.spriteDirection, 0f);
			// 贴图正方向为 +X（右），直接指向玩家
			return toTarget.ToRotation();
		}

		private void SpawnCrownslayerSwordSlashAtBlade(bool thrust, float rotation) {
			SpawnCrownslayerSwordSlash(thrust, rotation, GetBladeSlashSpawnOffset());
		}

		private void EmitThrustBurstParticles() {
			if (Main.dedServ)
				return;

			for (int i = 0; i < 16; i++) {
				float angle = MathHelper.TwoPi * i / 16f;
				Vector2 vel = angle.ToRotationVector2() * Main.rand.NextFloat(2f, 5f);
				Dust d = Dust.NewDustPerfect(NPC.Center, DustID.GemRuby, vel, 0,
					Color.Lerp(new Color(255, 60, 20), new Color(255, 200, 80), Main.rand.NextFloat()),
					Main.rand.NextFloat(0.6f, 1.1f));
				d.noGravity = true;
			}
		}

		private void EmitDashAccentParticles(float intensity) {
			if (Main.dedServ)
				return;

			Vector2 back = -NPC.velocity.SafeNormalize(Vector2.Zero);
			if (back.LengthSquared() < 0.0001f)
				return;

			intensity = MathHelper.Clamp(intensity, 0.12f, 1.6f);
			if (!Main.rand.NextBool((int)Math.Max(2, 9 - (int)(intensity * 4f))))
				return;

			Vector2 spawn = NPC.Center + Main.rand.NextVector2Circular(10f, 16f) - back * Main.rand.NextFloat(14f, 28f);
			Vector2 vel = back * Main.rand.NextFloat(1.8f, 4.6f) + Main.rand.NextVector2Circular(0.45f, 0.95f);
			int dustType = Main.rand.NextBool(4) ? DustID.Torch : DustID.GemRuby;
			Dust d = Dust.NewDustPerfect(spawn, dustType, vel, 0, default, Main.rand.NextFloat(0.42f, 0.85f));
			d.color = Color.Lerp(new Color(110, 10, 22), new Color(255, 118, 38), Main.rand.NextFloat(0.25f, 0.9f));
			d.noGravity = true;
			d.fadeIn = 0.32f;
		}

		// --- 技能槽位 ---

		// 一阶段横向斩击：蓄力 → 侧闪 → 出刀横穿。
		private void ExecuteSkill1(Player target) {
			StateTimer++;
			FaceTargetHorizontal(target);
			NPC.dontTakeDamage = false;
			NPC.damage = NPC.defDamage;

			if (StateTimer < 16) {
				SetPhysics(true, true);
				CurrentAnimation = NPCState.Attack2;
				NPC.velocity.X *= 0.82f;
			}
			else if (StateTimer == 16) {
				SetPhysics(true, true);
				float flankSide = Math.Sign(target.Center.X - NPC.Center.X);
				if (flankSide == 0f)
					flankSide = -target.direction;

				NPC.Center = target.Center + new Vector2(-flankSide * 180f, -4f);
				NPC.velocity = Vector2.Zero;
				FaceTargetHorizontal(target);
				CurrentAnimation = NPCState.Lurk;
				NPC.netUpdate = true;

				for (int i = 0; i < 12; i++)
					Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Smoke, 0f, 0f, 100, default, 1.2f);
			}
			else if (StateTimer < 30) {
				SetPhysics(true, true);
				CurrentAnimation = NPCState.Attack2;
				NPC.velocity *= 0.90f;
			}
			else if (StateTimer == 30) {
				Vector2 slashDir = target.Center - NPC.Center;
				slashDir.Y *= 0.15f;
				if (slashDir.LengthSquared() < 1f)
					slashDir = new Vector2(-NPC.spriteDirection, 0f);
				slashDir.Normalize();

				float slashRot = GetSlashRotationToward(target, NPC);

				NPC.velocity = slashDir * 26f;
				CurrentAnimation = NPCState.Attack2;
				SetPhysics(false, false);
				SpawnCrownslayerSwordSlashAtBlade(thrust: false, rotation: slashRot);
				Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero,
					ModContent.ProjectileType<TransparentSlash>(), damage, 3f, Main.myPlayer);
				SoundEngine.PlaySound(SoundID.Item1, NPC.Center);
				NPC.netUpdate = true;
			}
			else if (StateTimer < 50) {
				SetPhysics(false, false);
				CurrentAnimation = NPCState.Attack2;
				NPC.velocity.X *= 0.90f;
				NPC.velocity.Y *= 0.45f;
				NPC.spriteDirection = NPC.velocity.X >= 0f ? -1 : 1;

				if (StateTimer == 38) {
					Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero,
						ModContent.ProjectileType<TransparentSlash>(), damage / 2, 2f, Main.myPlayer);
				}

				if (NPC.velocity.LengthSquared() > 36f)
					EmitDashAccentParticles(0.35f);
			}
			else {
				NPC.velocity *= 0.8f;
				Move_EndMelee(target);
			}
		}

		// 投掷三柄重力飞刀；已移除旧的闪现贴近与下落突刺段。
		private void ExecuteSkill2(Player target) {
			StateTimer++;
			FaceTargetHorizontal(target);
			SetPhysics(true, true);
			NPC.dontTakeDamage = false;

			if (StateTimer < 48) {
				CurrentAnimation = NPCState.Attack2;
				NPC.velocity.X *= 0.85f;
			}
			else if (StateTimer == 48) {
				Vector2 toTarget = target.Center - NPC.Center;
				if (toTarget.LengthSquared() < 1f)
					toTarget = new Vector2(-NPC.spriteDirection, 0f);
				Vector2 baseVel = Vector2.Normalize(toTarget) * 16f;
				for (int i = -1; i <= 1; i++) {
					Vector2 shotVel = baseVel.RotatedBy(MathHelper.ToRadians(i * 25f));
					Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, shotVel,
						ModContent.ProjectileType<GravityDagger>(), (int)(damage * 0.4f), 2f, Main.myPlayer);
				}

				SoundEngine.PlaySound(SoundID.Item71, NPC.Center);
			}
			else if (StateTimer < 80) {
				CurrentAnimation = NPCState.Attack2;
				NPC.velocity *= 0.9f;
			}
			else {
				FinishDaggerSkill();
			}
		}

		private void ExecuteSkill3(Player target) {
			StateTimer++;
			SetPhysics(false, false);

			// 世界坐标：右上方 → 左上方（不依赖玩家朝向 direction）
			Vector2 upperRight = target.Center + new Vector2(Skill3CornerOffset, -Skill3CornerOffset);
			Vector2 upperLeft = target.Center + new Vector2(-Skill3CornerOffset, -Skill3CornerOffset);

			if (StateTimer < Skill3_VanishEnd) {
				CurrentAnimation = NPCState.JumpOut;
				NPC.velocity *= 0.85f;
				NPC.alpha = (int)MathHelper.Lerp(0, 255, StateTimer / (float)Skill3_VanishEnd);
				NPC.dontTakeDamage = true;
				NPC.damage = 0;
			}
			else if (StateTimer < Skill3_FirstAppearEnd) {
				if (StateTimer == Skill3_VanishEnd) {
					NPC.Center = upperRight;
					NPC.alpha = 0;
					NPC.velocity = Vector2.Zero;
					NPC.dontTakeDamage = false;
					NPC.damage = NPC.defDamage;
					NPC.netUpdate = true;
				}

				CurrentAnimation = NPCState.Attack2;
				NPC.velocity = Vector2.Zero;
				FaceTargetHorizontal(target);

				if (StateTimer == 20)
					ShootDaggers(target);
			}
			else if (StateTimer < Skill3_FirstRetreatEnd) {
				if (StateTimer == Skill3_FirstAppearEnd)
					BeginSkill3Retreat(target, secondRetreat: false);

				UpdateSkill3RetreatMotion(target, secondRetreat: false);
			}
			else if (StateTimer < Skill3_DashEnd) {
				if (StateTimer == Skill3_FirstRetreatEnd) {
					NPC.dontTakeDamage = true;
					NPC.damage = 0;
					skill3DashStart = NPC.Center;
					NPC.velocity = Vector2.Zero;
					CurrentAnimation = NPCState.Attack2;
					NPC.netUpdate = true;
				}

				float dashSpan = Skill3_DashEnd - Skill3_FirstRetreatEnd;
				float t = MathHelper.Clamp((StateTimer - Skill3_FirstRetreatEnd) / dashSpan, 0f, 1f);
				t = t * t * (3f - 2f * t);
				NPC.Center = Vector2.Lerp(skill3DashStart, upperLeft, t);
				NPC.velocity = Vector2.Zero;
				NPC.spriteDirection = upperLeft.X >= skill3DashStart.X ? -1 : 1;
				EmitDashAccentParticles(0.5f);
			}
			else if (StateTimer < Skill3_SecondHoverEnd) {
				if (StateTimer == Skill3_DashEnd) {
					NPC.Center = upperLeft;
					NPC.velocity = Vector2.Zero;
					NPC.alpha = 0;
					NPC.netUpdate = true;
				}

				NPC.dontTakeDamage = false;
				NPC.damage = NPC.defDamage;
				NPC.velocity *= 0.8f;
				CurrentAnimation = NPCState.Attack2;
				FaceTargetHorizontal(target);

				if (StateTimer == Skill3_DashEnd + 8)
					ShootDaggers(target);
			}
			else if (StateTimer < Skill3_End) {
				if (StateTimer == Skill3_SecondHoverEnd)
					BeginSkill3Retreat(target, secondRetreat: true);

				UpdateSkill3RetreatMotion(target, secondRetreat: true);
			}
			else {
				NPC.velocity = Vector2.Zero;
				skill3RetreatVisualStep = -1;
				FinishDaggerSkill();
			}
		}

		private void ExecuteSkill4(Player target) {
			StateTimer++;
			CurrentAnimation = NPCState.TeleportDown;
			NPC.velocity.X *= 0.5f;
			SetPhysics(true, true);

			// 动画中段：释放中心静止的法术匕首
			if (StateTimer == 24) {
				SoundEngine.PlaySound(SoundID.Item74, NPC.Center);
				if (Main.netMode != NetmodeID.MultiplayerClient) {
					// 生成 RedMagicBlade，不再跟随玩家斜角，而是固定在原地
					Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero,
						ModContent.ProjectileType<RedMagicBlade>(), (int)(damage * 1f), 0f, Main.myPlayer, target.whoAmI);
				}
			}

			// 动画播放完毕后的消失逻辑
			if (StateTimer >= 48 && StateTimer < 168) { // 48帧播放完动画，消失2秒（120帧）
				NPC.alpha = 255;
				NPC.dontTakeDamage = true; // 隐身期间不可被选中
			}

			// 2秒后（StateTimer = 48 + 120 = 168）现身
			if (StateTimer >= 168) {
				NPC.alpha = 0;
				NPC.dontTakeDamage = false;

				
				CurrentAIState = AIState.Idle;
				StateTimer = 0;
			}
		}

		private void ExecuteSkill5(Player target) {
			StateTimer++;
			if (CurrentAnimation == NPCState.JumpOut || CurrentAnimation == NPCState.Blank || NPC.alpha > 150) {
				NPC.dontTakeDamage = true;
				NPC.damage = 0;
			}
			else {
				// 在 Attack2 悬浮阶段或 TeleportDown 下砸阶段恢复受击和伤害
				NPC.dontTakeDamage = false;
				NPC.damage = NPC.defDamage;
			}
			if (StateTimer < 60) {
				NPC.noGravity = true;
				NPC.noTileCollide = true;
				CurrentAnimation = NPCState.Lurk;
				NPC.velocity *= 0.8f;
			}
			else if (StateTimer < 84) {
				CurrentAnimation = NPCState.JumpOut;
				if (StateTimer == 60) {
					phaseOffset = new Vector2(0, -360f);
				}
			}
			else if (StateTimer < 150) {
				HandleAerialDaggerPass(target, 84, new Vector2(0f, -360f), new Vector2(-360f, -360f));
			}
			else if (StateTimer < 216) {
				HandleAerialDaggerPass(target, 150, new Vector2(-360f, -360f), new Vector2(360f, -360f));
			}
			else if (StateTimer < 282) {
				HandleAerialDaggerPass(target, 216, new Vector2(360f, -360f), new Vector2(0f, -400f));
			}
			else {
				int finalTimer = (int)StateTimer - 282;
				if (finalTimer < 15) { // 快速瞬移到上方准备
					NPC.alpha = 255;
					NPC.Center = target.Center + new Vector2(0, -400f); // 18格高
					NPC.noTileCollide = true;
				}
				else if (finalTimer < 39) { // 下落开始
					if (finalTimer == 15) {
						NPC.alpha = 0;
						NPC.velocity = Vector2.Zero; // 瞬移后先急停
						NPC.noTileCollide = false; // 恢复碰撞
						CurrentAnimation = NPCState.TeleportDown;
					}
					if (finalTimer == 21) {
						for (int i = -5; i <= 5; i++) {
							if (i == 0)
								continue;

							Vector2 spawnPos = new Vector2(NPC.Center.X + (i * 120f), NPC.Center.Y - 800f - (i * 200f));

							// 这里的参数必须补全，否则小刀 AI 内部检测不到 owner 会直接 Kill
							Projectile.NewProjectile(
								NPC.GetSource_FromAI(),
								spawnPos,
								new Vector2(0, 10f),               // 初始速度给 0，防止它直接飞走
								ModContent.ProjectileType<GravityDagger>(),
								(int)(damage * 0.4f),
								3f,
								Main.myPlayer,
								NPC.whoAmI,                 // 对应 GravityDagger AI 里的 ai[0]
								1f                          // 对应 GravityDagger AI 里的 ai[1] (模式开关)
							);
						}
						NPC.velocity = new Vector2(0, 25f); // 垂直急速下坠
					}



					// 落地判定
					if (NPC.velocity.Y == 0 && finalTimer > 20) {
						ResetToIdle();
					}

				}
				else {
					ResetToIdle();
				}
			}
		}
		private void HandleAerialDaggerPass(Player target, int cycleStart, Vector2 teleportOffset, Vector2 nextOffset) {
			int localTimer = (int)StateTimer - cycleStart;
			if (localTimer < 12) {
				NPC.alpha = 255;
				NPC.velocity = Vector2.Zero;
				NPC.noGravity = true;
				NPC.noTileCollide = true;
				CurrentAnimation = NPCState.Blank;
				NPC.Center = target.Center + teleportOffset;
				phaseOffset = nextOffset;
				return;
			}

			if (localTimer == 12) {
				NPC.alpha = 0;
				NPC.noGravity = true;
				NPC.noTileCollide = true;
				CurrentAnimation = NPCState.Lurk;
				Vector2 dashDir = (target.Center + new Vector2(target.direction * 48f, -24f) - NPC.Center).SafeNormalize(Vector2.UnitX * target.direction);
				NPC.velocity = dashDir * 18f;
				NPC.spriteDirection = dashDir.X >= 0f ? -1 : 1;
				SoundEngine.PlaySound(SoundID.Item71, NPC.Center);
			}

			if (localTimer < 30) {
				NPC.alpha = 0;
				NPC.noGravity = true;
				NPC.noTileCollide = true;
				CurrentAnimation = NPCState.Lurk;
				return;
			}

			if (localTimer < 42) {
				NPC.alpha = 0;
				NPC.noGravity = true;
				NPC.noTileCollide = true;
				CurrentAnimation = NPCState.Attack2;
				NPC.velocity *= 0.84f;
				NPC.spriteDirection = (target.Center.X > NPC.Center.X) ? -1 : 1;
				if (localTimer == 32) {
					ShootMoreDaggers(target);
				}
				return;
			}

			if (localTimer == 42) {
				NPC.noGravity = false;
				NPC.noTileCollide = false;
				CurrentAnimation = NPCState.TeleportDown;
				NPC.velocity = new Vector2(NPC.velocity.X * 0.45f, 5.5f);
			}

			NPC.alpha = 0;
			CurrentAnimation = NPCState.TeleportDown;
			if (NPC.velocity.Y < 16f) {
				NPC.velocity.Y += 0.5f;
			}
		}
		private void ExecuteSkill6(Player target) {
			StateTimer++;
			// 一阶段（HP > 50%）只可能通过「远距离保底」选到本技能，因此一律走削弱版：
			// 飞刀数量与伤害减半，扔完立刻传送到玩家上方 10 格的上半圆并收招（不做后续冲刺/下砸）。
			if (StateTimer == 1)
				skill6Weakened = (float)NPC.life / NPC.lifeMax > 0.5f;
			// 技能期间手动控制重力，防止自然重力干扰手感

			// --- 阶段 1: 站在原地投掷匕首 (0 - 90帧) ---
			if (StateTimer < 90) {
				NPC.noGravity = false;
				NPC.noTileCollide = false;
				NPC.velocity *= 0.8f;
				int daggerCount = skill6Weakened ? Skill6DaggerCount / 2 : Skill6DaggerCount;
				int daggerDamage = skill6Weakened ? Skill6DaggerDamage / 2 : Skill6DaggerDamage;
				if (StateTimer % Skill6DaggerInterval == 0 && StateTimer / Skill6DaggerInterval < daggerCount) {
					Vector2 targetPos = target.Center + new Vector2(Main.rand.NextFloat(-330, 330), Main.rand.NextFloat(-320, -150));
					Vector2 launchVel = (targetPos - NPC.Center) / 25f;
					Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, launchVel,
						ModContent.ProjectileType<StallDagger>(), daggerDamage, 2f, Main.myPlayer, NPC.whoAmI, targetPos.Y);
					SoundEngine.PlaySound(SoundID.Item71, NPC.Center);

				}

				// 削弱版：这一轮飞刀扔完立刻传送拉近身位，然后收招
				if (skill6Weakened && StateTimer >= Skill6DaggerInterval * daggerCount) {
					TeleportTo(PickTeleportPointAbove(target));
					ResetToIdle();
					return;
				}
			}

			// --- 阶段 2: 第一次冲刺预备 (90 - 120帧) ---
			else if (StateTimer < 120) {
				NPC.velocity *= 0.9f;
				if (StateTimer == 119) {
					
					Vector2 dashTarget = target.Center + new Vector2(0, Main.rand.NextFloat(-300, -100));
					NPC.velocity = (dashTarget - NPC.Center).SafeNormalize(Vector2.Zero) * 30f;
					SoundEngine.PlaySound(SoundID.Item71, NPC.Center);
				}
			}

			// --- 阶段 3: 第一次冲刺 -> 减速 -> 下坠 (120 - 200帧) ---
			else if (StateTimer < 200) {
				// 当到达玩家头顶附近或经过一段时间后开始减速并下坠
				if (StateTimer > 130) {
					NPC.velocity.X *= 0.97f; // 快速减损横向惯性
					NPC.velocity.Y += 0.9f;  // 给予强大的向下加速度
					NPC.noTileCollide = false; // 开始与地形交互
				}

				// 碰撞地面检测：第一次落地
				if (StateTimer > 145 && NPC.collideY && StateTimer <180) {
					StateTimer = 180; // 强制跳转到第二次冲刺准备
					NPC.velocity = Vector2.Zero; // 落地急停
				}
			}

			// --- 阶段 4: 第二次冲刺 -> 悬停 (200 - 280帧) ---
			else if (StateTimer < 280) {
				if (StateTimer == 201) { // 再次暴力起跳
					NPC.noTileCollide=true; // 再次悬浮，穿过地形
					Vector2 secondDashTarget = target.Center + new Vector2(0, -350);
					NPC.velocity = (secondDashTarget - NPC.Center).SafeNormalize(Vector2.Zero) * 25f;
					SoundEngine.PlaySound(SoundID.ForceRoar, NPC.Center);
				}

				// 检测是否接近玩家头顶目标点，实现“急停悬停”
				float distToTarget = Vector2.Distance(NPC.Center, target.Center + new Vector2(0, -350));
				if (StateTimer > 210 && distToTarget < 100f) {
					NPC.velocity *= 0.2f; // 急刹车
					StateTimer = 280; // 直接进入悬停计时
				}
			}

			// --- 阶段 5: 悬停 0.5秒 (280 - 310帧) -> 下砸 ---
			else {
				// 280 - 310 帧 (30帧 = 0.5秒)：死死锁在玩家头顶位置
				if (StateTimer < 310) {
					
					NPC.velocity = Vector2.Zero;
				}
				if (StateTimer == 304) {
					CurrentAnimation = NPCState.TeleportDown;
				}
				// 310 帧：最终暴烈下砸
				else if (StateTimer == 310) {
					CurrentAnimation = NPCState.TeleportDown;
					NPC.noTileCollide = false;
					NPC.noGravity = false;
					NPC.velocity = new Vector2(0, 35f); // 比第一次更快
					SoundEngine.PlaySound(SoundID.Item14, NPC.Center); // 下砸瞬间的爆气声
				}

				// 检测最终落地
				if (StateTimer > 311 && (NPC.velocity.Y == 0 || NPC.collideY)) {
					NPC.ai[2] = 2f; // 信号：未激活的小刀全部同步下坠
					ExecuteSlamImpact();
					StateTimer = 0;
					ResetToIdle();
				}
			}

			// 冲刺期间残影
			if (StateTimer > 120 && NPC.velocity.Length() > 10f) {
				if (Main.rand.NextBool(2)) {
					Dust d = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.LifeDrain, 0, 0, 100, default, 1.5f);
					d.noGravity = true;
				}
			}
		}


		private void ExecuteSkill7(Player target) {
			StateTimer++;
			Player focusTarget = GetNearestThreatPlayer(target);

			// --- 阶段 1: 起手式（插刀、起雾） ---
			if (StateTimer < 60) {
				skill7CycleStep = 0;
				skill7StepTimer = 0;
				CurrentAnimation = NPCState.TeleportDown;
				NPC.velocity.X *= 0.5f;
				NPC.velocity.Y = 10f;
				NPC.noTileCollide = false;
				if (StateTimer == 30) {
					// 在原地留下一把插在地上的红色刀（装饰性弹幕）
					Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Bottom, Vector2.Zero,
						ModContent.ProjectileType<GroundDagger>(), 0, 0, Main.myPlayer);
					SoundEngine.PlaySound(SoundID.Item71, NPC.Center);
				}
			}

			// --- 阶段 2: 一排预警飞刀 → 清场 → V 字机动 → 短暂隐身，循环 ---
			else if (StateTimer < 960) {
				grayScaleIntensity = 0.85f;
				NPC.noGravity = true;
				NPC.noTileCollide = true;
				skill7StepTimer++;

				switch (skill7CycleStep) {
					// 0：定角、只召唤一排预警飞刀，等预警+飞刀打完
					case 0:
						NPC.alpha = 255;
						NPC.velocity = Vector2.Zero;
						CurrentAnimation = NPCState.Blank;

						if (skill7StepTimer == 1) {
							SetupSkill7VPath(focusTarget, out float barrageAngle);
							TrySpawnPhase2PredictBarrage(focusTarget, barrageAngle, 5);
							NPC.netUpdate = true;
						}

						if (skill7StepTimer > 8 && !HasActivePhase2PredictLines()) {
							Skill7AdvanceStep(1);
						}

						break;

					// 1：V 字第一腿 — 上角 → 玩家（顶点）
					case 1:
						NPC.alpha = 0;
						CurrentAnimation = NPCState.Lurk;

						if (skill7StepTimer == 1) {
							skill9DashStart = NPC.Center;
							SoundEngine.PlaySound(SoundID.Item71, NPC.Center);
						}

						Skill7ApplyVLerp(skill7StepTimer, Skill7Leg1Span, skill9DashStart, skill7VApex);

						if (skill7StepTimer >= Skill7Leg1End) {
							NPC.Center = skill7VApex;
							Skill7AdvanceStep(2);
						}

						break;

					// 2：V 字第二腿 — 玩家（顶点）→ 对角
					case 2:
						NPC.alpha = 0;
						CurrentAnimation = NPCState.Lurk;

						if (skill7StepTimer == 1)
							SoundEngine.PlaySound(SoundID.Item71, NPC.Center);

						Skill7ApplyVLerp(skill7StepTimer, Skill7Leg2Span, skill7VApex, skill7SecondCorner);

						if (skill7StepTimer >= Skill7Leg2End) {
							NPC.Center = skill7SecondCorner;
							Skill7AdvanceStep(3);
						}

						break;

					// 3：V 字结束后有时转身对玩家补一发普通飞刀
					case 3:
						NPC.alpha = 0;
						NPC.velocity *= 0.88f;
						FaceTargetHorizontal(focusTarget);

						if (skill7StepTimer == 1)
							skill7WillThrowDagger = Main.rand.NextBool(38);

						CurrentAnimation = skill7WillThrowDagger ? NPCState.Attack2 : NPCState.Lurk;

						if (skill7WillThrowDagger && skill7StepTimer == 14)
							ShootSingleDagger(focusTarget);

						if (skill7StepTimer >= (skill7WillThrowDagger ? 26 : 8))
							Skill7AdvanceStep(4);

						break;

					// 4：短隐身，下一圈再从一排飞刀开始
					case 4:
						NPC.alpha = 255;
						NPC.velocity = Vector2.Zero;
						CurrentAnimation = NPCState.Blank;

						if (skill7StepTimer >= 18)
							Skill7AdvanceStep(0);

						break;

					default:
						Skill7AdvanceStep(0);
						break;
				}
			}

			// --- 阶段 3: 结束 ---
			else {
				NPC.alpha = 0;
				NPC.dontTakeDamage = false;
				NPC.velocity.X = 0;
				NPC.noTileCollide = false;
				StateTimer = 0;
				ResetToIdle();
				
			}
		}

		// 独立斩击：Attack2 挥刀 + 精灵图帧 2~5 刀光。
		private void ExecuteSkill8(Player target) {
			StateTimer++;
			FaceTargetHorizontal(target);
			SetPhysics(true, true);

			if (StateTimer < 18) {
				CurrentAnimation = NPCState.Attack2;
				NPC.velocity.X *= 0.72f;

				if (StateTimer == 8) {
					Vector2 nudge = target.Center - NPC.Center;
					nudge.Y = 0f;
					if (nudge.LengthSquared() > 1f) {
						nudge.Normalize();
						NPC.position += nudge * 10f;
					}
				}
			}
			else if (StateTimer == 18) {
				CurrentAnimation = NPCState.Attack2;
				float slashRot = GetSlashRotationToward(target, NPC);
				Vector2 push = target.Center - NPC.Center;
				push.Y *= 0.15f;
				if (push.LengthSquared() < 1f)
					push = new Vector2(-NPC.spriteDirection, 0f);
				push.Normalize();
				NPC.velocity = push * 14f;

				SpawnCrownslayerSwordSlashAtBlade(thrust: false, rotation: slashRot);
				Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero,
					ModContent.ProjectileType<TransparentSlash>(), damage, 0f, Main.myPlayer);
				SoundEngine.PlaySound(SoundID.Item1, NPC.Center);
			}
			else if (StateTimer < 36) {
				CurrentAnimation = NPCState.Attack2;
				NPC.velocity *= 0.86f;
			}
			else {
				Move_EndMelee(target);
			}
		}

		// 独立突刺：蓄力 → 固定时长 Lerp 冲刺（穿透玩家一点）→ 收招。
		private void ExecuteSkill9(Player target) {
			StateTimer++;
			FaceTargetHorizontal(target);
			NPC.dontTakeDamage = false;
			NPC.damage = NPC.defDamage;

			if (StateTimer < Skill9WindupEnd) {
				SetPhysics(true, true);
				CurrentAnimation = NPCState.Attack2;
				NPC.velocity *= 0.72f;

				if (StateTimer == 14 && !Main.dedServ) {
					for (int i = 0; i < 8; i++) {
						Dust d = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(12f, 18f),
							DustID.GemRuby, Main.rand.NextVector2Circular(0.6f, 0.6f), 0,
							new Color(255, 80, 40), Main.rand.NextFloat(0.5f, 0.9f));
						d.noGravity = true;
					}
				}
			}
			else if (StateTimer == Skill9WindupEnd) {
				SetPhysics(false, false);
				CurrentAnimation = NPCState.JumpIn;
				Vector2 toTarget = target.Center - NPC.Center;
				if (toTarget.LengthSquared() < 1f)
					toTarget = new Vector2(-NPC.spriteDirection, 0f);
				Vector2 dir = Vector2.Normalize(toTarget);
				skill9DashStart = NPC.Center;
				skill9DashGoal = target.Center + dir * Skill9DashOvershoot;
				NPC.velocity = Vector2.Zero;
				NPC.netUpdate = true;

				EmitThrustBurstParticles();
				SpawnCrownslayerSwordSlashAtBlade(thrust: true, rotation: dir.ToRotation());
				Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero,
					ModContent.ProjectileType<TransparentSlash>(), damage, 2f, Main.myPlayer);
				SoundEngine.PlaySound(SoundID.Item71, NPC.Center);
			}
			else if (StateTimer < Skill9DashEnd) {
				SetPhysics(false, false);
				CurrentAnimation = NPCState.JumpIn;
				float span = Skill9DashEnd - Skill9WindupEnd;
				float t = MathHelper.Clamp((StateTimer - Skill9WindupEnd) / span, 0f, 1f);
				t = t * t * (3f - 2f * t);
				NPC.Center = Vector2.Lerp(skill9DashStart, skill9DashGoal, t);
				NPC.velocity = Vector2.Zero;
				NPC.spriteDirection = skill9DashGoal.X >= skill9DashStart.X ? -1 : 1;
				if (StateTimer % 3 == 0)
					EmitDashAccentParticles(0.45f);
			}
			else if (StateTimer < Skill9RecoverEnd) {
				SetPhysics(true, true);
				CurrentAnimation = NPCState.JumpIn;
				NPC.velocity *= 0.82f;
			}
			else {
				Move_EndMelee(target);
			}
		}

		private void ExecuteRecover() {
			StateTimer--;
			SetPhysics(true, true);
			NPC.velocity *= 0.8f;
			CurrentAnimation = NPCState.Walk;
			if (StateTimer <= 0) {
				CurrentAIState = AIState.Idle;
				StateTimer = 30;
				CurrentAnimation = NPCState.Walk;
				Move_ResetWatch();
			}
		}
		private void ExecuteSummoning(Player target) {
			StateTimer++;
			SetPhysics(false, false);
			NPC.dontTakeDamage = true;
			NPC.velocity *= 0.95f;

			// --- 阶段1：播放 JumpOut 渐隐 (30 帧) ---
			if (StateTimer < 24) {
				CurrentAnimation = NPCState.JumpOut;
				NPC.damage = 0; // 核心修改：隐身期间不撞伤玩家
				NPC.alpha = (int)MathHelper.Lerp(0, 255, StateTimer / 30f);
			}
			// --- 阶段2：进入召唤时间轴 ---
			else {
				CurrentAnimation = NPCState.Blank;
				NPC.alpha = 255;

				// 这里的计时器从 0 开始计算召唤时间 (StateTimer - 30)
				int summonTimer = (int)StateTimer - 24;
				if ((int)(summonTimer % 300) == 0) {
					float angleDeg = Main.rand.NextFloat(0f, 180f);
					TrySpawnPhase2PredictBarrage(target, MathHelper.ToRadians(angleDeg), 5);
				}
				// 根据不同的 PhaseLevel 执行不同的分批召唤计划
				switch (PhaseLevel) {
					case 1: // 75% 血量阶段
							// 第 0 秒：左边刷 A，右边刷 B
						if (summonTimer == 0) {
							SpawnMinion(ModContent.NPCType<HoundPro>(), true);  // true 代表左边
							SpawnMinion(ModContent.NPCType<SoldierLeader>(), false); // false 代表右边
						}
						if (summonTimer == 90) {
							SpawnMinion(ModContent.NPCType<Soldier>(), false);
						}
						// 第 3 秒 (180 帧)：左边再刷一个 C
						if (summonTimer == 180) {
							SpawnMinion(ModContent.NPCType<CrossbowmanLeader>(), true);
						}
						break;

					case 2: // 40% 血量阶段
						if (summonTimer == 0)
							SpawnMinion(ModContent.NPCType<LightShield>(), true);
						if (summonTimer == 60)
							SpawnMinion(ModContent.NPCType<DoubleSword>(), false); // 1秒后
						if (summonTimer == 120)
							SpawnMinion(ModContent.NPCType<Seniorcaster>(), true); // 2秒后
						if (summonTimer == 180)
							SpawnMinion(ModContent.NPCType<SoldierLeader>(), false);
						break;

					case 3: // 10% 血量阶段
						if (summonTimer == 0) {
							SpawnMinion(ModContent.NPCType<ShieldGuard>(), true);
							SpawnMinion(ModContent.NPCType<MortarGunner>(), true);
						}
						if (summonTimer == 60)
							SpawnMinion(ModContent.NPCType<MortarGunner>(), false);
						if (summonTimer == 120)
							SpawnMinion(ModContent.NPCType<LightShield>(), true);

						break;
				}

				// --- 阶段3：监测存活与退出条件 ---
				// 注意：必须在最后一波怪刷出之后，才开始监测是否全部死亡
				// 假设每个阶段最晚的一波是在第 3 秒（180帧）
				bool allWavesDispatched = summonTimer > 180;

				// 存活判定直接扫描全场，而不是只信 MinionWhoAmIs：
				// 该列表只在生成侧填充，多人局客户端为空，会让客户端提前退出召唤状态。
				bool minionsAlive = false;
				for (int i = 0; i < Main.maxNPCs; i++) {
					NPC summoned = Main.npc[i];
					if (CrownslayerSummonGlobalNPC.IsSummon(summoned) && summoned.life > 0) {
						minionsAlive = true;
						break;
					}
				}
				//foreach (int index in MinionWhoAmIs) {
				// 从后向前遍历以安全移除无效索引，同时在移除时发送提示
				for (int i = MinionWhoAmIs.Count - 1; i >= 0; i--) {
					int index = MinionWhoAmIs[i];
					if (index < 0 || index >= Main.maxNPCs) {
						MinionWhoAmIs.RemoveAt(i);
						continue;
					}
					NPC minion = Main.npc[index];

					// 关键：必须同时满足以下三个条件，才判定为“召唤的小怪还活着”：
					// 1. active: 该索引位置有 NPC
					// 2. NPC 没死（life > 0）
					// 3. 这里的特殊判定：检查该 NPC 的来源是否是本 Boss 召唤的
					//    或者检查其 type 是否在你的召唤名单内
					if (CrownslayerSummonGlobalNPC.IsSummon(minion) && minion.life > 0) {
						// 这样即使史莱姆路过，只要它的索引不在 MinionWhoAmIs 里，就不会被统计
						minionsAlive = true;
					} else {
						// 小怪已死亡或被移除，更新列表并提示剩余数量
						MinionWhoAmIs.RemoveAt(i);
						int remaining = 0;
						foreach (int id in MinionWhoAmIs) {
							if (id >= 0 && id < Main.maxNPCs) {
								NPC m = Main.npc[id];
								if (CrownslayerSummonGlobalNPC.IsSummon(m) && m.life > 0) remaining++;
							}
						}
						string deathMsg = $"支援者被击败，当前剩余支援者：{remaining}";
						if (Main.netMode != NetmodeID.Server) Main.NewText(deathMsg, Color.OrangeRed);
						CombatText.NewText(new Rectangle((int)NPC.position.X, (int)NPC.position.Y - 20, NPC.width, NPC.height), Color.OrangeRed, $"剩余 {remaining}");
					}
				}

				// 只有当所有批次都刷完了，且场上没怪了，才结束
				if (allWavesDispatched && !minionsAlive) {
					NPC.dontTakeDamage = false;
					NPC.alpha = 0;
					NPC.damage = NPC.defDamage; // 恢复原始伤害
					CurrentAIState = AIState.Recover;
					StateTimer = 18;
					NPC.netUpdate = true;
				}
			}
		}
		private void ShootDaggers(Player target) {
			Vector2 baseVel = Vector2.Normalize(target.Center - NPC.Center) * 16f;
			SoundEngine.PlaySound(SoundID.Item71, NPC.Center);
			for (int i = -1; i <= 1; i++) {
				Vector2 shotVel = baseVel.RotatedBy(MathHelper.ToRadians(i * 20f));
				Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, shotVel,
					ModContent.ProjectileType<GravityDagger>(), (int)(damage * 0.4f), 3f, Main.myPlayer);
			}
		}

		private void ShootSingleDagger(Player target) {
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			Vector2 toTarget = target.Center - NPC.Center;
			if (toTarget.LengthSquared() < 1f)
				toTarget = new Vector2(-NPC.spriteDirection, 0f);

			Vector2 shotVel = Vector2.Normalize(toTarget) * 16f;
			Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, shotVel,
				ModContent.ProjectileType<GravityDagger>(), (int)(damage * 0.4f), 3f, Main.myPlayer);
			SoundEngine.PlaySound(SoundID.Item71, NPC.Center);
		}
		private void ShootMoreDaggers(Player target) {

			Vector2 baseVel = Vector2.Normalize(target.Center - NPC.Center) * 20f;
			SoundEngine.PlaySound(SoundID.Item71, NPC.Center);
			for (int i = -2; i <= 2; i++) {
				Vector2 shotVel = baseVel.RotatedBy(MathHelper.ToRadians(i * 15f));
				Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, shotVel,
				ModContent.ProjectileType<GravityDagger>(), (int)(damage * 0.4f), 3f, Main.myPlayer);
			}

		}
		// 仅统计预警线；已发射的 BarrageDagger 不再挡住下一排。
		private static bool HasActivePhase2PredictLines() {
			for (int i = 0; i < Main.maxProjectiles; i++) {
				if (Main.projectile[i].active && Main.projectile[i].type == DaggerPredictLineType)
					return true;
			}

			return false;
		}

		private bool TrySpawnPhase2PredictBarrage(Player target, float? forcedAngleRad = null, int lineCount = 5) {
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return false;

			if (HasActivePhase2PredictLines())
				return false;

			Player focus = GetNearestThreatPlayer(target);
			int count = Math.Clamp(lineCount, 4, 6);
			float angleDeg = forcedAngleRad.HasValue
				? MathHelper.ToDegrees(forcedAngleRad.Value)
				: Main.rand.NextFloat(20f, 160f);
			float speed = 48f + Main.rand.NextFloat(-6f, 8f);
			float spawnDist = 760f + Main.rand.NextFloat(-80f, 120f);

			float radian = MathHelper.ToRadians(angleDeg);
			Vector2 shootDir = radian.ToRotationVector2();
			Vector2 perpDir = new Vector2(-shootDir.Y, shootDir.X);
			Vector2 centerPoint = focus.Center - shootDir * spawnDist;
			float spacing = Math.Max(48f, 240f - count * 8f);

			for (int i = 0; i < count; i++) {
				float offset = (i - (count - 1) / 2f) * spacing;
				Vector2 spawnPos = centerPoint + perpDir * offset;
				Projectile.NewProjectile(
					NPC.GetSource_FromAI(),
					spawnPos,
					Vector2.Zero,
					ModContent.ProjectileType<DaggerPredictLine>(),
					0, 0, Main.myPlayer,
					speed,
					radian);
			}

			return true;
		}

		// V 字路径：玩家为顶点，两对角为起止；每轮随机从左或右上方切入。
		private void SetupSkill7VPath(Player focusTarget, out float barrageAngle) {
			float startSide = Main.rand.NextBool() ? -1f : 1f;
			skill7VApex = focusTarget.Center + new Vector2(0f, -12f);
			Vector2 firstCorner = skill7VApex + new Vector2(startSide * Skill7CornerOffsetX, -Skill7CornerOffsetY);
			skill7SecondCorner = skill7VApex + new Vector2(-startSide * Skill7CornerOffsetX, -Skill7CornerOffsetY);
			NPC.Center = firstCorner;
			barrageAngle = (skill7VApex - firstCorner).ToRotation();
			NPC.ai[3] = barrageAngle;
		}

		private void Skill7AdvanceStep(int nextStep) {
			skill7CycleStep = nextStep;
			skill7StepTimer = 0;
		}

		private static float Skill7SmoothStep(float t) {
			t = MathHelper.Clamp(t, 0f, 1f);
			return t * t * (3f - 2f * t);
		}

		private void Skill7ApplyVLerp(int stepTimer, float span, Vector2 start, Vector2 goal) {
			float t = Skill7SmoothStep(stepTimer / span);
			NPC.Center = Vector2.Lerp(start, goal, t);
			Vector2 legDir = goal - start;
			if (legDir.LengthSquared() > 1f) {
				NPC.velocity = legDir.SafeNormalize(Vector2.UnitX) * (14f + t * 10f);
				NPC.spriteDirection = NPC.velocity.X >= 0f ? -1 : 1;
			}
		}

		private Player GetNearestThreatPlayer(Player fallbackTarget) {
			Player bestTarget = fallbackTarget;
			float bestDistanceSq = float.MaxValue;

			for (int i = 0; i < Main.maxPlayers; i++) {
				Player player = Main.player[i];
				if (!player.active || player.dead || player.ghost)
					continue;

				float distanceSq = Vector2.DistanceSquared(NPC.Center, player.Center);
				if (distanceSq < bestDistanceSq) {
					bestDistanceSq = distanceSq;
					bestTarget = player;
				}
			}

			return bestTarget;
		}

		// ==================== 脱战离场 ====================
		// 规格：周围不再有玩家 → 速度立即归零 → 原地释放烟雾 → 120 帧内 alpha 0→255（完全透明）
		//       → 清空全部弹幕与援军 → 脱离战斗。
		private const int LeaveTransparentEnd = 120; // 120 帧后完全透明
		private bool leaveCleared;

		/// <summary>
		/// 是否存在“活跃、未死、非幽灵”的玩家；存在则通过 <paramref name="nearest"/> 返回最近的一个。
		/// 与 GetNearestThreatPlayer 的区别：找不到时返回 false，而不是把死亡玩家当作兜底目标。
		/// </summary>
		private bool TryGetNearestCombatPlayer(out Player nearest) {
			nearest = null;
			float bestDistanceSq = float.MaxValue;

			for (int i = 0; i < Main.maxPlayers; i++) {
				Player player = Main.player[i];
				if (!player.active || player.dead || player.ghost)
					continue;

				float distanceSq = Vector2.DistanceSquared(NPC.Center, player.Center);
				if (distanceSq < bestDistanceSq) {
					bestDistanceSq = distanceSq;
					nearest = player;
				}
			}

			return nearest != null;
		}

		private void EnterLeaving() {
			if (CurrentAIState == AIState.Leaving)
				return;

			CurrentAIState = AIState.Leaving;
			StateTimer = 0;
			leaveCleared = false;
			NPC.netUpdate = true;
		}

		private void ExecuteLeaving() {
			StateTimer++;

			// 1) 速度立即归零，并关掉重力与地形碰撞，保证原地不动
			NPC.velocity = Vector2.Zero;
			SetPhysics(false, false);

			// 2) 脱战期间不参与战斗：不撞伤玩家，也不吃伤害
			NPC.damage = 0;
			NPC.dontTakeDamage = true;
			Move_RestoreCombatHitbox();

			// 3) 动画与透明度：40 帧内 0 → 255
			CurrentAnimation = StateTimer < 20 ? NPCState.JumpOut : NPCState.Blank;
			NPC.alpha = (int)MathHelper.Lerp(0f, 255f, MathHelper.Clamp(StateTimer / (float)LeaveTransparentEnd, 0f, 1f));

			// 4) 原地释放烟雾：起手三大团（参考原版气阱的成团气体），随后持续补充、越铺越开
			if (StateTimer == 1) {
				SpawnLeaveGasClouds();
				EmitLeaveSmoke(34, 3.2f, 26f);
			}
			else if (StateTimer == 13) {
				SpawnLeaveGasCloud(0.7f, 1.2f);
				EmitLeaveSmoke(16, 2.4f, 34f);
			}
			else if (StateTimer == 26) {
				SpawnLeaveGasCloud(0.5f, -1.4f);
				EmitLeaveSmoke(12, 2.0f, 44f);
			}
			else if (StateTimer < LeaveTransparentEnd && StateTimer % 3 == 0) {
				EmitLeaveSmoke(8, 1.8f, 24f + StateTimer * 0.9f);
			}

			// 5) 黑红烟雾在暗处也要看得见：补一点暗红光照
			Lighting.AddLight(NPC.Center, 0.55f, 0.11f, 0.11f);

			// 6) 完全透明后：清空所有弹幕与援军，并脱离战斗
			if (StateTimer >= LeaveTransparentEnd) {
				NPC.alpha = 255;
				NPC.velocity = Vector2.Zero;

				if (!leaveCleared) {
					leaveCleared = true;

					if (Main.netMode != NetmodeID.MultiplayerClient) {
						ClearCrownslayerProjectiles();
						DespawnSummonedMinions();
						LeaveCombat();
					}
				}
			}
		}

		// 原地烟雾：黑（暗灰）主体 + 黑红气团 + 赤红余烬，参考原版气阱"成团扩张"的观感
		private void EmitLeaveSmoke(int count, float maxKick, float spread) {
			if (Main.dedServ)
				return;

			for (int i = 0; i < count; i++) {
				// 以环形分布为主，让烟向外"鼓"成一团，而不是原地抖动
				float angle = MathHelper.TwoPi * (i / (float)count) + Main.rand.NextFloat(-0.3f, 0.3f);
				Vector2 dir = angle.ToRotationVector2();
				Vector2 spawnPos = NPC.Center + dir * (Main.rand.NextFloat(0.3f, 1f) * spread) + Main.rand.NextVector2Circular(8f, 10f);
				Vector2 outVel = dir * (Main.rand.NextFloat(0.5f, 1.9f) * maxKick / 3f) + new Vector2(0f, -Main.rand.NextFloat(0.2f, 1.0f));

				// A. 主体黑烟：大颗、暗色、不吃环境光，保证"黑"
				Dust dark = Dust.NewDustPerfect(spawnPos, DustID.Smoke, outVel, 0,
					Color.Lerp(new Color(12, 9, 9), new Color(46, 22, 22), Main.rand.NextFloat()),
					Main.rand.NextFloat(1.6f, 3.0f));
				dark.noGravity = true;
				dark.noLight = true;
				dark.fadeIn = 0.35f;

				// B. 黑红气团：中颗，负责整体色调"黑偏红"
				Dust gas = Dust.NewDustPerfect(spawnPos + Main.rand.NextVector2Circular(10f, 12f), DustID.Cloud,
					outVel * 0.8f, 60,
					Color.Lerp(new Color(52, 14, 16), new Color(122, 30, 28), Main.rand.NextFloat()),
					Main.rand.NextFloat(1.2f, 2.2f));
				gas.noGravity = true;

				// C. 赤红火星 / 暗焰（每 3 颗掺 1 颗）：提亮边缘，暗处也显眼
				if (i % 3 == 0) {
					Dust spark = Dust.NewDustPerfect(spawnPos,
						Main.rand.NextBool(3) ? DustID.Shadowflame : DustID.GemRuby, outVel * 1.2f, 0,
						Color.Lerp(new Color(210, 60, 40), new Color(255, 140, 90), Main.rand.NextFloat()),
						Main.rand.NextFloat(0.7f, 1.25f));
					spark.noGravity = true;
					spark.fadeIn = 0.2f;
				}
			}
		}

		// 清掉她自己的全部弹幕（这些类型只属于她，按类型清即可）
		private void ClearCrownslayerProjectiles() {
			for (int i = 0; i < Main.maxProjectiles; i++) {
				Projectile projectile = Main.projectile[i];
				if (!projectile.active)
					continue;

				if (projectile.type == ModContent.ProjectileType<SwordSlashEffect>()
					|| projectile.type == ModContent.ProjectileType<TransparentSlash>()
					|| projectile.type == ModContent.ProjectileType<GravityDagger>()
					|| projectile.type == ModContent.ProjectileType<StallDagger>()
					|| projectile.type == ModContent.ProjectileType<DelayDagger>()
					|| projectile.type == ModContent.ProjectileType<RedMagicBlade>()
					|| projectile.type == ModContent.ProjectileType<DaggerPredictLine>()
					|| projectile.type == ModContent.ProjectileType<BarrageDagger>()
					|| projectile.type == ModContent.ProjectileType<GroundDagger>())
					projectile.Kill();
			}
		}

		// 清掉她召唤的援军（统一带 CrownslayerSummonGlobalNPC 标记；用失活而非 Kill，避免产生掉落）
		private void DespawnSummonedMinions() {
			for (int i = 0; i < Main.maxNPCs; i++) {
				NPC minion = Main.npc[i];
				if (CrownslayerSummonGlobalNPC.IsSummon(minion))
					minion.active = false;
			}

			MinionWhoAmIs.Clear();
		}

		// 脱离战斗：直接失活并广播。
		// boss 不能靠 EncourageDespawn 走人：CheckActive 对 boss 只在 timeLeft 归零时失活
		// （同 WBoss.WDespawnState 的说明），因此这里手动失活并同步给客户端。
		private void LeaveCombat() {
			NPC.active = false;

			if (Main.netMode == NetmodeID.Server) {
				NPC.life = 0;
				NPC.netSkip = -1;
				NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, NPC.whoAmI);
			}
		}

		// 她死亡时：收走援军，并播放与脱战相同的黑红雾气演出（三大团气团 + 同参数烟雾）。
		public override void OnKill() {
			if (Main.netMode != NetmodeID.MultiplayerClient) {
				SpawnLeaveGasClouds();
				DespawnSummonedMinions();
			}

			// 与 ExecuteLeaving 起手同一套参数（客户端各自播放）
			EmitLeaveSmoke(34, 3.2f, 26f);
			EmitLeaveSmoke(16, 2.4f, 34f);
			EmitLeaveSmoke(12, 2.0f, 44f);
		}

		// 生成一组脱战气团（视觉弹幕，不造成任何伤害；比本体活得久，负责"烟散"）
		private void SpawnLeaveGasClouds() {
			SpawnLeaveGasCloud(1.15f, 0.35f);
			SpawnLeaveGasCloud(0.85f, -1.1f);
			SpawnLeaveGasCloud(0.6f, 2.0f);
		}

		private void SpawnLeaveGasCloud(float cloudScale, float textureSeed) {
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			int index = Projectile.NewProjectile(NPC.GetSource_FromAI(),
				NPC.Center + Main.rand.NextVector2Circular(10f, 14f),
				Main.rand.NextVector2Circular(0.35f, 0.5f) + new Vector2(0f, -0.25f),
				ModContent.ProjectileType<CrownslayerDespawnGas>(), 0, 0f, Main.myPlayer,
				cloudScale, textureSeed);

			if (index >= 0 && index < Main.maxProjectiles)
				Main.projectile[index].netUpdate = true;
		}
		// 辅助方法：封装召唤逻辑，减少重复代码
		private void SpawnMinion(int type, bool onLeft) {
			// 多人局只能由服务端生成，否则每个客户端都会各自刷出幽灵援军。
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;
			// 生成在目标玩家左右两侧各 16 格处：比屏幕内侧边缘更贴近玩家，且不再依赖屏幕坐标
			const float sideOffset = 256f;
			Player spawnTarget = Main.player[NPC.target];
			float spawnX = spawnTarget.Center.X + (onLeft ? -sideOffset : sideOffset);
			float spawnY = spawnTarget.Center.Y - 32f;

			Vector2 spawnPos = FindSafeSpot(new Vector2(spawnX, spawnY));
			int index = NPC.NewNPC(NPC.GetSource_FromAI(), (int)spawnPos.X, (int)spawnPos.Y, type);
			if (index >= 0 && index < Main.maxNPCs) {
				Main.npc[index].GetGlobalNPC<CrownslayerSummonGlobalNPC>().IsCrownslayerSummon = true;
				MinionWhoAmIs.Add(index);
				Main.npc[index].netUpdate = true;

				// 召唤特效
				for (int i = 0; i < 15; i++) {
					Dust.NewDust(Main.npc[index].position, 32, 32, DustID.Shadowflame);
				}

				// 聊天提示与屏幕浮动提示（显示当前存活召唤体数量）
				int alive = 0;
				foreach (int id in MinionWhoAmIs) {
					if (id >= 0 && id < Main.maxNPCs) {
						NPC m = Main.npc[id];
						if (CrownslayerSummonGlobalNPC.IsSummon(m) && m.life > 0)
							alive++;
					}
				}
				string spawnMsg = $"弑君者请求了支援：{Lang.GetNPCNameValue(type)}，该支援单位数量：{alive}";
				if (Main.netMode != NetmodeID.Server) Main.NewText(spawnMsg, Color.OrangeRed);
				CombatText.NewText(new Rectangle((int)spawnPos.X, (int)spawnPos.Y - 20, 2, 2), Color.OrangeRed, $"剩余 {alive}");
			}
		}
		private void ExecuteSlamImpact() {
			SoundEngine.PlaySound(SoundID.Item14, NPC.Center);

			// 向左右发射冲击波弹幕（类似史莱姆皇后）
			for (int i = -1; i <= 1; i += 2) { // -1为左，1为右
				for (int j = 1; j <= 6; j++) {
					Vector2 waveVel = new Vector2(i * j * 7f, 0f); // 速度逐渐变快的阶梯状弹幕
					Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Bottom, waveVel,
						ModContent.ProjectileType<TransparentSlash>(), damage / 2, 6f, Main.myPlayer);
				}
			}

			// 落地尘埃效果
			for (int i = 0; i < 40; i++) {
				Dust d = Dust.NewDustDirect(NPC.Bottom, 0, 0, DustID.Smoke, Main.rand.NextFloat(-10, 10), Main.rand.NextFloat(-5, 0));
				d.scale = 1.5f;
			}
		}
		private void DrawPredictiveLine(Vector2 start, Vector2 end) {
			for (float i = 0; i < 1; i += 0.05f) {
				Vector2 pos = Vector2.Lerp(start, end, i);
				Dust d = Dust.NewDustPerfect(pos, DustID.LifeDrain, Vector2.Zero);
				d.noGravity = true;
				d.scale = 0.8f;
			}
		}
	}
	// 弑君者脱战气团：参考原版气阱（ProjectileID.GasTrap / aiStyle 92）的
	// scale 1→6 扩张、包围盒同步放大，以及"前 25% 淡入、后 25% 淡出、峰值 0.85 不透明度"曲线；
	// 内部持续冒黑烟（同样参考气阱在气体里补 Shadowflame 尘埃的做法）。
	// 配色改为黑偏红；纯视觉弹幕：不伤害玩家、也不可被击中、不受地形阻挡。
	public class CrownslayerDespawnGas : ModProjectile
	{
		public override string Texture => FogPaths[0];

		private const int GasLife = 90; // 比本体离场（40 帧）更久，负责"烟散"

		private static readonly string[] FogPaths = {
			"ArknightsMod/Content/NPCs/Enemy/ThroughChapter4/CrownslayerFog_1",
			"ArknightsMod/Content/NPCs/Enemy/ThroughChapter4/CrownslayerFog_2",
			"ArknightsMod/Content/NPCs/Enemy/ThroughChapter4/CrownslayerFog_3",
			"ArknightsMod/Content/NPCs/Enemy/ThroughChapter4/CrownslayerFog_4",
		};

		private float Progress => 1f - Projectile.timeLeft / (float)GasLife;
		private float BaseScale => Math.Max(0.2f, Projectile.ai[0]);

		public override void SetDefaults() {
			Projectile.width = 50;   // 与气阱同尺寸，随 scale 一起放大
			Projectile.height = 50;
			Projectile.friendly = false;   // 不伤害任何目标
			Projectile.hostile = false;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.penetrate = -1;
			Projectile.aiStyle = -1;
			Projectile.timeLeft = GasLife;
			// 不要设 hide = true：Main.DrawProjectiles() 会跳过 hide 弹幕（Main.cs:22233/22252），
			// 而本弹幕完全由自己的 PreDraw 绘制，一旦 hide 就永远不会显示。
		}

		public override bool? CanDamage() => false;

		public override void AI() {
			float p = Progress;

			// 复刻气阱曲线：0~95% 进度内从 1 倍扩张到 6 倍
			float grow = Utils.Remap(p, 0f, 0.95f, 1f, 6f) * BaseScale;
			Projectile.scale = grow;

			// 尺寸放大时保持中心不变（气阱同款写法）
			Vector2 center = Projectile.Center;
			Projectile.width = (int)(50f * grow);
			Projectile.height = (int)(50f * grow);
			Projectile.Center = center;

			// 缓慢上浮 + 自转，让气团"活"起来
			Projectile.velocity *= 0.96f;
			Projectile.velocity.Y -= 0.012f;
			Projectile.rotation += (Projectile.ai[1] >= 0f ? 0.004f : -0.004f) * (0.6f + p);

			// 气团内部持续冒黑烟，量与体积挂钩
			if (!Main.dedServ && p < 0.9f && Main.rand.NextBool(2)) {
				Vector2 spawn = Projectile.Center + Main.rand.NextVector2Circular(Projectile.width * 0.35f, Projectile.height * 0.35f);
				Dust d = Dust.NewDustPerfect(spawn, DustID.Smoke,
					Main.rand.NextVector2Circular(0.6f, 0.6f) + new Vector2(0f, -0.5f), 0,
					Color.Lerp(new Color(14, 10, 10), new Color(44, 20, 20), Main.rand.NextFloat()),
					Main.rand.NextFloat(1.4f, 2.4f));
				d.noGravity = true;
				d.noLight = true;
			}
		}

		public override bool PreDraw(ref Color lightColor) {
			// 透明度：前 25% 淡入、后 25% 淡出，峰值 0.85（与气阱完全一致）
			float fadeIn = Utils.Remap(Progress, 0f, 0.25f, 0f, 1f);
			float fadeOut = Utils.Remap(Progress, 0.75f, 1f, 1f, 0f);
			float opacity = MathHelper.Clamp(fadeIn * fadeOut, 0f, 1f) * 0.85f;
			if (opacity <= 0.002f)
				return false;

			Texture2D tex = ModContent.Request<Texture2D>(FogPaths[(int)Math.Abs(Projectile.ai[1]) % FogPaths.Length]).Value;
			Vector2 origin = tex.Size() * 0.5f;
			Vector2 pos = Projectile.Center - Main.screenPosition;

			// 第 1 层：黑烟主体。必须用 AlphaBlend——加色混合下黑色等于不存在
			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
				DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
			Main.spriteBatch.Draw(tex, pos, null, new Color(16, 11, 12) * (opacity * 0.95f),
				Projectile.rotation, origin, Projectile.scale * 1.05f, SpriteEffects.None, 0f);
			Main.spriteBatch.Draw(tex, pos + new Vector2(Projectile.width * 0.06f, -Projectile.height * 0.05f), null,
				new Color(40, 14, 14) * (opacity * 0.75f), -Projectile.rotation * 0.8f, origin,
				Projectile.scale * 0.82f, SpriteEffects.None, 0f);

			// 第 2 层：赤红余晖（加色），负责"黑偏红"的亮边与可见度
			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState,
				DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
			Main.spriteBatch.Draw(tex, pos, null, new Color(150, 34, 30) * (opacity * 0.55f),
				Projectile.rotation * 1.3f, origin, Projectile.scale * 0.55f, SpriteEffects.None, 0f);

			// 还原批处理状态
			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
				DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

			return false;
		}

		public override void OnKill(int timeLeft) {
			if (Main.dedServ)
				return;

			// 消散：最后一小撮烟 + 火星
			for (int i = 0; i < 10; i++) {
				Dust d = Dust.NewDustPerfect(
					Projectile.Center + Main.rand.NextVector2Circular(Projectile.width * 0.3f, Projectile.height * 0.3f),
					DustID.Smoke, Main.rand.NextVector2Circular(1.2f, 1.2f) + new Vector2(0f, -0.8f), 0,
					Color.Lerp(new Color(16, 11, 11), new Color(60, 24, 24), Main.rand.NextFloat()),
					Main.rand.NextFloat(1.4f, 2.6f));
				d.noGravity = true;
				d.noLight = true;
			}
		}
	}

	public class GroundDagger : ModProjectile
	{
		public override string Texture => "ArknightsMod/Content/NPCs/Enemy/ThroughChapter4/GravityDagger_Barrage";
		public override void SetDefaults() {
			Projectile.width = 30;
			Projectile.height = 30;
			Projectile.aiStyle = -1;      // 自定义AI
			Projectile.hostile = false;    // 不伤害玩家
			Projectile.friendly = false;   // 不伤害怪物
			Projectile.tileCollide = false; // 不与地形碰撞（防止在半空碎掉）
			Projectile.ignoreWater = true;
			Projectile.timeLeft = 900;     // 15秒后消失 (15 * 60)
			Projectile.scale = 1f;         // 比例为1
		}

		public override void AI() {
			Projectile.rotation = MathHelper.PiOver2;

			// 可以在刀柄处产生一点微弱的红光粒子，增加氛围感
			if (Main.rand.NextBool(5)) {
				Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.LifeDrain);
				d.velocity *= 0.2f;
				d.noGravity = true;
			}
		}

		// 确保它没有伤害判定
		public override bool? CanDamage() => false;
	}
	public class StallDagger : ModProjectile
	{
		public override string Texture => "ArknightsMod/Content/NPCs/Enemy/ThroughChapter4/GravityDagger_Barrage";
		public override void SetDefaults() {
			Projectile.width = 20;
			Projectile.height = 20;
			Projectile.hostile = true;
			
			Projectile.timeLeft = 600;
		}

		public override void AI() {
			NPC owner = Main.npc[(int)Projectile.ai[0]];
			if (!owner.active) { Projectile.Kill(); return; }

			// 状态机 localAI[0]: 0-发射中, 1-驻留旋转, 2-准备追踪发射, 3-垂直下落

			// --- 特殊逻辑：监听 Boss 是否下砸落地 ---
			// 假设 Boss 下砸落地的瞬间会将自己的 ai[2] 设为 1 (作为全局信号)
			if (owner.ai[2] > 0 && Projectile.localAI[0] < 2) {
				Projectile.localAI[0] = 3;
				Projectile.velocity = new Vector2(0, 25f); // 垂直快速下落
			}

			if (Projectile.localAI[0] == 0) {
				Projectile.tileCollide = false;
				Projectile.rotation = Projectile.velocity.ToRotation();
				if (Projectile.Center.Y <= Projectile.ai[1] || Projectile.velocity.Length() < 2f) {
					Projectile.localAI[0] = 1;
					Projectile.velocity = new Vector2(0, 0.4f);
				}
			}
			else if (Projectile.localAI[0] == 1) {
				Projectile.tileCollide = false;
				// 旋转速度翻倍：从 0.25 提高到 0.5
				Projectile.rotation += 0.8f;

				// 碰撞检测
				if (owner.velocity.Length() > 15f && Projectile.Hitbox.Intersects(owner.Hitbox)) {
					// 清脆的金属碰撞声：使用石英或特定的金属音效
					SoundEngine.PlaySound(SoundID.NPCHit4, Projectile.Center);
					// 也可以尝试 SoundID.Item37 (金属叮当声)

					Projectile.localAI[0] = 2;
				}
			}
			else if (Projectile.localAI[0] == 2) {
				Player target = Main.player[owner.target];
				Projectile.velocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * 26f;
				Projectile.rotation = Projectile.velocity.ToRotation();
				Projectile.localAI[0] = 4; // 进入最终锁定飞行
				Projectile.tileCollide = true; // 开始与地形交互
			}
			else if (Projectile.localAI[0] == 3) {
				Projectile.rotation = MathHelper.PiOver2;
				Projectile.tileCollide = true; // 开始与地形交互
			}
			if (Main.rand.NextBool(3)) {
				Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GemRuby, -Projectile.velocity * 0.2f, 100, Color.Red, 0.8f);
				d.noGravity = true;
			}
		}
		public override void OnKill(int timeLeft) {
			// 播放击中地面的清脆音效（Item10 是子弹打墙声，你可以换成 Item70 这种金属声）
			SoundEngine.PlaySound(SoundID.Item10, Projectile.Center);

			// 产生赤红色碎屑粒子
			for (int i = 0; i < 15; i++) {
				Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GemRuby, Main.rand.NextVector2Circular(3f, 3f), 0, Color.Red, 1.2f);
				d.noGravity = false; // 落地粒子受重力掉落
				d.velocity.Y -= 2f;  // 向上弹跳一点
			}

			// 额外产生一点烟雾
			for (int i = 0; i < 5; i++) {
				Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, 0f, 0f, 100, Color.Gray, 1.5f);
			}
		}

	}
	public class DelayDagger : ModProjectile
	{
		public override string Texture => "ArknightsMod/Content/NPCs/Enemy/ThroughChapter4/GravityDagger_Barrage";
		public override void SetDefaults() {
			Projectile.width = 20;
			Projectile.height = 20;
			Projectile.hostile = true;

			Projectile.timeLeft = 600;
		}
		public override void AI() {
			// ai[0] 是玩家索引，ai[1] 是计时器
			int targetIdx = (int)Projectile.ai[0];
			Player target = Main.player[targetIdx];
			Projectile.ai[1]++;

			if (Projectile.ai[1] < 60) {
				// --- 仅在第一帧锁定方向，不再追踪 ---
				if (Projectile.ai[1] == 1) {
					Projectile.localAI[1] = (target.Center - Projectile.Center).ToRotation();
				}
				Projectile.rotation += 0.6f; // 1秒内保持快速旋转
			}
			else if (Projectile.ai[1] == 60) {
				// 1秒到，停止旋转，沿锁定角度射出
				Projectile.velocity = Projectile.localAI[1].ToRotationVector2() * 22f;
				Projectile.rotation = Projectile.velocity.ToRotation();
				SoundEngine.PlaySound(SoundID.Item71, Projectile.Center);
			}
			else {
				// 飞行中不再旋转
				Projectile.rotation = Projectile.velocity.ToRotation();
			}
		}

		public override bool PreDraw(ref Color lightColor) {
			// 只在准备阶段绘制判定线
			if (Projectile.ai[1] < 60) {
				float progress = Projectile.ai[1] / 60f;
				float pulse = 0.4f + 0.6f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 25f);
				float alpha = (progress * 0.4f + pulse * 0.4f) * 0.6f;

				Texture2D lineTex = TextureAssets.MagicPixel.Value;
				Vector2 beamStart = Projectile.Center;
				float beamRotation = Projectile.localAI[1]; // 使用第一帧锁定的角度

				// 1. 10 像素宽的红射线
				Main.EntitySpriteDraw(lineTex, beamStart - Main.screenPosition, new Rectangle(0, 0, 1, 1),
					Color.Red * alpha, beamRotation, new Vector2(0f, 0.5f), new Vector2(4000f, 10f), SpriteEffects.None, 0);

				// 2. 2 像素宽的白芯
				Main.EntitySpriteDraw(lineTex, beamStart - Main.screenPosition, new Rectangle(0, 0, 1, 1),
					Color.White * alpha * 0.5f, beamRotation, new Vector2(0f, 0.5f), new Vector2(4000f, 2f), SpriteEffects.None, 0);
			}
			return true; // 正常绘制小刀贴图
		}
		public override void OnKill(int timeLeft) {
			// 播放击中地面的清脆音效（Item10 是子弹打墙声，你可以换成 Item70 这种金属声）
			SoundEngine.PlaySound(SoundID.Item10, Projectile.Center);

			// 产生赤红色碎屑粒子
			for (int i = 0; i < 15; i++) {
				Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GemRuby, Main.rand.NextVector2Circular(3f, 3f), 0, Color.Red, 1.2f);
				d.noGravity = false; // 落地粒子受重力掉落
				d.velocity.Y -= 2f;  // 向上弹跳一点
			}

			// 额外产生一点烟雾
			for (int i = 0; i < 5; i++) {
				Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, 0f, 0f, 100, Color.Gray, 1.5f);
			}
		}
	}

	public class TransparentSlash : ModProjectile
	{
		public override void SetDefaults() {
			Projectile.width = 96;
			Projectile.height = 64;
			Projectile.friendly = false;
			Projectile.hostile = true;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.alpha = 255;
			Projectile.timeLeft = 2;
		}

		public override void AI() {
			Projectile.velocity = Vector2.Zero;
		}

		public override void OnHitPlayer(Player target, Player.HurtInfo info) {
			if (Main.dedServ)
				return;

			for (int i = 0; i < 8; i++) {
				Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GemRuby,
					Main.rand.NextVector2Circular(4f, 4f), 0, Color.Red, 1f);
				d.noGravity = true;
			}
		}
	}
	public class GravityDagger : ModProjectile
	{
		public override string Texture => "ArknightsMod/Content/NPCs/Enemy/ThroughChapter4/GravityDagger_Barrage";
		public override void SetStaticDefaults() {
			ProjectileID.Sets.TrailingMode[Type] = 2;
			ProjectileID.Sets.TrailCacheLength[Type] = 18;
		}

		public override void SetDefaults() {
			Projectile.width = 22;  // 长度作为宽度
			Projectile.height = 8;  // 宽度作为高度
			Projectile.hostile = true;
			Projectile.tileCollide = true;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 300;
			Projectile.aiStyle = -1;
		}

		public override void AI() {
			// 修正贴图指向：仅在有速度时更新角度，防止原地悬浮时乱转
			if (Projectile.velocity != Vector2.Zero) {
				Projectile.rotation = Projectile.velocity.ToRotation();
			}

			// 轻微重力
			Projectile.velocity.Y += 0.15f;

			// 速度上限限制
			if (Projectile.velocity.Y > 20f)
				Projectile.velocity.Y = 20f;

			// 飞行时的微弱红光粒子
			if (Main.rand.NextBool(3)) {
				Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GemRuby, -Projectile.velocity * 0.2f, 100, Color.Red, 0.8f);
				d.noGravity = true;
			}

			Projectile.localAI[0] += 0.35f + Projectile.velocity.Length() * 0.015f;
		}

		public override bool PreDraw(ref Color lightColor) {
			return CrownslayerTrailEffects.DrawGravityDaggerTrail(Projectile);
		}

		// --- 落地产生粒子效果和音效 ---
		public override void OnKill(int timeLeft) {
			// 播放击中地面的清脆音效（Item10 是子弹打墙声，你可以换成 Item70 这种金属声）
			SoundEngine.PlaySound(SoundID.Item10, Projectile.Center);

			// 产生赤红色碎屑粒子
			for (int i = 0; i < 15; i++) {
				Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GemRuby, Main.rand.NextVector2Circular(3f, 3f), 0, Color.Red, 1.2f);
				d.noGravity = false; // 落地粒子受重力掉落
				d.velocity.Y -= 2f;  // 向上弹跳一点
			}

			// 额外产生一点烟雾
			for (int i = 0; i < 5; i++) {
				Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, 0f, 0f, 100, Color.Gray, 1.5f);
			}
		}


	}
	public class RedMagicBlade : ModProjectile
	{
		public override string Texture => "ArknightsMod/Content/NPCs/Enemy/ThroughChapter4/GravityDagger_Barrage";

		public override void SetStaticDefaults() {
			ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
			ProjectileID.Sets.TrailCacheLength[Projectile.type] = 20;
		}

		public override void SetDefaults() {
			Projectile.width = 45;
			Projectile.height = 45;
			Projectile.hostile = true;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 480;
			Projectile.alpha = 255;
		}

		private bool IsLaunching => Projectile.localAI[1] > 0.5f;

		public override void AI() {
			Player target = Main.player[(int)Projectile.ai[0]];
			if (!target.active || target.dead) {
				Projectile.Kill();
				return;
			}

			Projectile.alpha = Math.Max(0, Projectile.alpha - 8);

			if (!IsLaunching) {
				Projectile.velocity = Vector2.Zero;

				if (Projectile.timeLeft > 60) {
					if (Projectile.localAI[0] == 0f) {
						Projectile.rotation = (target.Center - Projectile.Center).ToRotation();
						Projectile.localAI[0] = 1f;
					}

				}
				else {
					float chargeT = 1f - Projectile.timeLeft / 60f;
					float targetRot = (target.Center - Projectile.Center).ToRotation();

					// 蓄力最后 1s：预警线跟随玩家，发射前锁定朝向
					if (Projectile.timeLeft == 1)
						Projectile.rotation = targetRot;
					else
						Projectile.rotation = Projectile.rotation.AngleLerp(targetRot, 0.22f + chargeT * 0.45f);

					if (Projectile.timeLeft > 1)
						Projectile.Center += Main.rand.NextVector2Circular(
							1.5f + chargeT * 3f, 1.5f + chargeT * 3f);

					for (int i = 0; i < (int)(chargeT * 3f) + 1; i++) {
						Vector2 gp = Projectile.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(80f, 180f);
						Dust d = Dust.NewDustPerfect(gp, DustID.GemRuby,
							(Projectile.Center - gp) * 0.12f, 0, Color.Red, 1.0f);
						d.noGravity = true;
					}
				}

				if (Projectile.timeLeft == 1) {
					Projectile.localAI[1] = 1f;
					Projectile.rotation = (target.Center - Projectile.Center).ToRotation();
					Projectile.velocity = Projectile.rotation.ToRotationVector2() * 48f;
					Projectile.timeLeft = 140;
					SoundEngine.PlaySound(SoundID.Item71, Projectile.Center);
				}
			}
			else {
				Projectile.rotation = Projectile.velocity.ToRotation();
				if (Main.rand.NextBool(3)) {
					Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GemRuby,
						-Projectile.velocity * 0.15f, 0, Color.Red, 0.9f);
					d.noGravity = true;
				}
			}
		}

		public override bool PreDraw(ref Color lightColor) {
			Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
			Vector2 origin = tex.Size() / 2f;

			if (!IsLaunching && Projectile.timeLeft <= 60) {
				float progress = 1f - Projectile.timeLeft / 60f;
				float pulse = 0.4f + 0.6f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 28f);
				float lineAlpha = (progress * 0.5f + pulse * 0.3f) * 0.65f;

				Texture2D lineTex = TextureAssets.MagicPixel.Value;
				Vector2 beamStart = Projectile.Center - Main.screenPosition;

				Main.EntitySpriteDraw(lineTex, beamStart, new Rectangle(0, 0, 1, 1),
					Color.Red * lineAlpha, Projectile.rotation,
					new Vector2(0f, 0.5f), new Vector2(4000f, 18f), SpriteEffects.None, 0);
				Main.EntitySpriteDraw(lineTex, beamStart, new Rectangle(0, 0, 1, 1),
					Color.White * lineAlpha * 0.4f, Projectile.rotation,
					new Vector2(0f, 0.5f), new Vector2(4000f, 4f), SpriteEffects.None, 0);
			}

			for (int k = 0; k < Projectile.oldPos.Length; k++) {
				if (Projectile.oldPos[k] == Vector2.Zero)
					continue;
				Vector2 drawPos = Projectile.oldPos[k] - Main.screenPosition + origin
					+ new Vector2(0f, Projectile.gfxOffY);
				float factor = (float)(Projectile.oldPos.Length - k) / Projectile.oldPos.Length;
				Color trailColor = new Color(255, 0, 0, 0) * factor * (IsLaunching ? 0.9f : 0.3f);
				Main.EntitySpriteDraw(tex, drawPos, null, trailColor,
					Projectile.rotation, origin, Projectile.scale * 1.6f, SpriteEffects.None, 0);
			}

			Main.EntitySpriteDraw(tex,
				Projectile.Center - Main.screenPosition, null,
				new Color(255, 30, 30, 200) * Projectile.Opacity,
				Projectile.rotation, origin, Projectile.scale * 1.6f, SpriteEffects.None, 0);

			return false;
		}
	}

	public class DaggerPredictLine : ModProjectile
	{
		private const int TelegraphTicks = 48;
		private const int AfterShotTicks = 12;
		private const float BarrageSpeed = 52f;

		public override string Texture => "Terraria/Images/MagicPixel"; // 无需额外贴图

		public override void SetDefaults() {
			Projectile.width = 2;
			Projectile.height = 2;
			Projectile.tileCollide = false;
			Projectile.timeLeft = TelegraphTicks + AfterShotTicks;
		}

		public override void AI() {
			Projectile.localAI[0]++;

			if (Projectile.localAI[0] == TelegraphTicks) {
				if (Main.netMode != NetmodeID.MultiplayerClient) {
					float speed = Projectile.ai[0] > 0f ? Projectile.ai[0] : BarrageSpeed;
					Vector2 velocity = Projectile.ai[1].ToRotationVector2() * speed;
					int dagger = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity,
						ModContent.ProjectileType<BarrageDagger>(), 20, 1f, Projectile.owner);
					if (dagger >= 0 && dagger < Main.maxProjectiles) {
						Main.projectile[dagger].netUpdate = true;
					}
				}
			}
		}

		public override bool PreDraw(ref Color lightColor) {
			Texture2D lineTex = TextureAssets.MagicPixel.Value;
			float beamRotation = Projectile.ai[1];
			Vector2 drawPos = Projectile.Center - Main.screenPosition;
			Vector2 origin = new Vector2(0.5f, 0.5f);
			Vector2 scale = new Vector2(10000f, 10f);
			float telegraphProgress = MathHelper.Clamp(Projectile.localAI[0] / TelegraphTicks, 0f, 1f);
			float lingerProgress = Projectile.localAI[0] <= TelegraphTicks
				? 1f
				: 1f - (Projectile.localAI[0] - TelegraphTicks) / AfterShotTicks;
			lingerProgress = MathHelper.Clamp(lingerProgress, 0f, 1f);

			float width = MathHelper.Lerp(1.6f, 5.8f, telegraphProgress);
			float coreWidth = MathHelper.Lerp(0.5f, 2.2f, telegraphProgress);
			Color outerColor = Color.Lerp(new Color(95, 12, 12), new Color(210, 42, 36), telegraphProgress) * (0.26f + telegraphProgress * 0.48f) * lingerProgress;
			Color coreColor = Color.Lerp(new Color(145, 38, 38), new Color(255, 210, 185), telegraphProgress) * (0.18f + telegraphProgress * 0.42f) * lingerProgress;

			Main.EntitySpriteDraw(lineTex, drawPos, new Rectangle(0, 0, 1, 1),
				outerColor, beamRotation, origin, new Vector2(scale.X, width), SpriteEffects.None, 0);
			Main.EntitySpriteDraw(lineTex, drawPos, new Rectangle(0, 0, 1, 1),
				coreColor, beamRotation, origin, new Vector2(scale.X, coreWidth), SpriteEffects.None, 0);

			return false;
		}

	}
	public class BarrageDagger : ModProjectile
	{
		private const float SharedViewHalfWidth = 1320f;
		private const float SharedViewHalfHeight = 760f;

		public override string Texture => "ArknightsMod/Content/NPCs/Enemy/ThroughChapter4/GravityDagger_Barrage";

		public override void SetStaticDefaults() {
			ProjectileID.Sets.TrailingMode[Type] = 2;
			ProjectileID.Sets.TrailCacheLength[Type] = 18;
		}

		public override void SetDefaults() {
			Projectile.width = 22;
			Projectile.height = 8;
			Projectile.hostile = true;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 900;
			Projectile.aiStyle = -1;
		}

		public override void AI() {
			Projectile.rotation = Projectile.velocity.ToRotation();
			Projectile.localAI[0] += 0.45f + Projectile.velocity.Length() * 0.012f;

			if (Projectile.timeLeft < 885 && !IsWithinAnyActivePlayerView()) {
				Projectile.Kill();
			}
		}

		public override bool PreDraw(ref Color lightColor) {
			return CrownslayerTrailEffects.DrawGravityDaggerTrail(Projectile);
		}

		private bool IsWithinAnyActivePlayerView() {
			for (int i = 0; i < Main.maxPlayers; i++) {
				Player player = Main.player[i];
				if (!player.active || player.dead || player.ghost)
					continue;

				Rectangle approxView = Utils.CenteredRectangle(player.Center, new Vector2(SharedViewHalfWidth * 2f, SharedViewHalfHeight * 2f));
				if (approxView.Intersects(Projectile.Hitbox))
					return true;
			}

			return false;
		}
	}
}
