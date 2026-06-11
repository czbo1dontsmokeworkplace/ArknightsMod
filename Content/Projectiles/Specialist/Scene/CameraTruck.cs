using System;
using System.IO;
using ArknightsMod.Content.Buffs.Specialist.Scene;
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

		// 攻击（小跳）
		private const float AttackRange = 150f;
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

		private ref float AnimTimer => ref Projectile.ai[0];
		private ref float HopTimer => ref Projectile.ai[1];
		private ref float CooldownTimer => ref Projectile.ai[2];

		private int hopDir = 1;
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
			Projectile.minionSlots = 1f;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.aiStyle = -1;
			Projectile.timeLeft = 2;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = HopDuration;
		}

		public override void OnSpawn(IEntitySource source) {
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

		// 仅小跳扑击期间造成接触伤害。
		public override bool MinionContactDamage() => IsHopping;

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

			if (IsHopping) {
				UpdateHop();
			}
			else {
				SnapToGround();
				if (CooldownTimer > 0f)
					CooldownTimer--;
				else if (TryFindTarget(owner, out int dir))
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
			dir = 1;
			Vector2 center = Projectile.Center;
			float rangeSq = AttackRange * AttackRange;
			NPC best = null;
			float bestDistSq = rangeSq;

			if (owner.HasMinionAttackTargetNPC) {
				NPC marked = Main.npc[owner.MinionAttackTargetNPC];
				if (marked.active && marked.CanBeChasedBy(Projectile)
					&& Vector2.DistanceSquared(marked.Center, center) <= rangeSq)
					best = marked;
			}

			if (best == null) {
				for (int i = 0; i < Main.maxNPCs; i++) {
					NPC npc = Main.npc[i];
					if (!npc.active || !npc.CanBeChasedBy(Projectile))
						continue;
					float dSq = Vector2.DistanceSquared(npc.Center, center);
					if (dSq < bestDistSq) {
						bestDistSq = dSq;
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

			foreach (NPC npc in Main.ActiveNPCs) {
				if (!npc.active || npc.friendly || npc.damage <= 0 || npc.dontTakeDamage)
					continue;
				if (!Projectile.Hitbox.Intersects(npc.Hitbox))
					continue;
				return ApplyDamage(npc.damage);
			}

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
			int dealt = Math.Max(1, rawDamage - defense);
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
			Texture2D bodyTex = TextureAssets.Projectile[Type].Value;
			Texture2D headTex = ModContent.Request<Texture2D>(
				"ArknightsMod/Content/Projectiles/Specialist/Scene/CameraTruck_Head").Value;

			float bodyBob = IsHopping ? 0f : (1f - (float)Math.Cos(AnimTimer * BodyBobSpeed)) * 0.5f * BodyBobAmplitude;
			float lean = IsHopping ? LeanMax * (float)Math.Sin(MathHelper.Pi * MathHelper.Clamp(HopTimer / HopDuration, 0f, 1f)) * hopDir : 0f;
			float idleSep = ((float)Math.Sin(AnimTimer * HeadBobSpeed) * 0.5f + 0.5f) * HeadBodyMaxGap;

			Vector2 footScreen = new Vector2(Projectile.Center.X, Projectile.Bottom.Y) - Main.screenPosition
				+ new Vector2(0f, Projectile.gfxOffY);

			Vector2 bodyOrigin = new Vector2(bodyTex.Width / 2f, bodyTex.Height);
			Vector2 pivot = footScreen + new Vector2(0f, bodyBob);
			Main.EntitySpriteDraw(bodyTex, pivot, null, lightColor, lean, bodyOrigin, DrawScale, SpriteEffects.None, 0);

			float effectiveGap = Math.Min(idleSep - headJoltOffset, HeadBodyMaxGap);
			Vector2 headLocalUp = new Vector2(0f, -(bodyTex.Height * DrawScale) - effectiveGap);
			Vector2 headPos = pivot + headLocalUp.RotatedBy(lean);
			Vector2 headOrigin = new Vector2(headTex.Width / 2f, headTex.Height);
			Main.EntitySpriteDraw(headTex, headPos, null, lightColor, lean, headOrigin, DrawScale, SpriteEffects.None, 0);

			return false;
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
		}

		public override void ReceiveExtraAI(BinaryReader reader) {
			life = reader.ReadInt32();
			lifeMax = reader.ReadInt32();
			defense = reader.ReadInt32();
			spawnOrder = reader.ReadInt32();
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
