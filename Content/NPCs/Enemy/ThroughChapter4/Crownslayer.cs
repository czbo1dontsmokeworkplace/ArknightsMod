using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader.Utilities;
using Terraria.Localization;
using Terraria.DataStructures;
using ArknightsMod.Content.Items;
using Microsoft.Xna.Framework;
using System;
using Terraria.Audio;

using Microsoft.Xna.Framework.Graphics;

namespace ArknightsMod.Content.NPCs.Enemy.ThroughChapter4
{
	public class Crownslayer:ModNPC
	{
		public AIState LastSkill = AIState.Idle; // 记录上一个技能
		public int FogSkillCooldown = 0;
		public override void SetStaticDefaults() {
			
		}
		public override void SetDefaults() {
			NPC.ai[0] = (float)AIState.Idle; // 强制初始状态为 Idle
			StateTimer = 60;                // 给它 1 秒的出生缓冲时间，防止瞬间发动技能
			Main.npcFrameCount[NPC.type] = 56;
			NPC.lifeMax = 2048;
			NPC.boss = true;
			NPC.damage = 2;
			NPC.defense = 8;
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
		public enum AIState
		{
			Idle,           // 常态（等待/冷却中）
			Skill_1,        // 技能1
			Skill_2,        // 技能2
			Skill_3,        // 技能3
			Skill_4,        // 技能4
			Skill_5,        // 技能5
			Skill_6,         // 技能6
			Recover
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
			Music = MusicLoader.GetMusicSlot("ArknightsMod/Music/CrownSlayer");
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
			if (CurrentAIState == AIState.Idle) {
				HandleIdle(target, distanceToTarget);
			}
			else {
				// 所有非 Idle 技能状态下：
				NPC.noGravity = true;        // 不受重力
				NPC.noTileCollide = true;     // 不受碰撞

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
				case AIState.Recover:
					ExecuteRecover();
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
			// 向玩家水平移动
			float moveSpeed = 2.6f;
			NPC.velocity.X = (target.Center.X > NPC.Center.X ? 1 : -1) * moveSpeed;

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
					Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<TransparentSlash>(), NPC.damage, 0, Main.myPlayer);

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

				// 技能2：15格内 + 玩家面对NPC + 非连续
				if (distanceInTiles <= 15f && isPlayerFacingNPC && LastSkill != AIState.Skill_2) {
					availableSkills.Add(AIState.Skill_2);
				}

				// 技能3：15格内 + 血量 > 50% + 非连续
				if (distanceInTiles <= 15f && healthPercent > 0.5f && LastSkill != AIState.Skill_3) {
					availableSkills.Add(AIState.Skill_3);
				}

				// 技能4：血量 < 50% + 独立冷却20s + 非连续
				if (healthPercent <= 0.5f && FogSkillCooldown <= 0 && LastSkill != AIState.Skill_4) {
					availableSkills.Add(AIState.Skill_4);
				}

				// 技能5：血量 < 50% + 15格内 + 非连续
				if (healthPercent <= 0.5f && distanceInTiles <= 15f && LastSkill != AIState.Skill_5) {
					availableSkills.Add(AIState.Skill_5);
				}

				// 技能6：血量 < 50% + 15格内 + 非连续
				if (healthPercent <= 0.5f && distanceInTiles <= 15f && LastSkill != AIState.Skill_6) {
					availableSkills.Add(AIState.Skill_6);
				}

				// 3. 随机选择并触发
				if (availableSkills.Count > 0) {
					AIState chosen = availableSkills[Main.rand.Next(availableSkills.Count)];

					// 如果选中了技能4，重置其独立冷却 (20秒 * 60帧)
					if (chosen == AIState.Skill_4)
						FogSkillCooldown = 20 * 60;

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
			else if (StateTimer < 24 + 30 + 48) {
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
			else if (StateTimer < 24 + 30 + 48 + 20) {
				// 在突刺开始的瞬间锁定方向并给予爆发速度
				if (StateTimer == 24 + 30 + 48 + 1) {
					Vector2 dashDirection = target.Center - NPC.Center;
					dashDirection.Normalize();
					NPC.velocity = dashDirection * 28f; // 突刺速度

				}

				CurrentAnimation = NPCState.Attack2; // 切换到攻击1动画
				NPC.damage = 80; // 临时提高突刺伤害（可选）
				NPC.velocity *= 0.92f;
				// 穿透逻辑：不进行碰撞减速，保持匀速
			}
			// 技能结束
			else {
				NPC.damage = 2; // 恢复常规伤害
				ResetToIdle();   // 回到常态模式，进入随机冷却
			}
		}

		private void ExecuteSkill2(Player target) {
			// TODO: 实现技能2
			if (StateTimer == 0) {
				string message = "发动2技能";
				Color messageColor = Color.Orange; // 设置文字颜色

				// 判定：如果是单机或客户端
				if (Main.netMode == NetmodeID.SinglePlayer) {
					Main.NewText(message, messageColor);
				}
				// 判定：如果是服务器（广播给所有玩家）
				else if (Main.netMode == NetmodeID.Server) {
					Terraria.Chat.ChatHelper.BroadcastChatMessage(Terraria.Localization.NetworkText.FromLiteral(message), messageColor);
				}

				// 这里的 StateTimer 设为 1 只是为了防止逻辑重复执行
				StateTimer = 1;
			}

			// 立即结束技能，回到常态循环
			ResetToIdle();

		}

		private void ExecuteSkill3(Player target) {
			// TODO: 实现技能3
			if (StateTimer == 0) {
				string message = "发动3技能";
				Color messageColor = Color.Orange; // 设置文字颜色

				// 判定：如果是单机或客户端
				if (Main.netMode == NetmodeID.SinglePlayer) {
					Main.NewText(message, messageColor);
				}
				// 判定：如果是服务器（广播给所有玩家）
				else if (Main.netMode == NetmodeID.Server) {
					Terraria.Chat.ChatHelper.BroadcastChatMessage(Terraria.Localization.NetworkText.FromLiteral(message), messageColor);
				}

				// 这里的 StateTimer 设为 1 只是为了防止逻辑重复执行
				StateTimer = 1;
			}

			// 立即结束技能，回到常态循环
			ResetToIdle();
		}

		private void ExecuteSkill4(Player target) {
			// TODO: 实现技能4
			if (StateTimer == 0) {
				string message = "发动4技能";
				Color messageColor = Color.Orange; // 设置文字颜色

				// 判定：如果是单机或客户端
				if (Main.netMode == NetmodeID.SinglePlayer) {
					Main.NewText(message, messageColor);
				}
				// 判定：如果是服务器（广播给所有玩家）
				else if (Main.netMode == NetmodeID.Server) {
					Terraria.Chat.ChatHelper.BroadcastChatMessage(Terraria.Localization.NetworkText.FromLiteral(message), messageColor);
				}

				// 这里的 StateTimer 设为 1 只是为了防止逻辑重复执行
				StateTimer = 1;
			}

			// 立即结束技能，回到常态循环
			ResetToIdle();
		}

		private void ExecuteSkill5(Player target) {
			// TODO: 实现技能5
			if (StateTimer == 0) {
				string message = "发动5技能";
				Color messageColor = Color.Orange; // 设置文字颜色

				// 判定：如果是单机或客户端
				if (Main.netMode == NetmodeID.SinglePlayer) {
					Main.NewText(message, messageColor);
				}
				// 判定：如果是服务器（广播给所有玩家）
				else if (Main.netMode == NetmodeID.Server) {
					Terraria.Chat.ChatHelper.BroadcastChatMessage(Terraria.Localization.NetworkText.FromLiteral(message), messageColor);
				}

				// 这里的 StateTimer 设为 1 只是为了防止逻辑重复执行
				StateTimer = 1;
			}

			// 立即结束技能，回到常态循环
			ResetToIdle();
		}

		private void ExecuteSkill6(Player target) {
			// TODO: 实现技能6
			if (StateTimer == 0) {
				string message = "发动6技能";
				Color messageColor = Color.Orange; // 设置文字颜色

				// 判定：如果是单机或客户端
				if (Main.netMode == NetmodeID.SinglePlayer) {
					Main.NewText(message, messageColor);
				}
				// 判定：如果是服务器（广播给所有玩家）
				else if (Main.netMode == NetmodeID.Server) {
					Terraria.Chat.ChatHelper.BroadcastChatMessage(Terraria.Localization.NetworkText.FromLiteral(message), messageColor);
				}

				// 这里的 StateTimer 设为 1 只是为了防止逻辑重复执行
				StateTimer = 1;
			}

			// 立即结束技能，回到常态循环
			ResetToIdle();
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
}

