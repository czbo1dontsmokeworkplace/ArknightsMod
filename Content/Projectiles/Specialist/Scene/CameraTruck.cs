using System;
using System.Collections.Generic;
using System.IO;
using ArknightsMod.Content.Buffs.Specialist.Scene;
using ArknightsMod.Content.Items.Weapons.Specialist.Scene;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace ArknightsMod.Content.Projectiles.Specialist.Scene
{
	// 移动摄影车：部署在地面上的召唤物（占用 1 个原版仆从位）。
	// 由「身体」「头部」两张贴图组成，待机时头身以不同频率轻微振动
	// （身体只向下沉、底部不离地；头部相对身体最多分离 0.5px）。
	// 攻击：附近有敌人时朝其左/右「向前小跳」并整体前倾、头部被颠一下；
	// 小跳为「原地往返」不产生位移，扑出期间对敌人造成召唤接触伤害。
	// 拥有独立血条：召唤时血量/防御 = 玩家当时的最大生命/防御。
	public class CameraTruck : ModProjectile
	{
		public const int BodyWidth = 38;
		public const int BodyHeight = 20;
		public const int HeadWidth = 38;
		public const int HeadHeight = 18;

		public const int Width = BodyWidth;
		public const int Height = BodyHeight + HeadHeight;

		// 同一玩家可同时存在的摄影车硬上限。
		public const int MaxTrucks = 5;

		private const float DrawScale = 1.25f;

		// 待机振动
		private const float BodyBobSpeed = 0.085f;
		private const float HeadBobSpeed = 0.115f;
		private const float BodyBobAmplitude = 2f;
		private const float HeadBodyMaxGap = 0.5f;

		// 攻击（小跳）：仅当敌人在「左右约一个车身」且大致同高时才出击
		private const float AttackReachX = 48f;   // 横向攻击距离 ≈ 一个车身
		private const float AttackBandY = 40f;     // 纵向容差（敌人需与小车大致同高）
		private const int HopDuration = 22;
		private const int HopCooldownMax = 38;
		private const float HopForward = 26f;
		private const float HopHeight = 15f;
		private const float LeanMax = 0.22f;

		// 头部被颠的弹簧
		private const float HeadSpringK = 0.2f;
		private const float HeadDamp = 0.22f;
		private const float HopLandJolt = 2.6f;
		private const float HopTakeoffJolt = -0.8f;

		// 受伤
		private const int ReceiveDamageCooldownMax = 30;

		// 迷彩 / 眩晕 / 侦查圈
		private const float CamouflageForgetRange = 320f; // 免疫目标超出此距离则重新选取
		private const float StunHeadAngle = 0.6f;         // 眩晕时头部斜向下角度
		private const int ReconRingSegments = 60;
		private const float ReconRingBandWidth = 10f;     // 侦查圈光带宽度

		private ref float AnimTimer => ref Projectile.ai[0];
		private ref float HopTimer => ref Projectile.ai[1];
		private ref float CooldownTimer => ref Projectile.ai[2];

		private int hopDir = 1;
		private int facingDir = 1;   // 朝向（-1=左，1=右），用于贴图翻转与转向
		private float hopStartCenterX;
		private float hopGroundBottomY;
		private float headJoltOffset;
		private float headJoltVel;

		// 独立生命
		internal int life = -1;
		internal int lifeMax = 100;
		internal int defense;
		internal int spawnOrder;
		private int receiveCooldown;
		private bool lifeInitialized;

		// 技能相关
		internal bool freeSummon;     // 技能二额外召唤，不占仆从位
		private int immuneNpcId = -1; // 迷彩免疫目标
		internal int stunTimer;       // 眩晕剩余刻

		private bool IsHopping => HopTimer > 0f;

		private static readonly System.Collections.Generic.Dictionary<int, int> NextSpawnOrderByPlayer = new();

		public override string Texture => "ArknightsMod/Content/Projectiles/Specialist/Scene/CameraTruck";

		public override void SetStaticDefaults() {
			ProjectileID.Sets.MinionSacrificable[Type] = true;
		}

		public override void SetDefaults() {
			Projectile.width = Width;
			Projectile.height = Height;
			Projectile.friendly = true;
			Projectile.minion = true;
			Projectile.DamageType = DamageClass.Summon;
			// 不占用 vanilla 仆从位：摄影车数量完全由模组自管（MaxTrucks + GetDeployStats），
			// 避免新车（尤其技能二的免位车）创建瞬间触发 vanilla 的 MinionSacrificable 牺牲，把已有车清掉。
			Projectile.minionSlots = 0f;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.aiStyle = -1;
			Projectile.timeLeft = 2;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = HopDuration;
		}

		public override void OnSpawn(IEntitySource source) {
			freeSummon = Projectile.ai[0] != 0f; // SummonTruck 用 ai0 传入「免位」标记（区分是否计入部署配额）

			Projectile.velocity = Vector2.Zero;
			SnapToGround();
			AnimTimer = Main.rand.Next(360);

			// 血量/防御 = 召唤时玩家的最大生命/防御
			Player owner = Main.player[Projectile.owner];
			lifeMax = Math.Max(1, owner.statLifeMax2);
			life = lifeMax;
			defense = owner.statDefense;
			lifeInitialized = true;

			if (!NextSpawnOrderByPlayer.TryGetValue(Projectile.owner, out int next))
				next = 0;
			spawnOrder = next;
			NextSpawnOrderByPlayer[Projectile.owner] = next + 1;
		}

		public override bool? CanCutTiles() => false;

		// 仅小跳扑击期间、且未眩晕时造成接触伤害。
		public override bool MinionContactDamage() => IsHopping && stunTimer <= 0;

		// 技能攻击加成。
		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
			float m = SceneCameraSkills.AttackMult(Main.player[Projectile.owner]);
			if (m != 1f)
				modifiers.SourceDamage *= m;
		}

		public override void AI() {
			Player owner = Main.player[Projectile.owner];
			if (owner.dead || !owner.active || !owner.HasBuff<CameraTruckBuff>()) {
				Projectile.Kill();
				return;
			}

			Projectile.timeLeft = 2;
			Projectile.velocity = Vector2.Zero;

			if (!lifeInitialized && life < 0) {
				lifeMax = Math.Max(1, owner.statLifeMax2);
				life = lifeMax;
				defense = owner.statDefense;
				lifeInitialized = true;
			}

			UpdateHeadJoltSpring();

			if (stunTimer > 0) {
				stunTimer--;       // 眩晕：原地待机、不攻击
				SnapToGround();
			}
			else if (IsHopping) {
				UpdateHop();
			}
			else {
				SnapToGround();
				bool hasTarget = TryFindTarget(owner, out int dir);
				if (hasTarget)
					facingDir = dir; // 先转向面对目标（即便仍在冷却中）
				if (CooldownTimer > 0f)
					CooldownTimer--;
				else if (hasTarget)
					StartHop(dir);
			}

			if (TakeIncomingDamage(owner))
				return; // 已死亡

			AnimTimer++;
		}

		private void StartHop(int dir) {
			HopTimer = 1f;
			hopDir = dir;
			hopStartCenterX = Projectile.Center.X;
			hopGroundBottomY = Projectile.Bottom.Y;
			headJoltVel += HopTakeoffJolt;
		}

		// 原地往返小跳：水平/竖直都用 sin(pi*p)（中途到达最远/最高，结束回到原点）。
		private void UpdateHop() {
			float p = MathHelper.Clamp(HopTimer / HopDuration, 0f, 1f);
			float swing = (float)Math.Sin(MathHelper.Pi * p);
			float fwd = HopForward * swing;
			float up = HopHeight * swing;

			float centerX = hopStartCenterX + hopDir * fwd;
			float bottomY = hopGroundBottomY - up;
			Projectile.position.X = centerX - Projectile.width / 2f;
			Projectile.position.Y = bottomY - Projectile.height;

			HopTimer++;
			if (HopTimer > HopDuration) {
				HopTimer = 0f;
				CooldownTimer = HopCooldownMax;
				// 精确回到起跳前的 X，确保不产生累计位移。
				Projectile.position.X = hopStartCenterX - Projectile.width / 2f;
				SnapToGround();
				Projectile.ResetLocalNPCHitImmunity();
				headJoltVel += HopLandJolt;
			}
		}

		private void UpdateHeadJoltSpring() {
			float accel = -HeadSpringK * headJoltOffset - HeadDamp * headJoltVel;
			headJoltVel += accel;
			headJoltOffset += headJoltVel;
		}

		private bool TryFindTarget(Player owner, out int dir) {
			dir = facingDir;
			Vector2 center = Projectile.Center;
			NPC best = null;
			float bestDx = float.MaxValue;

			bool InRange(NPC npc) {
				if (npc == null || !npc.active || !npc.CanBeChasedBy(Projectile))
					return false;
				float dx = Math.Abs(npc.Center.X - center.X);
				float dy = Math.Abs(npc.Center.Y - center.Y);
				return dx <= AttackReachX && dy <= AttackBandY;
			}

			if (owner.HasMinionAttackTargetNPC) {
				NPC marked = Main.npc[owner.MinionAttackTargetNPC];
				if (InRange(marked))
					best = marked;
			}

			if (best == null) {
				for (int i = 0; i < Main.maxNPCs; i++) {
					NPC npc = Main.npc[i];
					if (!InRange(npc))
						continue;
					float dx = Math.Abs(npc.Center.X - center.X);
					if (dx < bestDx) {
						bestDx = dx;
						best = npc;
					}
				}
			}

			if (best == null)
				return false;

			dir = best.Center.X >= center.X ? 1 : -1;
			return true;
		}

		// 受到敌方接触/弹幕伤害；仅在 owner 客户端结算并同步。返回 true 表示已死亡。
		private bool TakeIncomingDamage(Player owner) {
			if (Main.myPlayer != Projectile.owner)
				return false;

			if (receiveCooldown > 0) {
				receiveCooldown--;
				return false;
			}

			// 迷彩：技能一生效且未眩晕时，免疫单一「最近」敌人的接触伤害。
			bool camo = stunTimer <= 0 && SceneCameraSkills.Skill1Active(owner);
			if (camo && immuneNpcId >= 0) {
				NPC im = Main.npc[immuneNpcId];
				if (!im.active || im.friendly || Vector2.Distance(im.Center, Projectile.Center) > CamouflageForgetRange)
					immuneNpcId = -1; // 目标失效/远离 → 重新选取
			}
			if (!camo)
				immuneNpcId = -1;

			foreach (NPC npc in Main.ActiveNPCs) {
				if (!npc.active || npc.friendly || npc.damage <= 0 || npc.dontTakeDamage)
					continue;
				if (!Projectile.Hitbox.Intersects(npc.Hitbox))
					continue;

				if (camo) {
					if (immuneNpcId < 0) {        // 首个来袭敌人成为免疫目标
						immuneNpcId = npc.whoAmI;
						continue;
					}
					if (npc.whoAmI == immuneNpcId) // 免疫目标的伤害被免除
						continue;
				}
				return ApplyDamage(npc.damage);    // 其他（第二个）敌人正常造成伤害
			}

			// 敌对弹幕（迷彩不免疫弹幕）
			foreach (Projectile other in Main.ActiveProjectiles) {
				if (!other.active || !other.hostile || other.owner == Projectile.owner)
					continue;
				if (!Projectile.Hitbox.Intersects(other.Hitbox))
					continue;
				return ApplyDamage(other.damage);
			}

			return false;
		}

		private bool ApplyDamage(int rawDamage) {
			int eff = (int)(defense * SceneCameraSkills.DefenseMult(Main.player[Projectile.owner]));
			int dealt = Math.Max(1, rawDamage - eff);
			life -= dealt;
			receiveCooldown = ReceiveDamageCooldownMax;
			Projectile.netUpdate = true;

			if (!Main.dedServ) {
				CombatText.NewText(Projectile.getRect(), Color.OrangeRed, dealt);
				SoundEngine.PlaySound(SoundID.NPCHit1, Projectile.Center);
			}

			if (life <= 0) {
				DeathEffect();
				Projectile.Kill();
				return true;
			}
			return false;
		}

		private void DeathEffect() {
			if (Main.dedServ)
				return;
			SoundEngine.PlaySound(SoundID.NPCDeath4, Projectile.Center);
			for (int i = 0; i < 22; i++) {
				Vector2 vel = Main.rand.NextVector2Circular(3.5f, 3.5f);
				Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(14f, 12f),
					DustID.GreenTorch, vel, 0, default, Main.rand.NextFloat(1.1f, 1.8f));
				d.noGravity = true;
			}
			for (int i = 0; i < 8; i++) {
				Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Smoke,
					Main.rand.NextVector2Circular(2.2f, 2.2f), 120, default, 1.1f);
				d.noGravity = true;
			}
		}

		public override bool PreDraw(ref Color lightColor) {
			Player owner = Main.player[Projectile.owner];
			bool stunned = stunTimer > 0;

			// 侦查圈光带（技能二、未眩晕时）：圆心=车中心，绿色，不透明度 ≤40%。
			if (!stunned && SceneCameraSkills.Skill2Active(owner))
				DrawReconRing();

			Texture2D bodyTex = TextureAssets.Projectile[Type].Value;
			Texture2D headTex = ModContent.Request<Texture2D>(
				"ArknightsMod/Content/Projectiles/Specialist/Scene/CameraTruck_Head").Value;

			float bodyBob = (IsHopping || stunned) ? 0f : (1f - (float)Math.Cos(AnimTimer * BodyBobSpeed)) * 0.5f * BodyBobAmplitude;
			float lean = (!stunned && IsHopping) ? LeanMax * (float)Math.Sin(MathHelper.Pi * MathHelper.Clamp(HopTimer / HopDuration, 0f, 1f)) * hopDir : 0f;
			float idleSep = ((float)Math.Sin(AnimTimer * HeadBobSpeed) * 0.5f + 0.5f) * HeadBodyMaxGap;
			// 眩晕：头部固定斜向下看。
			float headLean = stunned ? StunHeadAngle * facingDir : lean;

			Vector2 footScreen = new Vector2(Projectile.Center.X, Projectile.Bottom.Y) - Main.screenPosition
				+ new Vector2(0f, Projectile.gfxOffY);

			// 朝向翻转：面向左侧目标时水平翻转贴图，实现"转向"。
			SpriteEffects fx = facingDir < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

			Vector2 bodyOrigin = new Vector2(bodyTex.Width / 2f, bodyTex.Height);
			Vector2 pivot = footScreen + new Vector2(0f, bodyBob);
			Main.EntitySpriteDraw(bodyTex, pivot, null, lightColor, lean, bodyOrigin, DrawScale, fx, 0);

			float effectiveGap = Math.Min(idleSep - headJoltOffset, HeadBodyMaxGap);
			Vector2 headLocalUp = new Vector2(0f, -(bodyTex.Height * DrawScale) - effectiveGap);
			Vector2 headPos = pivot + headLocalUp.RotatedBy(lean);
			Vector2 headOrigin = new Vector2(headTex.Width / 2f, headTex.Height);
			Main.EntitySpriteDraw(headTex, headPos, null, lightColor, headLean, headOrigin, DrawScale, fx, 0);

			return false;
		}

		// 侦查圈：绿色环形光带（半径 = SceneCameraSkills.ReconRadiusPx）。
		private void DrawReconRing() {
			float rOut = SceneCameraSkills.ReconRadiusPx;
			float rIn = Math.Max(2f, rOut - ReconRingBandWidth);
			Vector2 c = Projectile.Center;
			Color col = new Color(60, 220, 90) * 0.35f; // ≤40% 不透明度

			var verts = new List<VertexPositionColor>(ReconRingSegments * 6);
			for (int i = 0; i < ReconRingSegments; i++) {
				float a0 = MathHelper.TwoPi * i / ReconRingSegments;
				float a1 = MathHelper.TwoPi * (i + 1) / ReconRingSegments;
				Vector2 d0 = a0.ToRotationVector2(), d1 = a1.ToRotationVector2();
				Vector2 o0 = c + d0 * rOut, o1 = c + d1 * rOut;
				Vector2 i0 = c + d0 * rIn, i1 = c + d1 * rIn;
				verts.Add(ScenePrimitiveRenderer.Vert(i0, col)); verts.Add(ScenePrimitiveRenderer.Vert(o0, col)); verts.Add(ScenePrimitiveRenderer.Vert(o1, col));
				verts.Add(ScenePrimitiveRenderer.Vert(i0, col)); verts.Add(ScenePrimitiveRenderer.Vert(o1, col)); verts.Add(ScenePrimitiveRenderer.Vert(i1, col));
			}
			ScenePrimitiveRenderer.DrawTriangles(verts);
		}

		public override void PostDraw(Color lightColor) {
			if (Main.dedServ || life < 0 || lifeMax <= 0)
				return;

			float topWorldY = Projectile.Bottom.Y - (BodyHeight + HeadHeight) * DrawScale + Projectile.gfxOffY;
			float barY = topWorldY - 8f;
			float alpha = Lighting.Brightness((int)(Projectile.Center.X / 16f), (int)((Projectile.Center.Y + Projectile.gfxOffY) / 16f));
			Main.instance.DrawHealthBar(Projectile.Center.X, barY, life, lifeMax, alpha, 1f);

			// 悬停显示数值
			Vector2 mouse = Main.MouseWorld;
			if (new Rectangle((int)mouse.X - 8, (int)mouse.Y - 8, 16, 16).Intersects(Projectile.getRect())) {
				Vector2 textPos = new Vector2(Projectile.Center.X, barY - 16f) - Main.screenPosition;
				string text = $"{Math.Max(0, life)}/{lifeMax}";
				Vector2 origin = FontAssets.MouseText.Value.MeasureString(text) * 0.5f;
				ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, FontAssets.MouseText.Value,
					text, textPos, Color.White, 0f, origin, Vector2.One * 0.85f);
			}
		}

		public override void SendExtraAI(BinaryWriter writer) {
			writer.Write(life);
			writer.Write(lifeMax);
			writer.Write(defense);
			writer.Write(spawnOrder);
			writer.Write(freeSummon);
			writer.Write(stunTimer);
		}

		public override void ReceiveExtraAI(BinaryReader reader) {
			life = reader.ReadInt32();
			lifeMax = reader.ReadInt32();
			defense = reader.ReadInt32();
			spawnOrder = reader.ReadInt32();
			freeSummon = reader.ReadBoolean();
			stunTimer = reader.ReadInt32();
			lifeInitialized = true;
		}

		// ---- 静态查询/管理 ----

		public static int CountActiveForPlayer(Player player) {
			int type = ModContent.ProjectileType<CameraTruck>();
			int count = 0;
			for (int i = 0; i < Main.maxProjectiles; i++) {
				Projectile p = Main.projectile[i];
				if (p.active && p.owner == player.whoAmI && p.type == type)
					count++;
			}
			return count;
		}

		// total = 全部摄影车；slotUsing = 占用仆从位的（非免位）摄影车。
		public static void CountForPlayer(Player player, out int total, out int slotUsing) {
			int type = ModContent.ProjectileType<CameraTruck>();
			total = 0;
			slotUsing = 0;
			for (int i = 0; i < Main.maxProjectiles; i++) {
				Projectile p = Main.projectile[i];
				if (!p.active || p.owner != player.whoAmI || p.type != type)
					continue;
				total++;
				if (p.ModProjectile is CameraTruck t && !t.freeSummon)
					slotUsing++;
			}
		}

		// 技能二结束：令该玩家所有摄影车眩晕。
		public static void StunAllForPlayer(Player player, int ticks) {
			int type = ModContent.ProjectileType<CameraTruck>();
			for (int i = 0; i < Main.maxProjectiles; i++) {
				Projectile p = Main.projectile[i];
				if (!p.active || p.owner != player.whoAmI || p.type != type)
					continue;
				if (p.ModProjectile is not CameraTruck t)
					continue;
				if (t.HopTimer > 0f) {
					p.position.X = t.hopStartCenterX - p.width / 2f; // 取消小跳，回到起跳点
					t.HopTimer = 0f;
				}
				t.stunTimer = ticks;
				t.SnapToGround();
				p.netUpdate = true;
			}
		}

		// 满员时移除最早召唤的一辆。
		public static bool TryRemoveOldestForPlayer(Player player) {
			int type = ModContent.ProjectileType<CameraTruck>();
			Projectile oldest = null;
			int oldestOrder = int.MaxValue;

			for (int i = 0; i < Main.maxProjectiles; i++) {
				Projectile p = Main.projectile[i];
				if (!p.active || p.owner != player.whoAmI || p.type != type)
					continue;
				if (p.ModProjectile is not CameraTruck truck)
					continue;
				if (truck.spawnOrder < oldestOrder) {
					oldestOrder = truck.spawnOrder;
					oldest = p;
				}
			}

			if (oldest == null)
				return false;

			(oldest.ModProjectile as CameraTruck)?.DeathEffect();
			oldest.Kill();
			return true;
		}

		public static Vector2 FindGroundSpawnPosition(Vector2 worldPosition, int width, int height) {
			int tileX = (int)(worldPosition.X / 16f);
			int startTileY = (int)(worldPosition.Y / 16f);
			tileX = Utils.Clamp(tileX, 1, Main.maxTilesX - 2);
			startTileY = Utils.Clamp(startTileY, 1, Main.maxTilesY - 2);

			for (int tileY = startTileY; tileY < Main.maxTilesY - 1; tileY++) {
				if (!WorldGen.SolidTile(tileX, tileY))
					continue;

				float spawnCenterX = tileX * 16f + 8f;
				float spawnTopY = tileY * 16f - height;
				return new Vector2(spawnCenterX - width / 2f, spawnTopY);
			}

			return worldPosition;
		}

		private void SnapToGround() {
			int tileX = (int)(Projectile.Center.X / 16f);
			int startTileY = (int)((Projectile.Bottom.Y + 2f) / 16f);
			tileX = Utils.Clamp(tileX, 1, Main.maxTilesX - 2);
			startTileY = Utils.Clamp(startTileY, 1, Main.maxTilesY - 2);

			for (int tileY = startTileY; tileY < Main.maxTilesY; tileY++) {
				if (!WorldGen.SolidTile(tileX, tileY))
					continue;

				Projectile.position.Y = tileY * 16f - Projectile.height;
				return;
			}
		}
	}
}
