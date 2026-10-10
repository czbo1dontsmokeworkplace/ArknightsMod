using ArknightsMod.Content.Dusts;
using ArknightsMod.Content.Dusts.Fire;
using ArknightsMod.Content.Items.Weapons.Guard.Surtr;
using ArknightsMod.Common.VisualEffects;
using ArknightsMod.Players;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Laevatain
{
	/// <summary>
	/// 黄昏期间的傀儡：逐渐显现，左键每 48 帧剁地一次，落地后朝面向方向依次喷出火柱。
	/// </summary>
	public class LaevatainProjectile_3 : ModProjectile
	{
		public override string Texture =>
			"ArknightsMod/Content/Projectiles/Guard/Laevatain/LaevatainProjectile_3_melee";

		private const int FrameCount = 11;
		private const int AttackInterval = 48;
		private const int ImpactFrame = 21;
		private const int RevealTicks = 90;
		private const int PillarCount = 10;
		private int age;
		private int attackTimer;
		private int attackDirection;
		private bool revealBurstPlayed;

		public override void SetStaticDefaults()
		{
			Main.projFrames[Type] = FrameCount;
		}

		public override void SetDefaults()
		{
			Projectile.width = 20;
			Projectile.height = 20;

			Projectile.friendly = false; // 伤害由落地点向前的火柱负责
			Projectile.DamageType = DamageClass.Melee;

			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = 2;
		}

		public override bool ShouldUpdatePosition() => false;

		public override void SendExtraAI(BinaryWriter writer) {
			writer.Write((byte)projMode);
			writer.Write((byte)attackTimer);
			writer.Write((sbyte)attackDirection);
		}

		public override void ReceiveExtraAI(BinaryReader reader) {
			projMode = (ProjMode)reader.ReadByte();
			attackTimer = reader.ReadByte();
			attackDirection = reader.ReadSByte();
		}

		public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) => behindNPCs.Add(index);

		public enum ProjMode {Move,Attack}
		public ProjMode projMode = ProjMode.Move;
		public Player player => Main.player[Projectile.owner];
		public override void AI() {
			if (!player.active || player.dead || player.HeldItem.type != ModContent.ItemType<SurtrLaevatain>() ||
			    player.GetModPlayer<WeaponPlayer>().Skill != 2 || !player.GetModPlayer<WeaponPlayer>().SkillActive)
			{
				Projectile.Kill();
				return;
			}
			Projectile.timeLeft = 2;
			age++;
			Projectile.spriteDirection = projMode == ProjMode.Attack ? attackDirection : player.direction;
			// 傀儡的贴图中心与玩家同高，始终位于玩家身后两格（32 像素）。
			Projectile.Center = player.Center + new Vector2(-32f * Projectile.spriteDirection, 0f);
			if (age < RevealTicks) {
				SpawnRevealFlames();
				return;
			}
			if (!revealBurstPlayed) {
				revealBurstPlayed = true;
				SpawnRevealBurst();
			}

			switch (projMode) {
				case ProjMode.Move:
					Projectile.frame = 0;
					if (Main.myPlayer == Projectile.owner && PlayerInput.MouseInfo.LeftButton == ButtonState.Pressed) {
						attackDirection = player.direction;
						attackTimer = 0;
						Projectile.netUpdate = true;
						projMode = ProjMode.Attack;
					}
					break;
				case ProjMode.Attack:
					attackTimer++;
					Projectile.frame = Math.Min(FrameCount - 1, attackTimer * FrameCount / AttackInterval);
					if (attackTimer == ImpactFrame)
						Attack_Proj();
					if (attackTimer >= AttackInterval) {
						attackTimer = 0;
						bool repeat = Main.myPlayer == Projectile.owner && PlayerInput.MouseInfo.LeftButton == ButtonState.Pressed;
						if (repeat)
							attackDirection = player.direction;
						else
							projMode = ProjMode.Move;
						Projectile.netUpdate = true;
					}
					break;
			}
		}

		private void SpawnRevealFlames() {
			if (Main.dedServ || age % 2 != 0)
				return;
			for (int i = 0; i < 3; i++) {
				Vector2 origin = player.Center + Main.rand.NextVector2Circular(45f, 62f);
				Vector2 velocity = (origin - player.Center).SafeNormalize(-Vector2.UnitY) * Main.rand.NextFloat(1.5f, 4f);
				Dust dust = Dust.NewDustPerfect(origin, DustID.Torch, velocity, 90,
					new Color(255, 76, 12), Main.rand.NextFloat(1.1f, 1.8f));
				dust.noGravity = true;
			}
		}

		private void SpawnRevealBurst() {
			if (Main.dedServ)
				return;
			for (int i = 0; i < 42; i++) {
				Vector2 velocity = (MathHelper.TwoPi * i / 42f).ToRotationVector2() * Main.rand.NextFloat(2.5f, 7f);
				Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Torch, velocity, 55,
					new Color(255, 100, 20), Main.rand.NextFloat(1.25f, 2.1f));
				dust.noGravity = true;
			}
		}

		public override bool PreDraw(ref Color lightColor) {
			Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
			Rectangle source = texture.Frame(1, FrameCount, 0, Projectile.frame);
			Vector2 origin = source.Size() * 0.5f;
			SpriteEffects flip = Projectile.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
			Vector2 position = Projectile.Center - Main.screenPosition;
			float reveal = MathHelper.Clamp(age / (float)RevealTicks, 0f, 1f);
			float outline = MathHelper.Clamp(1f - MathF.Abs(age - RevealTicks) / 15f, 0f, 1f);
			if (outline > 0f) {
				for (int i = 0; i < 8; i++) {
					Vector2 offset = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * (3f + 5f * outline);
					Main.EntitySpriteDraw(texture, position + offset, source,
						new Color(255, 92, 18) * (outline * 0.8f), 0f, origin, Projectile.scale, flip);
				}
			}
			Main.EntitySpriteDraw(texture, position, source, lightColor * reveal, 0f, origin, Projectile.scale, flip);
			return false;
		}

		public void Attack_Proj() {
			if (Main.myPlayer != Projectile.owner)
				return;
			int skillDamage = player.GetWeaponDamage(player.HeldItem);
			Vector2 impactGround = LaevatainTwilightPillar.FindGround(
				player.Center.X + attackDirection * 16f, player.Bottom.Y);
			Projectile.NewProjectile(Projectile.GetSource_FromThis(), impactGround - new Vector2(0f, 28f),
				Vector2.Zero, ModContent.ProjectileType<LaevatainTwilightImpact>(),
				0, 0f, Projectile.owner);
			for (int i = 0; i < PillarCount; i++) {
				float x = player.Center.X + attackDirection * (16f + 48f * i);
				Vector2 ground = LaevatainTwilightPillar.FindGround(x, player.Bottom.Y);
				Projectile.NewProjectile(Projectile.GetSource_FromThis(), ground - new Vector2(0f, LaevatainTwilightPillar.PillarHeight * 0.5f),
					Vector2.Zero, ModContent.ProjectileType<LaevatainTwilightPillar>(),
					skillDamage, Projectile.knockBack, Projectile.owner, i * 3f);
			}
			if (!Main.dedServ) {
				ShakeEffectPlayer shake = player.GetModPlayer<ShakeEffectPlayer>();
				shake.screenShakeTime = Math.Max(shake.screenShakeTime, 10);
				shake.screenShakeMaxDistance = Math.Max(shake.screenShakeMaxDistance, 11f);
				shake.screenShakeVelocity = new Vector2(attackDirection * 6f, -4f);
				Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.65f, Pitch = -0.25f }, player.Center);
				for (int i = 0; i < 20; i++) {
					Dust dust = Dust.NewDustPerfect(player.Bottom + new Vector2(attackDirection * 16f, -4f),
						DustID.Torch, Main.rand.NextVector2Circular(5f, 3f), 70,
						new Color(255, 92, 16), Main.rand.NextFloat(1f, 1.6f));
					dust.noGravity = true;
				}
			}
		}
	}

	/// <summary>
	/// 天降剑：在目标头顶生成，0.3s 内落到目标脚下位置，落地瞬间造成伤害并留下火焰特效。
	/// 下落过程中持续跟随目标当前位置（每帧读取目标的 Bottom），保证任何时刻都在目标正上方；
	/// 落地后不再跟随，固定停留在触地那一刻的位置直至淡出。
	/// </summary>
	public class LaevatainProjectile_3_swordDrop : ModProjectile
	{
		public override string Texture =>
			"ArknightsMod/Content/Items/Weapons/Guard/Surtr/SurtrLaevatain";

		public const int Size = 90; // 碰撞箱边长，生成位置换算要用到
		public const float FallHeight = 220f; // 下落起始高度（目标正上方多高开始落）

		private const int FallTicks = 18; // 0.3s * 60，垂直下落时长
		private const int HitWindowTicks = 4; // 落地瞬间的伤害判定窗口
		private const int FadeTicks = 18; // 0.3s * 60，落地命中后停留、期间透明度逐渐降低
		// 贴图默认朝向是"剑柄左下-剑尖右上"，下落时想让剑尖朝下，经验旋转值，如果朝向不对可以调这个
		// 在原有 45° 基础上再顺时针转 90°（贴图坐标系里正值就是顺时针）
		private const float FallRotationOffset = MathHelper.PiOver4 + MathHelper.PiOver2;

		private Vector2 lastKnownBottom; // 目标最近一次的脚下位置，目标失效后作为兜底
		private bool impactSpawned;

		// ── 金色拖尾参数：下落时剑身后拉出一条金光 ──
		private const float TrailMaxLength = 140f; // 拖尾最大长度
		private const float TrailWidth = 14f;      // 拖尾宽度

		public override void SetDefaults()
		{
			Projectile.width = Size;
			Projectile.height = Size;

			Projectile.friendly = false; // 下落途中不判定，落地才开启
			Projectile.DamageType = DamageClass.Melee;

			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = FallTicks + FadeTicks;

			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 20;
		}

		public override bool ShouldUpdatePosition() => false;

		public override void OnSpawn(IEntitySource source)
		{
			NPC target = GetTarget();
			lastKnownBottom = target?.Bottom ?? Projectile.Center;
		}

		private NPC GetTarget()
		{
			int index = (int)Projectile.ai[0];
			if (index < 0 || index >= Main.maxNPCs)
				return null;

			NPC npc = Main.npc[index];
			return npc.active && npc.life > 0 ? npc : null;
		}

		public override void AI()
		{
			NPC target = GetTarget();
			if (target != null)
				lastKnownBottom = target.Bottom; // 每帧刷新，跟随目标移动

			int elapsed = FallTicks + FadeTicks - Projectile.timeLeft;

			if (elapsed < FallTicks)
			{
				float t = elapsed / (float)FallTicks;
				float easedT = t * t; // 越落越快，模拟重力加速
				float altitude = FallHeight * (1f - easedT); // 离目标脚下还有多高
				Projectile.Center = lastKnownBottom - new Vector2(0f, altitude); // 始终在目标正上方
				Projectile.rotation = FallRotationOffset;

				// 金色拖尾点缀：尘埃留在下落轨迹上、微微上飘，形成剑身后的拖尾
				if (Main.rand.NextBool(2))
				{
					Dust d = Dust.NewDustPerfect(
						Projectile.Center + Main.rand.NextVector2Circular(8f, 20f),
						DustID.Torch,
						-Vector2.UnitY * Main.rand.NextFloat(0.5f, 1.5f),
						120,
						Color.Gold,
						Main.rand.NextFloat(0.9f, 1.4f)
					);
					d.noGravity = true;
				}
			}
			else
			{
				Projectile.Center = lastKnownBottom; // 落地后固定，不再跟随
				int sinceLanding = elapsed - FallTicks;
				Projectile.friendly = sinceLanding < HitWindowTicks; // 落地瞬间短暂判定，随后只停留淡出

				if (!impactSpawned)
				{
					impactSpawned = true;
					Projectile.netUpdate = true;

					SpawnLandingSparks(Projectile.Center, FallHeight);

					// if (Main.myPlayer == Projectile.owner)
					// {
					// 	Projectile.NewProjectile(
					// 		Projectile.GetSource_FromThis(),
					// 		Projectile.Center,
					// 		Vector2.Zero,
					// 		ModContent.ProjectileType<LaevatainProjectile_3_impactFire>(),
					// 		0,
					// 		0f,
					// 		Projectile.owner
					// 	);
					// }
				}
			}
		}

		// Terraria 普通 Dust 每帧的重力加速度（noGravity=false 时）
		private const float DustGravity = 0.1f;

		private static void SpawnLandingSparks(Vector2 landPos, float fallDistance)
		{
			float peakHeight = fallDistance / 2f; // 蹦起的最高点，取下落高度的一半
			float launchSpeed = (float)Math.Sqrt(2f * DustGravity * peakHeight);

			for (int i = 0; i < 10; i++)
			{
				Vector2 velocity = new(
					Main.rand.NextFloat(-2f, 2f),
					-launchSpeed * Main.rand.NextFloat(0.7f, 1f)
				);

				// 红黄为主，绿色只作为少量点缀
				float roll = Main.rand.NextFloat();
				Color sparkColor = roll < 0.45f ? Color.Red
					: roll < 0.85f ? Color.Gold
					: Color.LimeGreen;

				Dust d = Dust.NewDustPerfect(
					landPos,
					DustID.Torch,
					velocity,
					100,
					sparkColor,
					Main.rand.NextFloat(1.0f, 1.6f)
				);
				d.noGravity = false; // 蹦起后自由下落
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			if (Projectile.timeLeft <= 0)
				return false;

			int elapsed = FallTicks + FadeTicks - Projectile.timeLeft;

			// 金色拖尾：仅下落阶段画，剑身沿下落方向上方拉出一条金光
			if (elapsed < FallTicks)
				DrawFallTrail(elapsed);

			float alpha = 1f;
			if (elapsed >= FallTicks)
			{
				int sinceLanding = elapsed - FallTicks;
				float fadeProgress = MathHelper.Clamp(sinceLanding / (float)FadeTicks, 0f, 1f);
				alpha = MathHelper.Lerp(1f, 0.5f, fadeProgress);
			}

			Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
			Main.spriteBatch.Draw(
				tex,
				Projectile.Center - Main.screenPosition,
				null,
				Color.White * alpha,
				Projectile.rotation,
				tex.Size() / 2f,
				Projectile.scale,
				SpriteEffects.None,
				0f
			);
			return false;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			Dust.NewDust(target.Center, 0, 0, ModContent.DustType<surtrDamage_Dust>(),Scale:1f);
			if(Main.rand.NextBool(2))
				Dust.NewDust(target.Center, 0, 0, ModContent.DustType<fire_28>(),Scale:1f);
			else {
				Dust.NewDust(target.Center, 0, 0, ModContent.DustType<fire_03>(),Scale:1f);
			}
		}

		// 金色拖尾：贴图从剑身向上拉伸绘制，加法混合发光
		private void DrawFallTrail(int elapsed)
		{
			float t = MathHelper.Clamp(elapsed / (float)FallTicks, 0f, 1f);
			float easedT = t * t; // 与下落的缓动一致：落得越快拖尾拉得越长
			float trailLen = Math.Min(FallHeight * easedT, TrailMaxLength);
			if (trailLen < 4f)
				return;

			Texture2D tex = ModContent.Request<Texture2D>("ArknightsMod/Content/Textures/duaog/wbjex8").Value;

			// 切到加法混合，画完换回 AlphaBlend，不影响后面的剑本体绘制
			Main.spriteBatch.End();
			Main.spriteBatch.Begin(
				SpriteSortMode.Deferred,
				BlendState.Additive,
				Main.DefaultSamplerState,
				DepthStencilState.None,
				RasterizerState.CullNone,
				null,
				Main.GameViewMatrix.TransformationMatrix
			);

			// 以贴图左缘中点为原点，旋转 -90° 使其竖直向上延伸（剑往下落，拖尾在剑上方）
			Main.spriteBatch.Draw(
				tex,
				Projectile.Center - Main.screenPosition,
				null,
				Color.Gold * 0.8f,
				-MathHelper.PiOver2,
				new Vector2(0f, tex.Height / 2f),
				new Vector2(trailLen / tex.Width, TrailWidth / tex.Height),
				SpriteEffects.None,
				0f
			);

			Main.spriteBatch.End();
			Main.spriteBatch.Begin(
				SpriteSortMode.Deferred,
				BlendState.AlphaBlend,
				Main.DefaultSamplerState,
				DepthStencilState.None,
				RasterizerState.CullNone,
				null,
				Main.GameViewMatrix.TransformationMatrix
			);
		}
	}

	/// <summary>
	/// 天降剑落地时留下的火焰视觉效果，纯装饰，不判定伤害。
	/// </summary>
	public class LaevatainProjectile_3_impactFire : ModProjectile
	{
		public override string Texture =>
			"ArknightsMod/Content/Projectiles/Guard/Laevatain/LaevatainProjectile_3_fire";

		private const int Lifetime = 30;

		public override void SetDefaults()
		{
			Projectile.width = 10;
			Projectile.height = 10;

			Projectile.friendly = false;
			Projectile.hostile = false;
			Projectile.DamageType = DamageClass.Melee;

			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = Lifetime;
		}

		public override bool ShouldUpdatePosition() => false;

		public override void AI()
		{
			Lighting.AddLight(Projectile.Center, 1.0f, 0.5f, 0.2f);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
			float progress = 1f - Projectile.timeLeft / (float)Lifetime;
			float scale = MathHelper.Lerp(0.6f, 0.9f, progress);

			// 贴图是直通(非预乘)透明度，默认 AlphaBlend 按预乘透明度处理会把半透明区域画得过浓，这里换成 NonPremultiplied
			Main.spriteBatch.End();
			Main.spriteBatch.Begin(
				SpriteSortMode.Deferred,
				BlendState.NonPremultiplied,
				Main.DefaultSamplerState,
				DepthStencilState.None,
				RasterizerState.CullNone,
				null,
				Main.GameViewMatrix.TransformationMatrix
			);

			Main.spriteBatch.Draw(
				tex,
				Projectile.Center - Main.screenPosition,
				null,
				Color.White, // 原图输出，不叠加染色/淡出
				0f,
				tex.Size() / 2f,
				scale,
				SpriteEffects.None,
				0f
			);

			Main.spriteBatch.End();
			Main.spriteBatch.Begin(
				SpriteSortMode.Deferred,
				BlendState.AlphaBlend,
				Main.DefaultSamplerState,
				DepthStencilState.None,
				RasterizerState.CullNone,
				null,
				Main.GameViewMatrix.TransformationMatrix
			);
			return false;
		}
	}
}
