using ArknightsMod.Common.VisualEffects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.ThroughChapter4
{
	public class Crownslayer : ModNPC
	{
		public AIState LastSkill = AIState.Idle; // 记录上一个技能
		public int FogSkillCooldown = 0;
		public int ExSkillCooldown = 0;
		public int UtSkillCooldown = 0;
		// 记录已经触发过的阶段：0=未触发, 1=75%, 2=40%, 3=10%
		public int PhaseLevel = 0;
		private Vector2 phaseOffset; // 用于记录相对于玩家的偏移点位
									 // 记录当前召唤的小怪 ID 列表，用于判断是否全部消灭
		private System.Collections.Generic.List<int> MinionWhoAmIs = new System.Collections.Generic.List<int>();
		public float grayScaleIntensity = 0f;
		public override void SetStaticDefaults() {

		}
		public override void SetDefaults() {
			NPC.ai[0] = (float)AIState.Idle; // 强制初始状态为 Idle
			StateTimer = 60;                // 给它 1 秒的出生缓冲时间，防止瞬间发动技能
			Main.npcFrameCount[NPC.type] = 56;
			NPC.lifeMax = 4200;
			NPC.boss = true;
			NPC.damage = 40;
			NPC.defense = 10;
			NPC.width = 36;
			NPC.height = 64;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath3;
			NPC.value = 100f;
			NPC.knockBackResist = 0f;
			NPC.aiStyle = -1; // 重要：不使用任何预设AI
			NPCID.Sets.BossBestiaryPriority.Add(Type);
		}
		// 定义状态枚举
		private int damage = 40; //为某些情况设置的伤害，记得跟初始（经典）伤害同步。
								 // 在 NPC 类中添加
		public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
			float healthPercent = (float)NPC.life / NPC.lifeMax;

			// 1. 更新强度逻辑
			if (healthPercent <= 0.5f) {
				float target = 0.4f + (0.5f - healthPercent) * 0.8f;
				grayScaleIntensity = MathHelper.Lerp(grayScaleIntensity, target, 0.05f);

				// 半血下半透明逻辑：只有在非隐身状态下生效
				if (NPC.alpha < 200) {
					NPC.alpha = (int)MathHelper.Lerp(30, 180, grayScaleIntensity);
				}
			}
			else {
				grayScaleIntensity = MathHelper.Lerp(grayScaleIntensity, 0f, 0.05f);
				NPC.alpha = 0;
			}

			// 2. 本体融入环境 (仅应用半透明，不再渲染红色剪影)
			if (grayScaleIntensity > 0.01f) {
				drawColor = NPC.GetAlpha(drawColor);
			}

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
			// 动画播放速度：数值越小越快
			int frameSpeed = 6;
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
			if (NPC.alpha > 200)
				return;

			if (grayScaleIntensity > 0.05f) {
				Texture2D glowTex = Terraria.GameContent.TextureAssets.Extra[98].Value;
				Vector2 origin = glowTex.Size() / 2f;


				float finalIntensity = MathHelper.Clamp(grayScaleIntensity * 1.2f, 0f, 1f);
				Color haloColor = new Color(255, 0, 0, 10) * finalIntensity;

				// 3. 动态脉冲：稍微放慢了一点呼吸频率，看起来更自然
				float pulse = (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4f) * 0.2f + 1.6f;

				// 绘制两层光晕（从三层减为两层，增加通透感）
				for (int i = 0; i < 2; i++) {
					spriteBatch.Draw(
						glowTex,
						NPC.Center - screenPos,
						null,
						haloColor * (0.8f - i * 0.4f), // 每一层逐渐变淡
						0f,
						origin,
						NPC.scale * (pulse + i * 0.3f),
						SpriteEffects.None,
						0f
					);
				}
			}
		}
		public enum AIState
		{
			Idle,           // 常态（等待/冷却中）
			Skill_1,        // 技能1
			Skill_2,        // 技能2
			Skill_3,        // 技能3
			Skill_4,        // 技能4
			Skill_5,        // 技能5
			Skill_6,
			Skill_7,
			Recover,
			Summoning

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
			// 确保有目标，否则清空状态
			// 获取当前到目标的距离
			float healthPercent = (float)NPC.life / NPC.lifeMax;



			// 仅计算强度，不直接修改颜色
			if (healthPercent <= 0.5f) {
				grayScaleIntensity = MathHelper.Lerp(grayScaleIntensity, (0.5f - healthPercent) * 2f, 0.05f);
			}
			else {
				grayScaleIntensity = MathHelper.Lerp(grayScaleIntensity, 0f, 0.05f);
			}
			if (grayScaleIntensity > 0.2f && Main.rand.NextBool(1)) {
				// 在屏幕范围内随机生成微小的灰色烟尘
				Vector2 spawnPos = Main.screenPosition + new Vector2(Main.rand.Next(Main.screenWidth), Main.rand.Next(Main.screenHeight));
				Dust d = Dust.NewDustPerfect(spawnPos, DustID.Smoke, Vector2.Zero, 0, Color.GhostWhite, 3f);
				Dust d2 = Dust.NewDustPerfect(spawnPos, DustID.Smoke, Vector2.Zero, 0, Color.WhiteSmoke, 3f);
				d.noGravity = true;
				d.velocity *= 0.1f;
				d.alpha = 0;
				d2.noGravity = true;
				d2.velocity *= 0.1f;
				d2.alpha = 0;
			}
			if (grayScaleIntensity > 0.1f && NPC.alpha < 200) { // 隐身时不产生粒子
				if (Main.rand.NextBool(2)) {
					// 使用透明度较高的黑红色粒子
					// 这里的 200 是 Alpha 值，值越高越透明
					int dustType = Main.rand.NextBool() ? DustID.Shadowflame : DustID.Blood;
					Dust d = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, dustType, 0, 0, 200, default, Main.rand.NextFloat(1f, 2f));

					d.noGravity = true;
					d.velocity.Y = -Main.rand.NextFloat(0.5f, 2f); // 速度放慢，更像烟雾

					// 强制染成极深的红色
					d.color = Color.Lerp(Color.Black, Color.DarkRed, 0.5f);
					d.fadeIn = 0.4f;
				}
			}
			if (NPC.life > NPC.lifeMax * 0.5f) {
				Music = MusicLoader.GetMusicSlot("ArknightsMod/Music/Crownslayer1");
			}
			if (NPC.life < NPC.lifeMax * 0.5f) {
				Music = MusicLoader.GetMusicSlot("ArknightsMod/Music/Crownslayer2");
			}
			NPC.TargetClosest(true);
			Player target = Main.player[NPC.target];
			float distanceToTarget = Vector2.Distance(NPC.Center, target.Center);
			if (target.Center.X > NPC.Center.X) {
				NPC.spriteDirection = -1;
			}
			else {
				NPC.spriteDirection = 1;
			}
			if (target.dead || !target.active)
				return;


			// --- 阶段转场监测 ---
			if ((healthPercent <= 0.75f && PhaseLevel == 0) ||
				(healthPercent <= 0.40f && PhaseLevel == 1) ||
				(healthPercent <= 0.10f && PhaseLevel == 2)) {
				PhaseLevel++;
				CurrentAIState = AIState.Summoning;
				StateTimer = 0; // 重置计时器
				NPC.netUpdate = true;
			}
			if (CurrentAIState == AIState.Idle) {
				HandleIdle(target, distanceToTarget);
			}
			if (CurrentAIState != AIState.Idle && CurrentAIState != AIState.Skill_2 && CurrentAIState != AIState.Skill_3 && CurrentAIState != AIState.Skill_5 && CurrentAIState != AIState.Skill_6 && CurrentAIState != AIState.Skill_7) {
				NPC.noGravity = true;
				NPC.noTileCollide = true;
			}
			// 注意：如果是 Recover 状态，也应该受重力和碰撞影响
			else if (CurrentAIState == AIState.Recover) {
				NPC.noGravity = false;
				NPC.noTileCollide = false;
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
				case AIState.Recover:
					ExecuteRecover();
					break;
				case AIState.Summoning:
					ExecuteSummoning(target);
					break;
			}
		}

		// --- 核心方法：处理常态模式与技能切换 ---
		private void HandleIdle(Player target, float distance) {
			// 更新动画状态为走路或待机
			// CurrentState = NPCState.Walk; (对应你之前的动画代码)
			// --- 基础属性重置 ---
			NPC.noGravity = false;      // 常态受重力影响
			NPC.noTileCollide = false;   // 常态受物块碰撞影响
			NPC.damage = NPC.defDamage; // 恢复默认伤害
										// --- 1. 卡墙检查与修正 ---

			if (Collision.SolidCollision(NPC.position, NPC.width, NPC.height)) {
				// 寻找最近的空地并直接闪现
				Vector2 safePos = FindSafeSpot(NPC.Center);
				if (safePos != NPC.Center) {
					NPC.Center = safePos;
					NPC.velocity = Vector2.Zero; // 瞬移后急停，防止惯性再次入墙
					NPC.netUpdate = true;        // 确保联机同步
												 // --- 修改这里：进入恢复硬直 ---
					CurrentAIState = AIState.Recover;
					StateTimer = 18; // 设置硬直时间为 18 单位
					return;
				}

			}

			// --- 2. 移动逻辑 ---
			float maxSpeed = 2.4f;
			float acceleration = 0.2f;
			float friction = 0.15f; // 减速摩擦力
			float deadzone = 12f;   // 死区范围（像素）。如果水平距离小于 12，就不再左右横跳。

			float diffX = target.Center.X - NPC.Center.X;

			// 检查是否在死区内
			if (Math.Abs(diffX) < deadzone) {
				// 在死区内：快速减速至静止，避免抽搐
				if (NPC.velocity.X > 0) {
					NPC.velocity.X -= friction;
					if (NPC.velocity.X < 0)
						NPC.velocity.X = 0;
				}
				else if (NPC.velocity.X < 0) {
					NPC.velocity.X += friction;
					if (NPC.velocity.X > 0)
						NPC.velocity.X = 0;
				}
			}
			else {
				// 在死区外：正常的加速逻辑
				if (diffX > 0) {
					NPC.velocity.X += acceleration;
					// 掉头补正：如果正在向左跑却要向右转，给个双倍动力
					if (NPC.velocity.X < 0)
						NPC.velocity.X += acceleration;
				}
				else {
					NPC.velocity.X -= acceleration;
					// 掉头补正
					if (NPC.velocity.X > 0)
						NPC.velocity.X -= acceleration;
				}
			}

			// 限制最大速度
			NPC.velocity.X = MathHelper.Clamp(NPC.velocity.X, -maxSpeed, maxSpeed);
			// 平台下跳逻辑：如果玩家在下方且自己站在平台上
			if (target.Center.Y > NPC.Center.Y + 32f && Main.tile[(int)(NPC.Center.X / 16), (int)((NPC.position.Y + NPC.height + 8) / 16)].TileType == TileID.Platforms) {
				NPC.position.Y += 1f; // 轻轻沉入平台使其通过
			}

			// --- 3. 挥刀攻击逻辑 (距离 < 2格, 即约 32 像素) ---
			// 使用 NPC.localAI[0] 作为攻击内部冷却
			if (distance < 32f) {
				NPC.velocity.X = 0; // 靠近时停止水平移动
				if (NPC.localAI[0] <= 0) {
					// 播放攻击动画
					CurrentAnimation = NPCState.Attack1;

					// 生成透明穿透弹幕 (假设弹幕ID为 mProjectileType)
					// 这里的弹幕在你的 Projectile 类里应设置：projectile.penetrate = -1; projectile.alpha = 255;
					Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<TransparentSlash>(), damage / 2, 0, Main.myPlayer);

					NPC.localAI[0] = 90; // 1.5秒冷却
				}
			}
			else {
				CurrentAnimation = NPCState.Walk; // 没攻击时走路
			}

			if (NPC.localAI[0] > 0)
				NPC.localAI[0]--;
			if (FogSkillCooldown > 0)
				FogSkillCooldown--;
			if (ExSkillCooldown > 0)
				ExSkillCooldown--;
			if (UtSkillCooldown > 0)
				UtSkillCooldown--;
			StateTimer--;

			if (StateTimer <= 0) {
				var availableSkills = new System.Collections.Generic.List<AIState>();

				float distanceInTiles = distance / 16f; // 将像素距离转换为格数
				float healthPercent = (float)NPC.life / NPC.lifeMax;
				bool isPlayerFacingNPC = (target.direction == 1 && target.Center.X < NPC.Center.X) || (target.direction == -1 && target.Center.X > NPC.Center.X);

				// 2. 条件判定逻辑

				// 技能1：远距离（15格开外），允许连续发动
				if (distanceInTiles > 15f) {
					availableSkills.Add(AIState.Skill_1);
				}

				// 技能2：15格内 + 非连续
				if (distanceInTiles <= 15f && LastSkill != AIState.Skill_2) {
					availableSkills.Add(AIState.Skill_2);
				}

				// 技能3：血量 > 50% + 非连续
				if (healthPercent > 0.5f && LastSkill != AIState.Skill_3) {
					availableSkills.Add(AIState.Skill_3);
				}

				// 技能4：血量 < 50% + 独立冷却20s + 非连续
				if (healthPercent <= 0.5f && FogSkillCooldown <= 0 && LastSkill != AIState.Skill_4) {
					availableSkills.Add(AIState.Skill_4);
				}

				// 技能5：血量 < 50% + 非连续
				if (healthPercent <= 0.5f && LastSkill != AIState.Skill_5) {
					availableSkills.Add(AIState.Skill_5);
				}

				// 技能6：血量 < 50%  + 独立冷却10 s+ 非连续
				if (healthPercent <= 0.5f && ExSkillCooldown <= 0 && LastSkill != AIState.Skill_6) {
					availableSkills.Add(AIState.Skill_6);
				}
				// 技能6：血量 < 50%  + 独立冷却20 s+ 非连续
				if (healthPercent <= 0.5f && UtSkillCooldown <= 0 && LastSkill != AIState.Skill_7) {
					availableSkills.Add(AIState.Skill_7);
				}
				// 3. 随机选择并触发
				if (availableSkills.Count > 0) {
					AIState chosen = availableSkills[Main.rand.Next(availableSkills.Count)];

					// 如果选中了技能4，重置其独立冷却 (20秒 * 60帧)
					if (chosen == AIState.Skill_4)
						FogSkillCooldown = 20 * 60;
					if (chosen == AIState.Skill_6)
						ExSkillCooldown = 10 * 60;
					if (chosen == AIState.Skill_7)
						UtSkillCooldown = 20 * 60;

					CurrentAIState = chosen;
					LastSkill = chosen; // 记录本次技能
					StateTimer = 0;     // 重置计时器供技能内部使用
				}
				else {
					// 如果没有任何技能满足条件，保持 Idle 并延长一点等待时间防止死循环
					StateTimer = 30;
				}
			}
		}
		public override void PostAI() {
			if (NPC.ai[2] > 0) {
				NPC.ai[2] -= 1f; // 逐渐衰减
			}
		}
		private void ResetToIdle() {
			CurrentAIState = AIState.Idle;

			// 4. 根据血量计算下一次技能的冷却时间 (60帧 = 1秒)
			float lifeRatio = (float)NPC.life / NPC.lifeMax;
			float minTime = MathHelper.Lerp(0.5f, 1.5f, lifeRatio); // 血少时0.5s，血多时1.5s
			float maxTime = MathHelper.Lerp(1.5f, 2.5f, lifeRatio); // 血少时1.5s，血多时2.5s

			StateTimer = Main.rand.NextFloat(minTime, maxTime) * 60f;
		}
		private Vector2 FindSafeSpot(Vector2 currentPos) {
			Point tileCoords = currentPos.ToTileCoordinates();

			// 如果当前位置已经没有物块，直接返回
			if (!Collision.SolidCollision(currentPos - new Vector2(NPC.width / 2, NPC.height / 2), NPC.width, NPC.height)) {
				return currentPos;
			}

			// 在周围 16x16的范围内寻找最近的可容纳空间
			for (int i = 1; i < 16; i++) {
				for (int x = -i; x <= i; x++) {
					for (int y = -i; y <= i; y++) {
						Vector2 checkPos = currentPos + new Vector2(x * 16, y * 16);
						// 检测该位置是否能塞下 NPC 的碰撞箱且不是实体墙
						if (!Collision.SolidCollision(checkPos - new Vector2(NPC.width / 2, NPC.height / 2), NPC.width, NPC.height)) {
							return checkPos;
						}
					}
				}
			}
			return currentPos; // 实在没找到就原地不动（理论上极少发生）
		}
		// --- 技能槽位 ---

		private void ExecuteSkill1(Player target) {
			// TODO: 实现技能1
			// 结束后调用 ResetToIdle();
			StateTimer++; // 使用 StateTimer 作为技能内部的计时器

			// 第一阶段：跳跃出场并准备消失 (约 0.25 秒，假设 4 帧动画)
			if (StateTimer < 24) {
				CurrentAnimation = NPCState.JumpOut; // 播放跳跃出场动画
				NPC.velocity *= 0.9f; // 逐渐减速
			}
			// 第二阶段：完全消失 (0.5 秒)
			else if (StateTimer < 24 + 30) {
				CurrentAnimation = NPCState.Blank; // 切换到空白帧
				NPC.dontTakeDamage = true;         // 消失期间无敌
				NPC.velocity = Vector2.Zero;       // 停留在原处（或准备瞬移）
			}
			// 第三阶段：出现在目标身后并悬浮 (0.8 秒)
			else if (StateTimer < 24 + 30 + 60) {
				// 只在刚进入这个阶段的瞬间执行一次瞬移
				if (StateTimer == 55) {
					// 出现在玩家面前 6 格（160像素）的位置
					Vector2 teleportPos = target.Center + new Vector2(target.direction * 160f, -16f);
					NPC.Center = teleportPos;
					NPC.netUpdate = true; // 同步位置到服务器
				}

				CurrentAnimation = NPCState.Lurk; // 播放瞬移下落/准备动画
				NPC.dontTakeDamage = false;               // 恢复受伤

				// 悬浮逻辑：缓慢飘向玩家高度，保持锁定
				NPC.velocity = (target.Center - NPC.Center) * 0.01f;
			}
			// 第四阶段：向玩家方向突刺 (1.5 秒)
			else if (StateTimer < 24 + 30 + 60 + 20) {
				// 在突刺开始的瞬间锁定方向并给予爆发速度
				if (StateTimer == 24 + 30 + 60 + 1) {
					Vector2 dashDirection = target.Center - NPC.Center;
					dashDirection.Normalize();
					NPC.velocity = dashDirection * 20f; // 突刺速度

				}

				CurrentAnimation = NPCState.Attack2; // 切换到攻击1动画
				 // 临时提高突刺伤害（可选）
				NPC.velocity *= 0.92f;
				// 穿透逻辑：不进行碰撞减速，保持匀速
			}
			// 技能结束
			else {
				 // 恢复常规伤害
				ResetToIdle();   // 回到常态模式，进入随机冷却
			}
		}

		private void ExecuteSkill2(Player target) {
			StateTimer++;

			// --- 第一阶段：预警与投掷 (0.5s) ---
			if (StateTimer < 60) {
				CurrentAnimation = NPCState.Attack2;
				NPC.velocity *= 0.8f;
			}
			else if (StateTimer == 60) {
				// 发射弹幕
				Vector2 baseVel = Vector2.Normalize(target.Center - NPC.Center) * 16f;
				for (int i = -1; i <= 1; i++) {
					Vector2 shotVel = baseVel.RotatedBy(MathHelper.ToRadians(i * 25f));
					Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, shotVel,
						ModContent.ProjectileType<GravityDagger>(), (int)(damage * 0.4f), 2f, Main.myPlayer);
				}
			}
			// --- 第二阶段：JumpOut 消失动作 (30帧) ---
			else if (StateTimer < 84) {
				CurrentAnimation = NPCState.JumpOut;
				if (StateTimer == 73) {
					NPC.velocity = new Vector2(target.direction * 5f, -10f);
				}
			}
			// --- 第三阶段：关键修改！出现在玩家身后高处并【潜伏】 (0.5s) ---
			else if (StateTimer < 84 + 30) {
				if (StateTimer == 84) {
					// 闪现到玩家背后上方
					NPC.Center = target.Center + new Vector2(target.direction * -160f, -160f);
					NPC.velocity = Vector2.Zero;
					CurrentAnimation = NPCState.Lurk;

					// 闪现特效：在旧位置和新位置都产生烟尘
					for (int i = 0; i < 20; i++) {
						Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Smoke, 0, 0, 100, default, 1.5f);
					}
					NPC.netUpdate = true;
				}

				// --- 使用潜伏动画 ---


				// 锁定方向：即使在身后也要盯着玩家看
				NPC.spriteDirection = (target.Center.X > NPC.Center.X) ? -1 : 1;

				NPC.noTileCollide = true; // 悬浮期保持虚化
				NPC.velocity *= 0f;       // 彻底静止在空中
			}
			// --- 第四阶段：下落突刺 ---
			else if (StateTimer < 134 + 60) {
				if (StateTimer == 134) {
					CurrentAnimation = NPCState.JumpIn;
					NPC.noTileCollide = false; // 突刺开始，恢复碰撞
					NPC.noGravity = false;
					Vector2 targetPos = target.Center + new Vector2(0, 40f);

					// 计算向量
					Vector2 dashVel = targetPos - NPC.Center;
					dashVel.Normalize();
					NPC.velocity = dashVel * 19f;
					NPC.netUpdate = true;
				}
				if (StateTimer == 148) {
					NPC.velocity.X = 0;
					CurrentAnimation = NPCState.Lurk;
				}
			}
			else {
				ResetToIdle();
			}

		}

		private void ExecuteSkill3(Player target) {
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
			// --- 阶段 1：原地蓄力一秒 (0-60帧) + JumpOut (60-84帧) ---
			if (StateTimer < 60) {
				NPC.noGravity = true;
				NPC.noTileCollide = true;
				CurrentAnimation = NPCState.Lurk;
				NPC.velocity *= 0.8f;
			}
			else if (StateTimer < 84) {
				CurrentAnimation = NPCState.JumpOut;
				if (StateTimer == 60) {
					// 在消失这一刻，固定好第一跳的相对位置 (身后 15*16像素)
					// 假设 target.direction 为消失时的朝向参考
					phaseOffset = new Vector2(240f, -240f);
				}
			}
			// --- 阶段 2：第一次相位突袭 (84 + 30延迟 + 36悬浮) ---
			else if (StateTimer < 114 + 36) {
				// 消失 0.5秒 (30帧)
				if (StateTimer < 114) {
					CurrentAnimation = NPCState.Blank;
					NPC.alpha = 255;
					NPC.Center = target.Center + phaseOffset; // 持续跟随玩家中心，直到 114 帧出现
				}
				else {
					// 悬浮阶段 (36帧)
					NPC.alpha = 0;
					CurrentAnimation = NPCState.Attack2;
					NPC.velocity = Vector2.Zero; // 悬浮不移动

					if (StateTimer == 114 + 24) { // 第24帧发射飞刀
						ShootDaggers(target);
						// 此时计算好下一跳的偏移量 (另一侧上方)
						phaseOffset = new Vector2(-240f, -240f);
					}
				}
			}
			// --- 阶段 3：第二次相位突袭 (消失24帧 + 36帧悬浮) ---
			else if (StateTimer < 150 + 24 + 36) {
				int subTimer = (int)StateTimer - (114 + 36);
				if (subTimer < 24) {
					CurrentAnimation = NPCState.JumpOut;
					NPC.alpha = (int)MathHelper.Lerp(0, 255, subTimer / 24f);
				}
				else if (subTimer < 24 + 36) {
					NPC.alpha = 0;
					CurrentAnimation = NPCState.Attack2;
					NPC.Center = target.Center + phaseOffset; // 出现在另一侧
					NPC.velocity = Vector2.Zero;

					if (subTimer == 24 + 24) { // 第24帧再射一次
						ShootDaggers(target);
					}
				}
			}
			// --- 阶段 4：正上方下砸 ---
			else {
				int finalTimer = (int)StateTimer - (150 + 24 + 36);
				if (finalTimer < 15) { // 快速瞬移到上方准备
					NPC.alpha = 255;
					NPC.Center = target.Center + new Vector2(0, -288f); // 18格高
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
						NPC.velocity = new Vector2(0, 20f); // 垂直急速下坠
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

		private void ExecuteSkill4(Player target) {
			CurrentAnimation = NPCState.TeleportDown;
			NPC.velocity.X *= 0.5f;
			StateTimer++;

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
			// --- 阶段 2：第一次相位突袭 (84 + 30延迟 + 36悬浮) ---
			else if (StateTimer < 114 + 36) {
				// 消失 0.5秒 (30帧)
				if (StateTimer < 114) {
					CurrentAnimation = NPCState.Blank;
					NPC.alpha = 255;
					NPC.Center = target.Center + phaseOffset;
				}
				else {
					
					NPC.alpha = 0;
					CurrentAnimation = NPCState.Attack2;
					NPC.velocity = Vector2.Zero; // 悬浮不移动

					if (StateTimer == 114 + 24) { 
						ShootMoreDaggers(target);
						// 此时计算好下一跳的偏移量 (另一侧上方)
						phaseOffset = new Vector2(-360f, -360f);
					}
				}
			}
			else if (StateTimer < 180 + 36) {
				// 消失 0.5秒 (30帧)
				if (StateTimer < 180) {
					CurrentAnimation = NPCState.Blank;
					NPC.alpha = 255;
					NPC.Center = target.Center + phaseOffset; // 持续跟随玩家中心，直到 114 帧出现
				}
				else {
					// 悬浮阶段 (36帧)
					NPC.alpha = 0;
					CurrentAnimation = NPCState.Attack2;
					NPC.velocity = Vector2.Zero; // 悬浮不移动

					if (StateTimer == 180 + 24) { // 第24帧发射飞刀
						ShootMoreDaggers(target);
						// 此时计算好下一跳的偏移量 (另一侧上方)
						phaseOffset = new Vector2(360f, -360f);
					}
				}
			}
			else if (StateTimer < 246 + 36) {
				// 消失 0.5秒 (30帧)
				if (StateTimer < 246) {
					CurrentAnimation = NPCState.Blank;
					NPC.alpha = 255;
					NPC.Center = target.Center + phaseOffset; // 持续跟随玩家中心，直到 114 帧出现
				}
				else {
					// 悬浮阶段 (36帧)
					NPC.alpha = 0;
					CurrentAnimation = NPCState.Attack2;
					NPC.velocity = Vector2.Zero; // 悬浮不移动
					if (StateTimer == 246 + 24) { // 第24帧发射飞刀
						ShootMoreDaggers(target);
						// 此时计算好下一跳的偏移量 (另一侧上方)

					}

				}
			}
			else {
				int finalTimer = (int)StateTimer - (246 + 36);
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
		private void ExecuteSkill6(Player target) {
			StateTimer++;
			// 技能期间手动控制重力，防止自然重力干扰手感

			// --- 阶段 1: 站在原地投掷匕首 (0 - 90帧) ---
			if (StateTimer < 90) {
				NPC.noGravity = false;
				NPC.noTileCollide = false;
				NPC.velocity *= 0.8f;
				if (StateTimer % 6 == 0) {
					Vector2 targetPos = target.Center + new Vector2(Main.rand.NextFloat(-330, 330), Main.rand.NextFloat(-320, -150));
					Vector2 launchVel = (targetPos - NPC.Center) / 25f;
					Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, launchVel,
						ModContent.ProjectileType<StallDagger>(), (int)(NPC.damage * 0.6f), 2f, Main.myPlayer, NPC.whoAmI, targetPos.Y);
					SoundEngine.PlaySound(SoundID.Item71, NPC.Center);

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

			// --- 阶段 1: 起手式（插刀、起雾） ---
			if (StateTimer < 60) {
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

			// --- 阶段 2: 隐身与循环闪现冲刺 (60 - 960帧, 约15秒) ---
			else if (StateTimer < 960) {
				grayScaleIntensity = 0.85f; // 维持红雾渲染
				NPC.noGravity = true;
				int subTimer = (int)(StateTimer - 60) % 120;
				NPC.noTileCollide = true; // 闪现期间穿过地形
										  // 1. 寻找位置 + 锁定角度 + 显示判定线 (0 - 42帧)
				if (subTimer < 42) {
					NPC.alpha = 255;
					NPC.velocity = Vector2.Zero;

					if (subTimer == 1) {
						
						// --- 核心逻辑：计算固定 45 度角 ---
						// 玩家在 Boss 右边，就从左上往右下冲 (45°)；反之从右上往左下冲 (135°)
						float direction = (target.Center.X > NPC.Center.X) ? -1f : 1f;

						// 计算 45 度或 135 度的弧度
						// MathHelper.PiOver4 是 45度
						float dashRadian = (direction == 1f) ? MathHelper.PiOver4 : MathHelper.PiOver4 * 3f;

						// 存入 ai[3]，让判定线弹幕和后续冲刺逻辑直接读这个固定值
						NPC.ai[3] = dashRadian;

						// 根据 45 度角反推 Boss 的闪现位置 (距离玩家中心 400 像素)
						Vector2 offset = new Vector2(-direction * 300, -300); // 形成 45 度等腰直角三角形
						NPC.Center = target.Center + offset;

						// 召唤【不追踪】的静态判定线
						// 注意：把锁定的角度 NPC.ai[3] 传给弹幕的 ai[1]

						NPC.netUpdate = true;
					}
				}

				// 2. 固定角度冲刺 (42 - 72帧)
				else if (subTimer < 72) {
					NPC.alpha = 0;
					if (subTimer == 42) {
						CurrentAnimation = NPCState.Lurk;
						// 直接读取第一帧锁定的角度，绝对不会随玩家移动改变
						float dashAngle = NPC.ai[3];
						float speed = 15f + (StateTimer - 60) / 70f;
						NPC.velocity = dashAngle.ToRotationVector2() * speed;

						SoundEngine.PlaySound(SoundID.Item71, NPC.Center);
					}
					// 旋转角度锁定在冲刺方向
					
				}

				// 3. 镜像弹走 (72 - 102帧)
				else if (subTimer < 102) {
					if (subTimer == 72) {
						CurrentAnimation = NPCState.Lurk;
						NPC.velocity.Y = -NPC.velocity.Y; // 镜像反射
														  // 同样，这里的弹幕也应该使用锁定的角度方向
						
					}

					NPC.alpha = 0;
					
				}

				// 4. 彻底消失等待下一次闪现 (90帧 - 120帧)
				else {
					if (subTimer == 103) {


						int count = 5 + (int)(10 * (1 - (float)StateTimer / 960));
						float spacing = 270f - count * 6;
						float angleDeg = Main.rand.NextFloat(30f, 150f);
						float speed = 15f;
						float spawnDist = 1300f;

						float radian = MathHelper.ToRadians(angleDeg);
						Vector2 shootDir = radian.ToRotationVector2();
						Vector2 perpDir = new Vector2(-shootDir.Y, shootDir.X); // 垂直于射击方向的排队方向

						// 1. 确定这排“点”的中心位置（在玩家屏幕外 1300 像素）
						Vector2 centerPoint = target.Center - shootDir * spawnDist;

						// 2. 循环生成每一个“点”
						for (int i = 0; i < count; i++) {
							// 计算每个点相对于中心点的偏移
							float offset = (i - (count - 1) / 2f) * spacing;
							Vector2 spawnPos = centerPoint + perpDir * offset;

							// 3. 在这个点上，画一条线（生成预警弹幕）
							// 第二个参数传 spawnPos，保证弹幕的 Center 就在这个点上
							Projectile.NewProjectile(
								NPC.GetSource_FromAI(),
								spawnPos,
								Vector2.Zero,
								ModContent.ProjectileType<DaggerPredictLine>(),
								0, 0, Main.myPlayer,
								speed,
								radian
							);
						}




					}
					NPC.alpha = 255;
					NPC.velocity = Vector2.Zero;
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

		private void ExecuteRecover() {
			NPC.velocity *= 0.8f; // 保持原地不动或微弱减速
			CurrentAnimation = NPCState.Dodge; // 播放 Dodge 动画

			StateTimer--;
			if (StateTimer <= 0) {
				// 18单位时间结束后，回到 Idle，动画会自动由 HandleIdle 切回 Walk
				CurrentAIState = AIState.Idle;
				StateTimer = 30; // 给一点点缓冲防止立即连续触发技能
			}
		}
		private void ExecuteSummoning(Player target) {
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

					int count = 5 + (int)(6 * (1 - (float)NPC.life / NPC.lifeMax));
					float spacing = 240f - count * 6;  // 你的原始公式
					float angleDeg = Main.rand.NextFloat(0f, 180f); // 你的原始随机范围
					float speed = 13f; // 你的原始速度
					float spawnDist = 1300f; // 你的原始距离

					// 弧度和方向
					float radian = MathHelper.ToRadians(angleDeg);
					Vector2 shootDir = radian.ToRotationVector2(); // (0, 1) 代表向下

					// 阵列中心点：玩家中心 减去 (0, 800) = 玩家头顶 800 像素
					Vector2 centerOrigin = target.Center - shootDir * spawnDist;
					Vector2 perpDir = new Vector2(-shootDir.Y, shootDir.X); // 垂直向量 (1, 0)

					for (int i = 0; i < count; i++) {
						// 水平排开生成点
						Vector2 spawnPos = centerOrigin + perpDir * (i - (count - 1) / 2f) * spacing;

						// 召唤预警线
						Projectile.NewProjectile(
							NPC.GetSource_FromAI(),
							spawnPos,
							Vector2.Zero,
							ModContent.ProjectileType<DaggerPredictLine>(),
							0, 0, Main.myPlayer,
							speed,
							radian
						);
					}


				}
				// 根据不同的 PhaseLevel 执行不同的分批召唤计划
				switch (PhaseLevel) {
					case 1: // 75% 血量阶段
							// 第 0 秒：左边刷 A，右边刷 B
						if (summonTimer == 0) {
							SpawnMinion(ModContent.NPCType<HoundPro>(), true);  // true 代表左边
							SpawnMinion(ModContent.NPCType<SoldierLeader>(), false); // false 代表右边
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
						break;

					case 3: // 10% 血量阶段
						if (summonTimer == 0) {
							SpawnMinion(ModContent.NPCType<ShieldGuard>(), true);
							SpawnMinion(ModContent.NPCType<MortarGunner>(), true);
						}
						if (summonTimer == 60)
							SpawnMinion(ModContent.NPCType<MortarGunner>(), false);

						break;
				}

				// --- 阶段3：监测存活与退出条件 ---
				// 注意：必须在最后一波怪刷出之后，才开始监测是否全部死亡
				// 假设每个阶段最晚的一波是在第 3 秒（180帧）
				bool allWavesDispatched = summonTimer > 180;

				bool minionsAlive = false;
				foreach (int index in MinionWhoAmIs) {
					NPC minion = Main.npc[index];

					// 关键：必须同时满足以下三个条件，才判定为“召唤的小怪还活着”：
					// 1. active: 该索引位置有 NPC
					// 2. NPC 没死（life > 0）
					// 3. 这里的特殊判定：检查该 NPC 的来源是否是本 Boss 召唤的
					//    或者检查其 type 是否在你的召唤名单内
					if (minion.active && minion.life > 0 && minion.ai[3] == 999f) {
						// 这样即使史莱姆路过，只要它的索引不在 MinionWhoAmIs 里，就不会被统计
						minionsAlive = true;
						break;
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

			StateTimer++;
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
		private void ShootMoreDaggers(Player target) {

			Vector2 baseVel = Vector2.Normalize(target.Center - NPC.Center) * 20f;
			SoundEngine.PlaySound(SoundID.Item71, NPC.Center);
			for (int i = -2; i <= 2; i++) {
				Vector2 shotVel = baseVel.RotatedBy(MathHelper.ToRadians(i * 15f));
				Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, shotVel,
				ModContent.ProjectileType<GravityDagger>(), (int)(damage * 0.4f), 3f, Main.myPlayer);
			}

		}
		// 辅助方法：封装召唤逻辑，减少重复代码
		private void SpawnMinion(int type, bool onLeft) {
			float spawnX = onLeft ? Main.screenPosition.X - 48 : Main.screenPosition.X + Main.screenWidth + 48;
			float spawnY = Main.player[NPC.target].Center.Y - 32;

			Vector2 spawnPos = FindSafeSpot(new Vector2(spawnX, spawnY));
			int index = NPC.NewNPC(NPC.GetSource_FromAI(), (int)spawnPos.X, (int)spawnPos.Y, type);
			Main.npc[index].ai[3] = 999f;
			if (index < Main.maxNPCs) {
				MinionWhoAmIs.Add(index);
				Main.npc[index].netUpdate = true;

				// 召唤特效
				for (int i = 0; i < 15; i++) {
					Dust.NewDust(Main.npc[index].position, 32, 32, DustID.Shadowflame);
				}
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
	public class GroundDagger : ModProjectile
	{
		public override string Texture => "ArknightsMod/Content/NPCs/Enemy/ThroughChapter4/GravityDagger";
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
			// 既然是“插”在地上，贴图需要倾斜或者垂直
			// 假设你的贴图刀刃朝左上，旋转 MathHelper.PiOver4 * 3 左右能让它刀尖向下
			Projectile.rotation = -MathHelper.PiOver4*3;

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
		public override string Texture => "ArknightsMod/Content/NPCs/Enemy/ThroughChapter4/GravityDagger";
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
				// 修正贴图朝向左上角：需要补偿旋转偏移
				Projectile.tileCollide = false;
				Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver4;
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
				// 停止旋转，指向飞行方向
				Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4 * 3f;
				Projectile.localAI[0] = 4; // 进入最终锁定飞行
				Projectile.tileCollide = true; // 开始与地形交互
			}
			else if (Projectile.localAI[0] == 3) {
				// 垂直下落模式：刀尖向下
				Projectile.rotation = MathHelper.PiOver2 + MathHelper.PiOver4 * 3f;
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
		public override string Texture => "ArknightsMod/Content/NPCs/Enemy/ThroughChapter4/GravityDagger";
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
				Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4 * 3f;
				SoundEngine.PlaySound(SoundID.Item71, Projectile.Center);
			}
			else {
				// 飞行中不再旋转
				Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4 * 3f;
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
			Projectile.width = 64;
			Projectile.height = 64;
			Projectile.friendly = false;
			Projectile.hostile = true;     // 敌对弹幕
			Projectile.tileCollide = false; // 穿墙
			Projectile.penetrate = -1;     // 无限穿透
			Projectile.alpha = 255;        // 全透明
			Projectile.timeLeft = 2;       // 关键：只存在极短时间，产生一次判定即消失
		}

		public override void AI() {
			Projectile.velocity = Vector2.Zero; // 停在原地
		}
	}
	public class GravityDagger : ModProjectile
	{

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
				Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4 * 3f;
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
		}

		// --- 实现赤红色残影 ---
		public override bool PreDraw(ref Color lightColor) {
			Main.instance.LoadProjectile(Projectile.type);
			Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
			Texture2D trailtexture = ModContent.Request<Texture2D>("ArknightsMod/Common/VisualEffects/LineTrail").Value;
			TrailMaker.ProjectileDrawTailByConstWidth(Projectile, trailtexture, new Vector2(15, 15), new Color(120, 20, 20), new Color(100, 20, 30), 20f, true);

			// 绘制残影
			for (int i = 0; i < Projectile.oldPos.Length; i++) {
				// 计算残影透明度，越往后越淡
				float alpha = 1f - (float)i / Projectile.oldPos.Length;
				Vector2 drawPos = Projectile.oldPos[i] - Main.screenPosition + Projectile.Size / 2f;
				Color trailColor = Color.Red * alpha * 0.6f; // 赤红色，半透明

				Main.EntitySpriteDraw(texture, drawPos, null, trailColor, Projectile.oldRot[i], texture.Size() / 2f, Projectile.scale, SpriteEffects.None, 0);
			}

			// 绘制主体
			return true;
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
		public override string Texture => "ArknightsMod/Content/NPCs/Enemy/ThroughChapter4/GravityDagger";

		public override void SetStaticDefaults() {
			ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
			ProjectileID.Sets.TrailCacheLength[Projectile.type] = 20; // 加长残影
		}

		public override void SetDefaults() {
			Projectile.width = 45;
			Projectile.height = 45;
			Projectile.hostile = true;
			Projectile.tileCollide = false; // 不碰撞物块
			Projectile.penetrate = -1;
			Projectile.timeLeft = 660; // 10秒准备 + 1秒发射时间
			Projectile.alpha = 255;
		}

		private bool isLaunching = false;
		private Vector2 launchVelocity;

		public override void AI() {
			Player target = Main.player[(int)Projectile.ai[0]];
			if (!target.active || target.dead) {
				Projectile.Kill();
				return;
			}

			// --- 1. 矩阵边界与喷发逻辑 ---

			// 矩阵参数
			const float rangeX = 400f;
			const float rangeY = 400f;

			if (!isLaunching) {
				// A. 边界墙效果（让矩形边界清晰）
				// 每帧在矩形的四个边随机生成少量静止粒子，勾勒出轮廓
				for (int i = 0; i < 4; i++) {
					Vector2 edgePos;
					if (Main.rand.NextBool()) { // 左右边
						edgePos = Projectile.Center + new Vector2(Main.rand.NextBool() ? -rangeX : rangeX, Main.rand.NextFloat(-rangeY, rangeY));
					}
					else { // 上下边
						edgePos = Projectile.Center + new Vector2(Main.rand.NextFloat(-rangeX, rangeX), Main.rand.NextBool() ? -rangeY : rangeY);
					}
					Dust d = Dust.NewDustPerfect(edgePos, DustID.Smoke, Vector2.Zero, 150, Color.Red, 1.2f);
					d.noGravity = true;
				}

				// B. 从碰撞箱边缘向外喷发（中心留空）
				for (int j = 0; j < 12; j++) {
					float spreadAngle = Main.rand.NextFloat(MathHelper.TwoPi);
					Vector2 spawnDir = spreadAngle.ToRotationVector2();

					// --- 提示实现的数学逻辑 ---
					// 为了让粒子形成的轮廓是方形而非圆形，我们需要计算在该角度下到矩形边界的角度修正系数
					// 修正系数公式：1 / max(|cos(theta)|, |sin(theta)|)
					// 当 theta 为 45 度时，修正值约为 1.414
					float cos = Math.Abs((float)Math.Cos(spreadAngle));
					float sin = Math.Abs((float)Math.Sin(spreadAngle));
					float rectModifier = 1f / Math.Max(cos, sin);

					// 基础速度控制，确保粒子在消失前能到达边界
					// 400 像素距离 / 60 帧生命值 ≈ 6.6f 基础速度
					float baseSpeed = 28f;
					Vector2 vel = spawnDir * baseSpeed * rectModifier * Main.rand.NextFloat(0.5f, 1.7f);

					// 在碰撞箱外圈生成
					Vector2 spawnPos = Projectile.Center + spawnDir * 45f;

					int type = Main.rand.NextBool() ? DustID.Smoke : DustID.Cloud;
					Color c = Color.Lerp(Color.Red, Color.White, Main.rand.NextFloat(0.1f, 0.4f));

					Dust d = Dust.NewDustPerfect(spawnPos, type, vel, 100, c, Main.rand.NextFloat(2.5f, 3.8f));
					d.noGravity = true;

					// 阻力微调：让粒子在到达边界时平滑减速停下，形成清晰的“烟雾墙”
					d.velocity *= 0.97f;
					d.fadeIn = 0.5f;
				}
				for (int k = 0; k < 5; k++) {
					// 在 800x800 范围内随机选点
					Vector2 randomInnerPos = Projectile.Center + new Vector2(
						Main.rand.NextFloat(-rangeX, rangeX),
						Main.rand.NextFloat(-rangeY, rangeY)
					);

					// 内部烟雾：速度极慢或静止，主要为了遮挡视线
					Dust innerD = Dust.NewDustPerfect(randomInnerPos, DustID.Smoke, Vector2.Zero, 150, Color.Red * 0.5f, 3.5f);
					innerD.noGravity = true;
					innerD.velocity = Main.rand.NextVector2Circular(1f, 1f); // 极其轻微的漂浮感
				}

				// 矩阵减益判定（保持不变）
				Rectangle smokeRect = new Rectangle((int)Projectile.Center.X - (int)rangeX, (int)Projectile.Center.Y - (int)rangeY, (int)rangeX * 2, (int)rangeY * 2);
				if (target.getRect().Intersects(smokeRect)) {
					target.AddBuff(BuffID.BrokenArmor, 2);
					target.AddBuff(BuffID.Slow, 2);
				}

				// --- 2. 旋转与发射逻辑 (修正 45 度贴图) ---
				float textureRotationOffset = MathHelper.PiOver4 * 3f;

				if (!isLaunching) {
					Projectile.alpha = Math.Max(0, Projectile.alpha - 10);
					Vector2 aimDir = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY);


					if (!isLaunching) {
						Projectile.alpha = Math.Max(0, Projectile.alpha - 10);

						// 计算目标角度
						float targetRotation = aimDir.ToRotation() + textureRotationOffset;

						// --- 核心修复：分阶段逻辑 ---
						if (Projectile.timeLeft <= 30) {
							// A. 最后0.5秒：蓄力与剧烈锁定
							Projectile.rotation = Projectile.rotation.AngleLerp(targetRotation, 0.45f);

							// 蓄力震动：让匕首颤抖
							Projectile.Center += Main.rand.NextVector2Circular(2.5f, 2.5f);

							// 能量汇聚粒子：向中心倒流
							for (int i = 0; i < 2; i++) {
								Vector2 gatherPos = Projectile.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(100f, 160f);
								Vector2 gatherVel = (Projectile.Center - gatherPos) * 0.15f;
								Dust d = Dust.NewDustPerfect(gatherPos, DustID.GemRuby, gatherVel, 0, Color.Red, 1.2f);
								d.noGravity = true;
							}
						}
						else {
							// B. 前9.5秒：普通瞄准，始终对准玩家
							// 使用 Lerp 平滑一点，或者直接赋值 Projectile.rotation = targetRotation;
							Projectile.rotation = Projectile.rotation.AngleLerp(targetRotation, 0.15f);
						}

						// --- 3. 发射瞬间 (砰！) ---
						if (Projectile.timeLeft == 1) {
							isLaunching = true;
							Projectile.velocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * 42f;
							SoundEngine.PlaySound(SoundID.Item71, Projectile.Center);
							Projectile.timeLeft = 150;

							// 爆发大范围红色烟雾
							for (int i = 0; i < 60; i++) {
								Vector2 burstSpeed = Main.rand.NextVector2Unit() * Main.rand.NextFloat(8f, 20f);
								Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Smoke, burstSpeed, 50, Color.Red, 3.8f);
								d.noGravity = true;
								d.fadeIn = 1.5f;
							}
						}
					}
					else {
						// 飞行中维持方向
						Projectile.rotation = Projectile.velocity.ToRotation() + textureRotationOffset;
					}
				}
			}
		}

		public override bool PreDraw(ref Color lightColor) {
			Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
			Vector2 drawOrigin = texture.Size() / 2f;
			if (!isLaunching && Projectile.timeLeft <= 120) {
				Player target = Main.player[(int)Projectile.ai[0]];
				if (target.active && !target.dead) {

					float currentScale = Projectile.scale;

					// 2. 手动调整偏移量 (这是关键！)
					// 假设你的贴图是 22x8，放大后中心点理论在 (11 * scale, 4 * scale)
					// 如果线偏左：增加 X (比如从 4f 改到 6f 或 10f)
					// 如果线偏上：增加 Y (比如从 height/2f 改到 height/2f + 2f)
					float offsetX = 4f; // 增加这个值，射线会向“剑尖”方向移动
					float offsetY = 3f; // 增加这个值，射线会向“下方”移动

					Vector2 scaledOrigin = new Vector2(offsetX * currentScale, offsetY * currentScale);

					// 3. 计算修正后的起点
					// 注意：这里要减去贴图的视觉中心 (TextureSize / 2)
					Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
					Vector2 visualCenter = tex.Size() / 2f * currentScale;

					Vector2 beamStart = Projectile.Center + (scaledOrigin - visualCenter).RotatedBy(Projectile.rotation);

					// 2. 射线旋转逻辑
					float beamRotation = Projectile.rotation - (MathHelper.PiOver4 * 3f);

					// 3. 视觉效果：随时间推移颜色变实，并剧烈闪烁
					float progress = 1f - (Projectile.timeLeft / 120f);
					float pulse = 0.4f + 0.6f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 25f);
					float alpha = (progress * 0.4f + pulse * 0.4f) * 0.6f; // 稍微调低总透明度，防止20像素太挡视线

					Texture2D lineTex = TextureAssets.MagicPixel.Value;

					// 4. 绘制 20 像素宽的主射线
					Main.EntitySpriteDraw(
						lineTex,
						beamStart - Main.screenPosition,
						new Rectangle(0, 0, 1, 1),
						Color.Red * alpha,
						beamRotation,
						new Vector2(0f, 0.5f), // 确保这20像素是以发射点为中心上下展开的
						new Vector2(4000f, 20f), // 20像素宽
						SpriteEffects.None,
						0
					);

					// 5. 绘制中心亮线（让 20 像素宽的粗线看起来有层次感，而不是一坨色块）
					Main.EntitySpriteDraw(
						lineTex,
						beamStart - Main.screenPosition,
						new Rectangle(0, 0, 1, 1),
						Color.White * alpha * 0.4f,
						beamRotation,
						new Vector2(0f, 0.5f),
						new Vector2(4000f, 4f), // 内部 4 像素宽的白芯
						SpriteEffects.None,
						0
					);
				}
			}
			// 1. 绘制极长的赤红色残影
			for (int k = 0; k < Projectile.oldPos.Length; k++) {
				Vector2 drawPos = Projectile.oldPos[k] - Main.screenPosition + drawOrigin + new Vector2(0f, Projectile.gfxOffY);
				float factor = (float)(Projectile.oldPos.Length - k) / Projectile.oldPos.Length;
				Color trailColor = new Color(255, 0, k == 0 ? 0 : 0, 0) * factor * (isLaunching ? 1f : 0.4f);
				Main.EntitySpriteDraw(texture, drawPos, null, trailColor, Projectile.rotation, drawOrigin, Projectile.scale * 1.6f, SpriteEffects.None, 0);
			}

			// 2. 本体
			Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, new Color(255, 30, 30, 200) * Projectile.Opacity, Projectile.rotation, drawOrigin, Projectile.scale * 1.6f, SpriteEffects.None, 0);

			return false;
		}
	}
	public class StaticPredictLine : ModProjectile
	{
		public override string Texture => "Terraria/Images/MagicPixel";

		public override void SetDefaults() {
			Projectile.width = 1;
			Projectile.height = 1;
			Projectile.hostile = false;
			Projectile.tileCollide = false;
			Projectile.timeLeft = 42; // 0.7秒预警
			Projectile.alpha = 255;
		}

		public override void AI() {
			// 第一帧：根据生成时 Boss 指向玩家的角度锁定旋转
			if (Projectile.localAI[0] == 0) {
				NPC owner = Main.npc[(int)Projectile.ai[0]];
				Player target = Main.player[owner.target];

				// 锁定方向：指向产生那一刻的玩家中心
				Projectile.rotation = (target.Center - owner.Center).ToRotation();
				Projectile.localAI[0] = 1;
			}

			// 后续逻辑：虽然位置跟随 Boss 移动，但 rotation 保持不变
			NPC boss = Main.npc[(int)Projectile.ai[0]];
			if (boss.active) {
				Projectile.Center = boss.Center;
			}
			else {
				Projectile.Kill();
			}
		}

		public override bool PreDraw(ref Color lightColor) {
			// 绘制长度覆盖整个屏幕的实线
			float lineLength = 2400f;

			// 随时间衰减或闪烁
			float progress = 1f - (Projectile.timeLeft / 42f); // 0 到 1
			Color lineColor = Color.Red * (0.4f + 0.6f * progress); // 临近发射时颜色更亮

			Main.EntitySpriteDraw(
				Terraria.GameContent.TextureAssets.MagicPixel.Value,
				Projectile.Center - Main.screenPosition,
				new Rectangle(0, 0, 1, 1),
				lineColor,
				Projectile.rotation,
				new Vector2(0f, 0.5f),
				new Vector2(lineLength, 2f), // 极长的判定线
				SpriteEffects.None,
				0
			);
			return false;
		}
	}

	public class DaggerPredictLine : ModProjectile
	{
		public override string Texture => "Terraria/Images/MagicPixel"; // 无需额外贴图

		public override void SetDefaults() {
			Projectile.width = 2;
			Projectile.height = 2;
			Projectile.tileCollide = false;
			Projectile.timeLeft = 30; // 0.5秒预警
		}

		public override void AI() {
			// 【调试专用】：如果看不见判定线，取消下面两行的注释，如果你看到一排红粒子，说明位置是对的
			// Vector2 dustPos = Projectile.Center + Projectile.ai[1].ToRotationVector2() * Main.rand.NextFloat(0, 1000);
			// Dust.NewDustPerfect(dustPos, DustID.RedTorch, Vector2.Zero, 100, Color.Red, 1.5f).noGravity = true;

			if (Projectile.timeLeft == 1) {
				if (Projectile.owner == Main.myPlayer) {
					Vector2 velocity = Projectile.ai[1].ToRotationVector2() * Projectile.ai[0];
					// 变成带重力的匕首
					Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity,
						ModContent.ProjectileType<GravityDagger>(), 20, 1f, Main.myPlayer);
				}
			}
		}

		public override bool PreDraw(ref Color lightColor) {
			Texture2D lineTex = TextureAssets.MagicPixel.Value;
			float beamRotation = Projectile.ai[1]; // 拿回我们传进来的 radian
			float alpha = 0.5f * (1f - (float)Projectile.timeLeft / 30f);

			// 绘制原点：弹幕自己的中心（就是 NPC 传进来的 spawnPos）
			Vector2 drawPos = Projectile.Center - Main.screenPosition;

			// 【关键】Origin 设为中心 (0.5, 0.5)，线段就会像一根无限长的轴穿过 spawnPos
			Vector2 origin = new Vector2(0.5f, 0.5f);

			// 长度给 10000，确保这排平行的线绝对能横跨玩家屏幕
			Vector2 scale = new Vector2(10000f, 10f);

			Main.EntitySpriteDraw(lineTex, drawPos, new Rectangle(0, 0, 1, 1),
				Color.Red * alpha, beamRotation, origin, scale, SpriteEffects.None, 0);

			return false;
		}

	}
	public class CrownslayerScreenSystem : ModSystem
	{
		public override void PostDrawInterface(SpriteBatch spriteBatch) {
			// 1. 寻找当前的弑君者实例
			int bossType = ModContent.NPCType<Crownslayer>();
			for (int i = 0; i < Main.maxNPCs; i++) {
				NPC npc = Main.npc[i];
				if (npc.active && npc.type == bossType) {
					var cs = npc.ModNPC as Crownslayer;

					// 2. 如果强度足够，绘制置顶滤镜
					if (cs.grayScaleIntensity > 0.01f) {
						Texture2D pixel = Terraria.GameContent.TextureAssets.MagicPixel.Value;

						// 灰红色，不透明度控制在 130 左右 (0-255)
						int alpha = (int)(cs.grayScaleIntensity * 200);
						Color bloodFog = new Color(60, 50, 52, 20 + alpha);

						// 3. 这里的 Draw 是绘制在 UI 层级的，绝对置顶
						spriteBatch.Draw(
							pixel,
							new Rectangle(0, 0, Main.screenWidth, Main.screenHeight),
							bloodFog
						);
					}
					break; // 找到一个就退出循环
				}
			}
		}
	}
}


