using System;
using System.Collections.Generic;
using ArknightsMod.Common.GlobalProjectiles;
using ArknightsMod.Content.Buffs.Summoner;
using ArknightsMod.Content.Items.Weapons.Summoner;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Summoner
{
	// 塔防式固定召唤物
	public class DeepcolorMinion : ModProjectile
	{
		public const int MaxTentacles = 4;
		public const int FrameWidth = 40;
		public const int FrameHeight = 40;
		public const int GroundOverlapPixels = 2;
		public const int TotalFrameCount = 21;
		public const int AttackFrameStart = 0;
		public const int AttackFrameCount = 11;
		public const int IdleFrameStart = 11;
		public const int IdleFrameCount = 10;

		private const int AttackTicksPerFrame = 3;
		private const int IdleTicksPerFrame = 6;
		// 半圆弧索敌基础半径；二技能再 +4 格
		public const float BaseAttackRangeRadiusPx = 48f;
		private const int AttackCooldownMax = 45;
		private const int AttackDrawExtendRight = 12;
		private const int AttackDrawCullingPadding = 192;

		public override string Texture => $"{nameof(ArknightsMod)}/Content/Items/Weapons/Summoner/DeepcolorMinion";
		private ref float IdleAnimTimer => ref Projectile.ai[0];
		private ref float AttackAnimTimer => ref Projectile.ai[1];
		private ref float AttackCooldownTimer => ref Projectile.ai[2];
		private bool IsAttacking => AttackAnimTimer > 0f;

		public override void SetStaticDefaults() {
			Main.projFrames[Type] = TotalFrameCount;
			ProjectileID.Sets.MinionSacrificable[Type] = true;
			ProjectileID.Sets.MinionTargetingFeature[Type] = true;
			ProjectileID.Sets.DrawScreenCheckFluff[Type] = AttackDrawCullingPadding;
		}

		public override void SetDefaults() {
			Projectile.width = FrameWidth;
			Projectile.height = FrameHeight;
			Projectile.friendly = true;
			Projectile.minion = true;
			Projectile.DamageType = DamageClass.Summon;
			Projectile.minionSlots = 0f;
			Projectile.penetrate = -1;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = false;
			Projectile.aiStyle = -1;
		}

		public override void OnSpawn(IEntitySource source) {
			Projectile.velocity = Vector2.Zero;
			SnapToGround();
			Projectile.tileCollide = false;

			// 每只触手独立动画相位，避免同时同帧
			IdleAnimTimer = Main.rand.Next(IdleFrameCount * IdleTicksPerFrame);
			AttackAnimTimer = 0f;
			AttackCooldownTimer = Main.rand.Next(AttackCooldownMax / 2);
			Projectile.frame = IdleFrameStart + ((int)IdleAnimTimer / IdleTicksPerFrame) % IdleFrameCount;

			var life = Projectile.MinionLife();
			life.useLife = true;
			life.lifeMax = DeepcolorMinionLifeGlobalProj.DefaultLifeMax;
			life.life = life.lifeMax;
			life.defense = DeepcolorMinionLifeGlobalProj.DefaultDefense;
			life.drawHealthBar = true;
		}

		public static int CountActiveForPlayer(Player player) {
			int type = ModContent.ProjectileType<DeepcolorMinion>();
			int count = 0;
			for (int i = 0; i < Main.maxProjectiles; i++) {
				Projectile proj = Main.projectile[i];
				if (proj.active && proj.owner == player.whoAmI && proj.type == type)
					count++;
			}
			return count;
		}

		// 满员时移除该玩家最早召唤的一只触手
		public static bool TryDespawnOldestForPlayer(Player player) {
			int type = ModContent.ProjectileType<DeepcolorMinion>();
			Projectile oldest = null;
			int oldestOrder = int.MaxValue;

			for (int i = 0; i < Main.maxProjectiles; i++) {
				Projectile proj = Main.projectile[i];
				if (!proj.active || proj.owner != player.whoAmI || proj.type != type)
					continue;

				int order = proj.MinionLife().spawnOrder;
				if (order >= oldestOrder)
					continue;

				oldestOrder = order;
				oldest = proj;
			}

			if (oldest == null)
				return false;

			DespawnWithDeathEffect(oldest);
			return true;
		}

		public static void DespawnWithDeathEffect(Projectile projectile) {
			if (!projectile.active || projectile.type != ModContent.ProjectileType<DeepcolorMinion>())
				return;

			SpawnDeathParticles(projectile);
			SoundEngine.PlaySound(SoundID.NPCDeath4, projectile.Center);
			projectile.Kill();
		}

		public override bool? CanCutTiles() => false;

		public override bool MinionContactDamage() {
			if (!IsAttacking || !HasTargetInAttackRange())
				return false;

			if (!Projectile.CanDealContactDamage())
				return false;

			return Projectile.frame is >= AttackFrameStart + 4 and <= AttackFrameStart + 7;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			Projectile.SetDealContactCooldown();
			// 与召唤主共用免疫槽，避免贴脸时同帧多次结算
			target.immune[Projectile.owner] = DeepcolorMinionLifeGlobalProj.DealContactDamageCooldownMax;
		}

		public override void AI() {
			Player owner = Main.player[Projectile.owner];
			if (!CheckActive(owner)) {
				Projectile.Kill();
				return;
			}

			Projectile.velocity = Vector2.Zero;
			SnapToGround();

			bool targetInRange = TryGetClosestTarget(owner, out Vector2 targetCenter);

			if (targetInRange && AttackCooldownTimer <= 0f && !IsAttacking)
				StartAttack();

			if (IsAttacking)
				UpdateAttackAnimation();
			else
				UpdateIdleAnimation();

			if (AttackCooldownTimer > 0f)
				AttackCooldownTimer--;

			if (targetInRange)
				UpdateSpriteDirection(targetCenter);
		}

		private static bool CheckActive(Player owner) {
			if (owner.dead || !owner.active)
				return false;
			return owner.HasBuff<DeepcolorMinionBuff>();
		}

		// 左—上—右半圆弧内索敌，不攻击正下方
		public bool IsInAttackRange(NPC npc) {
			if (!npc.CanBeChasedBy(Projectile))
				return false;

			Player owner = Main.player[Projectile.owner];
			return DeepcolorSketchSkills.IsInAttackRangeAt(Projectile, npc.Center, owner);
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
			Player owner = Main.player[Projectile.owner];
			if (DeepcolorSketchSkills.ShadowTentacleActive(owner))
				modifiers.SourceDamage.Base *= DeepcolorSketchSkills.ShadowTentacleDamageMult;
		}

		private bool HasTargetInAttackRange() {
			Player owner = Main.player[Projectile.owner];

			if (owner.HasMinionAttackTargetNPC && owner.MinionAttackTargetNPC >= 0 && owner.MinionAttackTargetNPC < Main.maxNPCs) {
				NPC npc = Main.npc[owner.MinionAttackTargetNPC];
				if (npc.active && IsInAttackRange(npc))
					return true;
			}

			for (int i = 0; i < Main.maxNPCs; i++) {
				NPC npc = Main.npc[i];
				if (npc.active && IsInAttackRange(npc))
					return true;
			}

			return false;
		}

		private bool TryGetClosestTarget(Player owner, out Vector2 targetCenter) {
			targetCenter = Projectile.Center;
			bool found = false;
			float bestDist = 0f;

			if (owner.HasMinionAttackTargetNPC && owner.MinionAttackTargetNPC >= 0 && owner.MinionAttackTargetNPC < Main.maxNPCs)
				ConsiderTarget(Main.npc[owner.MinionAttackTargetNPC], ref found, ref bestDist, ref targetCenter);

			for (int i = 0; i < Main.maxNPCs; i++)
				ConsiderTarget(Main.npc[i], ref found, ref bestDist, ref targetCenter);

			return found;
		}

		private void ConsiderTarget(NPC npc, ref bool found, ref float bestDist, ref Vector2 targetCenter) {
			if (!npc.active || !IsInAttackRange(npc))
				return;

			float dist = Vector2.Distance(npc.Center, Projectile.Center);
			if (!found || dist < bestDist) {
				found = true;
				bestDist = dist;
				targetCenter = npc.Center;
			}
		}

		private void StartAttack() {
			AttackAnimTimer = 1f;
			Projectile.frame = AttackFrameStart;
		}

		private void UpdateAttackAnimation() {
			AttackAnimTimer++;
			if (AttackAnimTimer % AttackTicksPerFrame != 0)
				return;

			Projectile.frame++;
			if (Projectile.frame >= AttackFrameStart + AttackFrameCount) {
				AttackAnimTimer = 0f;
				AttackCooldownTimer = AttackCooldownMax;
				Projectile.frame = IdleFrameStart + ((int)IdleAnimTimer / IdleTicksPerFrame) % IdleFrameCount;
			}
		}

		private void UpdateIdleAnimation() {
			IdleAnimTimer++;
			if (IdleAnimTimer % IdleTicksPerFrame != 0)
				return;

			int idleIndex = ((int)IdleAnimTimer / IdleTicksPerFrame) % IdleFrameCount;
			Projectile.frame = IdleFrameStart + idleIndex;
		}

		private void UpdateSpriteDirection(Vector2 lookAt) {
			Projectile.spriteDirection = lookAt.X >= Projectile.Center.X ? 1 : -1;
		}

		private static bool IsAttackFrame(int frame) => frame >= AttackFrameStart && frame < AttackFrameStart + AttackFrameCount;

		public float VisualFootWorldY => Projectile.Bottom.Y + GroundOverlapPixels;

		public override bool PreDraw(Player player, ref Color lightColor) {
			Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
			Rectangle source = new(0, Projectile.frame * FrameHeight, FrameWidth, FrameHeight);
			SpriteEffects effects = SpriteEffects.None;
			float footY = VisualFootWorldY + Projectile.gfxOffY;
			Vector2 origin;
			Vector2 drawPos;
			Player owner = Main.player[Projectile.owner];
			float drawScale = DeepcolorSketchSkills.VisualTrapActive(owner) ? DeepcolorSketchSkills.VisualTrapDrawScale : 1f;

			// 攻击贴图向右伸出：用脚底左锚点绘制，避免中心锚点裁切右侧；向左攻击仍水平翻转
			if (IsAttacking) {
				if (Projectile.spriteDirection > 0) {
					int extraWidth = Math.Min(AttackDrawExtendRight, Math.Max(0, texture.Width - FrameWidth));
					if (extraWidth > 0)
						source.Width = FrameWidth + extraWidth;

					origin = new Vector2(0f, FrameHeight);
					drawPos = new Vector2(Projectile.Left.X - AttackDrawExtendRight * 0.5f, footY) - Main.screenPosition;
				}
				else {
					effects = SpriteEffects.FlipHorizontally;
					origin = new Vector2(FrameWidth, FrameHeight);
					drawPos = new Vector2(Projectile.Right.X + AttackDrawExtendRight * 0.5f, footY) - Main.screenPosition;
				}
			}
			else {
				origin = new Vector2(FrameWidth * 0.5f, FrameHeight);
				drawPos = new Vector2(Projectile.Center.X, footY) - Main.screenPosition;
			}

			Main.EntitySpriteDraw(texture, drawPos, source, lightColor, Projectile.rotation, origin, drawScale, effects, 0);
			return false;
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

		public static Vector2 FindGroundSpawnPosition(Vector2 worldPosition, int width, int height) {
			int tileX = (int)(worldPosition.X / 16f);
			int startTileY = (int)(worldPosition.Y / 16f);
			tileX = Utils.Clamp(tileX, 1, Main.maxTilesX - 2);
			startTileY = Utils.Clamp(startTileY, 1, Main.maxTilesY - 2);

			for (int tileY = startTileY; tileY < Main.maxTilesY - 1; tileY++) {
				Tile tile = Main.tile[tileX, tileY];
				Tile tileAbove = tileY > 0 ? Main.tile[tileX, tileY - 1] : tile;

				if (tile.HasUnactuatedTile && Main.tileSolid[tile.TileType] && (!tileAbove.HasUnactuatedTile || !Main.tileSolid[tileAbove.TileType])) {
					float spawnX = tileX * 16f + 8f;
					float spawnY = tileY * 16f - height;
					return new Vector2(spawnX, spawnY);
				}
			}

			for (int tileY = startTileY; tileY < Main.maxTilesY; tileY++) {
				if (WorldGen.SolidTile(tileX, tileY)) {
					float spawnX = tileX * 16f + 8f;
					float spawnY = tileY * 16f - height;
					return new Vector2(spawnX, spawnY);
				}
			}

			return worldPosition;
		}

		public static void SpawnDeathParticles(Projectile projectile) {
			if (Main.netMode == NetmodeID.Server)
				return;

			Texture2D texture;
			try {
				texture = ModContent.Request<Texture2D>(projectile.ModProjectile.Texture, AssetRequestMode.ImmediateLoad).Value;
			}
			catch {
				return;
			}

			int frame = projectile.frame;
			Rectangle frameRect = new(0, frame * FrameHeight, FrameWidth, FrameHeight);
			Color[] pixels = new Color[FrameWidth * FrameHeight];
			try {
				texture.GetData(0, frameRect, pixels, 0, pixels.Length);
			}
			catch {
				return;
			}

			List<Color> palette = [];
			for (int attempt = 0; attempt < 120 && palette.Count < 16; attempt++) {
				int x = Main.rand.Next(FrameWidth);
				int y = Main.rand.Next(FrameHeight);
				Color sample = pixels[y * FrameWidth + x];
				if (sample.A <= 40)
					continue;

				bool exists = false;
				foreach (Color c in palette) {
					if (c == sample) {
						exists = true;
						break;
					}
				}

				if (!exists)
					palette.Add(sample);
			}

			if (palette.Count == 0)
				return;

			Vector2 center = projectile.Center;
			for (int i = 0; i < 24; i++) {
				Color dustColor = palette[Main.rand.Next(palette.Count)];
				Vector2 offset = Main.rand.NextVector2Circular(10f, 10f);
				Dust dust = Dust.NewDustPerfect(center + offset, DustID.Smoke, Scale: Main.rand.NextFloat(0.9f, 1.5f));
				dust.color = dustColor;
				dust.noGravity = true;
				dust.fadeIn = 1.1f;
				dust.velocity = offset.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(1.2f, 3.2f);
				if (dust.velocity == Vector2.Zero)
					dust.velocity = Main.rand.NextVector2Circular(2.5f, 2.5f);
			}

			for (int i = 0; i < 8; i++) {
				Color dustColor = palette[Main.rand.Next(palette.Count)];
				Dust dust = Dust.NewDustPerfect(center, DustID.Cloud, Scale: Main.rand.NextFloat(0.7f, 1.1f));
				dust.color = dustColor;
				dust.noGravity = true;
				dust.velocity = Main.rand.NextVector2Circular(2f, 2f);
			}
		}
	}
}
