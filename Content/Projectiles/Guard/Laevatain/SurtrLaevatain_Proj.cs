using ArknightsMod.Common.Particle.T;
using ArknightsMod.Content.Dusts;
using ArknightsMod.Content.Dusts.Fire;
using ArknightsMod.Content.Items.Weapons.Guard.Surtr;
using ArknightsMod.Common.VisualEffects;
using ArknightsMod.Content.Projectiles.Guard.ArtsFighter;
using ArknightsMod.Content.Projectiles.Guard.Mousse;
using System.IO;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Net.Mail;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using HeatWaveRTEffect = ArknightsMod.Content.SwingHelper.HeatWaveRTEffect;
using RTHelper = ArknightsMod.Content.SwingHelper.RTHelper;
using RotationHelper = ArknightsMod.Content.SwingHelper.RotationHelper;

namespace ArknightsMod.Content.Projectiles.Guard.Laevatain
{
	//TODO : 根据技能变化武器攻击形态 常态为顺劈 技能1 稍微倾斜的挥砍 但是长度更长 技能2 攻击模式修改为戳刺 为了和原版单纯的数量区分开来
	public class SurtrLaevatain_Proj : ModProjectile
	{
		public const int StabDuration = 26;
		public Player player => Main.player[Projectile.owner];
		// public Texture2D tailTex => ModContent.Request<Texture2D>("ArknightsMod/Content/Projectiles/Guard/Leavatain/surtr_01").Value;
		public Laevatain_SwingHelper helper;
		private readonly LaevatainStabEffect stabEffect = new()
		{
			WindShape = LaevatainStabWindShape.Parallel,
			WindAngleOffsetRadians = MathHelper.ToRadians(-15f),
			ParallelSpacing = 18f * Laevatain_SwingHelper.StabAreaScale,
			WindColor = new Color(255, 90, 24),
			WindPositionAlongBlade = 0.65f,
			WindFlowSpeed = 80f
		};
		public override void SetDefaults() {
			Projectile.width = 10;
			Projectile.height = 10;
			Projectile.friendly = true;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.ownerHitCheck = true;
			Projectile.DamageType = DamageClass.MeleeNoSpeed;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = 60;
			Projectile.aiStyle = -1;
			Projectile.localNPCHitCooldown = 12;
		}

		public override void OnSpawn(IEntitySource source) {
			helper = new Laevatain_SwingHelper(16, 2);
			helper.SetPlayer(player)
				.SetProj(Projectile)
				.SetTex(TextureAssets.Projectile[Projectile.type].Value)
				.SetSwingRad(MathHelper.ToRadians(270));
			helper.SetSetoff(new Vector2(-25, 0));
			helper.PivotOnPlayerCenter = true;
			helper.setoff = Vector2.Zero;
			helper.SetHandlePos(new Vector2(0, 10));
			helper.SetSwordPos(new Vector2(70, 10));
			helper.SetScale(new Vector2(1f,1f));
			helper.lagTime = 8;
			helper.MaxChargetime = 12;
			helper.TotalStabDuration = StabDuration;
			helper.SetScale(1f);
			helper.SwingUseTime = 13;
		}

		public override bool ShouldUpdatePosition() => false;

		public enum ProjMode {Move,Attack,Wait,Stab}
		public ProjMode projMode = ProjMode.Move;
		public WeaponPlayer mp => player.GetModPlayer<WeaponPlayer>();
		private bool hitFeedbackPlayed;
		private int swingFlamesSpawned;
		private const int MaxSwingFlames = 15;
		private readonly HashSet<int> stabHitTargets = new();
		private int comboFacing = 1;
        private float normalAngle;
        private float normalPower;
        private int normalUnit = ArtsFighterSwing.BaseUseTime;
        private bool normalDamageWindow;
        private int normalDirection = -1;
        private float NormalTimer { get => Projectile.ai[0]; set => Projectile.ai[0] = value; }
        private float NormalCount { get => Projectile.ai[1]; set => Projectile.ai[1] = value; }
		private const float NormalSwingSize = 2f;

		private int AimFacing() => Projectile.velocity.X < 0f ? -1 : 1;

		private void BeginAttack()
		{
			hitFeedbackPlayed = false;
			swingFlamesSpawned = 0;
			stabHitTargets.Clear();
			stabEffect.Reset();
			projMode = ProjMode.Attack;
			// 常驻剑弹幕不会重生；每次挥砍都要重新读取当前技能的武器伤害。
			Projectile.damage = player.GetWeaponDamage(player.HeldItem);
			Projectile.originalDamage = Projectile.damage;
			float aimAngle = Projectile.velocity.ToRotation();
            comboFacing = AimFacing();
            player.direction = comboFacing;
            helper.PointMouseRad(aimAngle);
            helper.SetStabRad(aimAngle);
            helper.ResetTime();
			helper.ReloadIndex();
			helper.SetScale(new Vector2(1f, 1f));
			helper.SetScale(NormalSwingSize * (mp.Skill == 0 && mp.SkillActive ? 1.2f : 1f));
			if (mp.Skill == 0) {
				if (mp.SkillCharge >= mp.SkillChargeMax) {
					helper.SetScale(new Vector2(1f, 0.856f));
					helper.SetScale(NormalSwingSize * (mp.SkillActive ? 1.4f : 1.3f));
					mp.SkillCharge = 0;
					SoundEngine.PlaySound(SurtrLaevatain.SkillActiveSound, player.Center);
				}
				else
					mp.SkillCharge++;
			}
			else if (mp.Skill == 1 && mp.SkillActive) {
				helper.SetScale(1.2f);
				projMode = ProjMode.Stab;
			}
			helper.PivotOnPlayerCenter = projMode != ProjMode.Stab;
			if (helper.PivotOnPlayerCenter)
				helper.setoff = Vector2.Zero;
			Projectile.netUpdate = true;
		}

        private void StartNormalCombo()
        {
            normalUnit = Math.Max(2, (int)MathF.Round(ArtsFighterSwing.BaseUseTime / player.GetAttackSpeed(DamageClass.Melee)));
            NormalTimer = -normalUnit;
            NormalCount = 0;
            normalAngle = MathHelper.Pi;
            normalPower = 0f;
            normalDamageWindow = false;
            helper.PivotOnPlayerCenter = true;
            helper.SetScale(Vector2.One);
            helper.SetScale(NormalSwingSize);
            helper.setoff = Vector2.Zero;
            helper.ReloadIndex();
            projMode = ProjMode.Wait;
            Projectile.netUpdate = true;
        }

        private void UpdateNormalCombo()
        {
            if (NormalTimer < 0f && Main.myPlayer == Projectile.owner &&
                PlayerInput.MouseInfo.LeftButton != ButtonState.Pressed)
            {
                projMode = ProjMode.Move;
                normalDamageWindow = false;
                helper.SetScale(1f);
                helper.ReloadIndex();
                Projectile.netUpdate = true;
                return;
            }
            float before = NormalTimer;
            bool release = ArtsFighterSwing.Step(ref Projectile.ai[0], ref Projectile.ai[1], normalUnit,
                ref normalAngle, ref normalPower);
            normalDamageWindow = before >= 0f && before < normalUnit;
            if (before < 0f && NormalTimer == 0f && Main.myPlayer == Projectile.owner)
            {
                BeginAttack();
                if (projMode == ProjMode.Stab) return;
                Array.Clear(Projectile.localNPCImmunity);
            }
            projMode = normalDamageWindow ? ProjMode.Attack : ProjMode.Wait;
            comboFacing = AimFacing();
            player.direction = comboFacing;
            float aim = Projectile.velocity.ToRotation();
            normalDirection = (NormalCount % 2f != 0f ? -1 : 1) * comboFacing;
            float start = aim - .75f * MathHelper.Pi * normalDirection;
            helper.PivotOnPlayerCenter = true;
            helper.setoff = Vector2.Zero;
            helper.PointMouseRad(aim);
            helper.swingTime = normalDamageWindow ? (int)before + 1 : 0;
            helper.ApplyArtsSwingPose(aim + normalAngle * comboFacing, start,
                normalDirection * comboFacing, normalDamageWindow);
            if (normalDamageWindow) SpawnSwingFire();
            if (release && Main.myPlayer == Projectile.owner)
            {
                Vector2 axis = Projectile.velocity.SafeNormalize(Vector2.UnitX * comboFacing);
                float visualScale = MathHelper.Clamp(Vector2.Distance(helper.handlePos, helper.swordHead) / 168.75f, .5f, 3f);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), helper.handlePos + axis * 25f * visualScale,
                    axis * 31.25f * visualScale, ModContent.ProjectileType<LaevatainArtsSlash>(),
                    0, 0f, Projectile.owner, NormalCount, visualScale);
            }
            if (release && !Main.dedServ)
                SoundEngine.PlaySound(SoundID.Item1 with { Volume = .42f,
                    Pitch = NormalCount == 2f ? -.12f : 0f, MaxInstances = 3 }, player.Center);
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)projMode);
            writer.Write(normalUnit);
            writer.Write(normalAngle);
            writer.Write(normalPower);
            writer.Write(helper.scale.X);
            writer.Write(helper.scale.Y);
            writer.Write(helper.lenScale);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            ProjMode previous = projMode;
            projMode = (ProjMode)reader.ReadByte();
            normalUnit = reader.ReadInt32();
            normalAngle = reader.ReadSingle();
            normalPower = reader.ReadSingle();
            helper.scale = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            helper.SetScale(reader.ReadSingle());
            if (previous != projMode && projMode == ProjMode.Stab)
            {
                helper.ResetTime();
                helper.SetStabRad(Projectile.velocity.ToRotation());
            }
        }
		public override void AI() {
            if (Main.myPlayer == Projectile.owner) {
                Vector2 aim = (Main.MouseWorld - player.MountedCenter).SafeNormalize(new Vector2(player.direction, 0));
                if (Vector2.DistanceSquared(aim, Projectile.velocity) > .0009f) {
                    Projectile.velocity = aim;
                    Projectile.netUpdate = true;
                }
            }
			if (mp.Skill == 2 && mp.SkillActive) {
				if (Main.myPlayer == Projectile.owner &&
				    player.ownedProjectileCounts[ModContent.ProjectileType<LaevatainProjectile_3>()] == 0)
					Projectile.NewProjectile(player.GetSource_FromThis(), player.MountedCenter,
						Vector2.Zero, ModContent.ProjectileType<LaevatainProjectile_3>(),
						player.GetWeaponDamage(player.HeldItem), player.HeldItem.knockBack, Projectile.owner);
				Projectile.Kill();
				return;
			}
			Projectile.timeLeft = 2;
			if(player.dead||player.HeldItem.type != ModContent.ItemType<SurtrLaevatain>()) {
				Projectile.Kill();
				return;
			}
			switch (projMode) {
				case ProjMode.Move:
					helper.setoff = Vector2.Zero;
					helper.Move();
                    if (Main.myPlayer == Projectile.owner && PlayerInput.MouseInfo.LeftButton == ButtonState.Pressed) {
                        if (mp.Skill == 1 && mp.SkillActive) BeginAttack();
                        else StartNormalCombo();
                    }
					break;
				case ProjMode.Attack:
                case ProjMode.Wait:
                    UpdateNormalCombo();
                    break;
				case ProjMode.Stab:
					bool stabFinished = helper.Stab(player.Center + Projectile.velocity * 200f);
					stabEffect.Update(helper);
					if (helper.IsStabThrustPhase)
						SpawnStabTrail();
					if (helper.IsStabThrustStart) {
						if (!Main.dedServ)
							SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.38f, Pitch = -0.3f }, player.Center);
						helper.GetStabHitLine(out Vector2 stabStart, out Vector2 stabEnd);
						stabEffect.OnBurst(stabEnd);
						SpawnStabFire(stabStart, stabEnd);
					}
					if (stabFinished) {
						projMode = ProjMode.Move;
					}

					break;

			}

		}

		private void SpawnSwingFire()
		{
			if (Main.dedServ || helper.swingTime <= 0)
				return;

			Vector2 blade = (helper.swordPos - helper.handlePos).SafeNormalize(Vector2.UnitX);
			Vector2 tangent = blade.RotatedBy(normalDirection * MathHelper.PiOver2);
			Vector2 drawOffset = helper.setoff.RotatedBy(helper.swordRot);
			float reach = Vector2.Distance(helper.handlePos, helper.swordPos);
			for (int i = 0; i < 3; i++) {
				Vector2 origin = helper.handlePos + drawOffset + blade * reach * Main.rand.NextFloat(0.48f, 0.98f);
				Vector2 shed = (tangent + blade * Main.rand.NextFloat(0.2f, 0.45f))
					.SafeNormalize(tangent).RotatedBy(Main.rand.NextFloat(-0.22f, 0.22f));
				Dust flame = Dust.NewDustPerfect(origin, DustID.Torch,
					shed * Main.rand.NextFloat(4.5f, 8.5f), 65,
					new Color(255, 100, 25), Main.rand.NextFloat(1.1f, 1.8f));
				flame.noGravity = true;
			}
			if (Main.rand.NextBool(2)) {
				Vector2 origin = helper.handlePos + drawOffset + blade * reach * Main.rand.NextFloat(0.7f, 1f);
				Dust flare = Dust.NewDustPerfect(origin, ModContent.DustType<T_Flame_Attack_Particle>(),
					tangent * Main.rand.NextFloat(3f, 5.5f) + blade * Main.rand.NextFloat(0.8f, 2f),
					0, Color.White, Main.rand.NextFloat(0.75f, 1.2f));
				flare.noGravity = true;
			}
		}

		public static Effect fire = ModContent.Request<Effect>("ArknightsMod/Content/Projectiles/Guard/Laevatain/FireProcedural").Value;
		private int timer = 0;
		public override bool PreDraw(ref Color lightColor) {

			SpriteBatch sb = Main.spriteBatch;
			if (projMode == ProjMode.Attack && helper.swingTime > 0)
				DrawHeatWave(sb);
			if (projMode == ProjMode.Stab)
				stabEffect.DrawDistortion(helper, sb);
			helper.DrawBlade(sb);
            if (projMode == ProjMode.Attack || projMode == ProjMode.Wait)
                MousseVisuals.DrawEnergyColumn(helper.handlePos, (helper.swordHead - helper.handlePos).ToRotation(),
                    normalPower, new Color(255, 104, 30),
                    MathHelper.Clamp(Vector2.Distance(helper.handlePos, helper.swordHead) / 135f, .5f, 3.75f));
			if (projMode == ProjMode.Stab)
				stabEffect.DrawWindLines(helper);
			if (projMode == ProjMode.Attack) {
				helper.DrawTrip(SwingHelper.SwingHelper.SwingEffect.Zero, Color.White * 0.3f, sb,
					ModContent.Request<Texture2D>("ArknightsMod/Content/Projectiles/Guard/Laevatain/surtr_01").Value,
					blendState: BlendState.Additive);
				helper.DrawTrip(SwingHelper.SwingHelper.SwingEffect.Flow,Color.White,sb);
			}

			timer++;
			return false;
		}

		private void DrawHeatWave(SpriteBatch sb) {
			// 与 DrawBlade 使用同一个剑柄偏移，转换到当前屏幕像素坐标。
			Matrix view = Main.GameViewMatrix.TransformationMatrix;
			Vector2 grip = helper.handlePos + helper.setoff.RotatedBy(helper.swordRot);
			Vector2 screenOrigin = Vector2.Transform(grip - Main.screenPosition, view);
			Vector2 screenBlade = Vector2.TransformNormal(helper.swordPos - helper.handlePos, view);
			float distance = screenBlade.Length() * 1.15f;
			float sweptAngle = helper.swordRad - helper.ArtsSwingStart;
			if (distance < 1f || MathF.Abs(sweptAngle) < 0.001f)
				return;

			// 直接使用 helper 的起手角和当前角度，覆盖本次已经扫过的弧线。
			var direction = sweptAngle > 0f ? RotationHelper.SwingDir.plus : RotationHelper.SwingDir.minus;

			ModContent.GetInstance<RTHelper>().DrawScreen(sb, sourceRT => {
				HeatWaveRTEffect.Draw(sb, sourceRT, helper.ArtsSwingStart, helper.swordRad, direction,
					screenOrigin, distance, strength: 5f, blendState: BlendState.Opaque);
			});
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
			if ((projMode == ProjMode.Attack && normalDamageWindow) || projMode == ProjMode.Stab)
				return helper.Colliding(targetHitbox);
			return false;
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
			if (projMode == ProjMode.Stab &&
				(stabHitTargets.Count == 0 ||
				 (stabHitTargets.Count == 1 && stabHitTargets.Contains(target.whoAmI))))
				modifiers.FinalDamage *= 1.5f;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			if (projMode == ProjMode.Attack && Main.myPlayer == Projectile.owner && swingFlamesSpawned < MaxSwingFlames) {
				int count = Math.Min(Main.rand.Next(2, 4), MaxSwingFlames - swingFlamesSpawned);
				Vector2 forward = Vector2.UnitX * comboFacing;
				Vector2 origin = target.Center + forward * Math.Min(target.width * 0.25f, 18f);
				for (int i = 0; i < count; i++) {
					Vector2 velocity = forward.RotatedBy(Main.rand.NextFloat(-MathHelper.ToRadians(11f), MathHelper.ToRadians(11f)))
						* Main.rand.NextFloat(10f, 14f);
					Projectile.NewProjectile(Projectile.GetSource_FromThis(), origin, velocity,
						ModContent.ProjectileType<LaevatainSlagfire>(), Projectile.damage,
						Projectile.knockBack, Projectile.owner);
				}
				swingFlamesSpawned += count;
			}
			if (projMode == ProjMode.Stab)
				stabHitTargets.Add(target.whoAmI);
			if (projMode == ProjMode.Stab)
				stabEffect.OnHit(target.Center);
			if (!hitFeedbackPlayed && !Main.dedServ) {
				hitFeedbackPlayed = true;
				SoundEngine.PlaySound(SoundID.NPCHit1 with { Volume = 0.36f, Pitch = -0.18f, MaxInstances = 5 }, target.Center);
				if (projMode == ProjMode.Stab) {
					SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.34f, Pitch = -0.28f, MaxInstances = 4 }, target.Center);
					for (int i = 0; i < 24; i++) {
						Dust spark = Dust.NewDustPerfect(target.Center + Main.rand.NextVector2Circular(12f, 12f),
							DustID.Torch, helper.StabDirection * Main.rand.NextFloat(2f, 6f) +
							Main.rand.NextVector2Circular(3f, 3f), 55, new Color(255, 145, 35),
							Main.rand.NextFloat(1.1f, 1.8f));
						spark.noGravity = true;
					}
				}
				if (Main.myPlayer == Projectile.owner) {
					ShakeEffectPlayer shake = player.GetModPlayer<ShakeEffectPlayer>();
					shake.screenShakeTime = Math.Max(shake.screenShakeTime, projMode == ProjMode.Stab ? 5 : 2);
					shake.screenShakeVelocity = (target.Center - player.Center).SafeNormalize(Vector2.UnitX) *
						(projMode == ProjMode.Stab ? 3f : 1.2f);
				}
			}
			Dust.NewDustPerfect(target.Center, ModContent.DustType<T_Flame_Damage_Particle>(), Vector2.Zero,
				0, Color.White, 1f);
			if (projMode == ProjMode.Stab)
				Dust.NewDustPerfect(target.Center, ModContent.DustType<T_Flame_Animated28_Particle>(), Vector2.Zero,
					0, Color.White, 1.35f);
			else if (Main.rand.NextBool(2))
				Dust.NewDustPerfect(target.Center, ModContent.DustType<T_Flame_Animated28_Particle>(), Vector2.Zero,
					0, Color.White, 1f);
			else
				Dust.NewDustPerfect(target.Center, ModContent.DustType<T_Flame_Animated03_Particle>(), Vector2.Zero,
					0, Color.White, 1f);
		}

		private void SpawnStabTrail()
		{
			if (Main.dedServ)
				return;
			helper.GetStabHitLine(out Vector2 start, out Vector2 end);
			Vector2 direction = (end - start).SafeNormalize(helper.StabDirection);
			Vector2 side = new(-direction.Y, direction.X);
			for (int i = 0; i < 9; i++) {
				Vector2 origin = Vector2.Lerp(start, end, Main.rand.NextFloat()) +
					side * Main.rand.NextFloat(-Laevatain_SwingHelper.StabLineWidth * 0.5f,
						Laevatain_SwingHelper.StabLineWidth * 0.5f);
				Dust flame = Dust.NewDustPerfect(origin, DustID.Torch,
					direction * Main.rand.NextFloat(2f, 5f) + side * Main.rand.NextFloat(-2f, 2f),
					70, new Color(255, 103, 18), Main.rand.NextFloat(1f, 1.65f));
				flame.noGravity = true;
			}
		}

		private void SpawnStabFire(Vector2 start, Vector2 end)
		{
			if (Main.dedServ)
				return;
			Vector2 direction = (end - start).SafeNormalize(helper.StabDirection);
			Vector2 side = new(-direction.Y, direction.X);
			for (int i = 0; i < 42; i++) {
				Vector2 origin = Vector2.Lerp(start, end, Main.rand.NextFloat()) +
					side * Main.rand.NextFloat(-Laevatain_SwingHelper.StabLineWidth * 0.5f,
						Laevatain_SwingHelper.StabLineWidth * 0.5f);
				Dust flame = Dust.NewDustPerfect(origin, DustID.Torch,
					direction * Main.rand.NextFloat(3f, 8f) + side * Main.rand.NextFloat(-3f, 3f),
					55, new Color(255, 86, 16), Main.rand.NextFloat(1.15f, 1.9f));
				flame.noGravity = true;
			}
			if (Main.myPlayer != Projectile.owner)
				return;
			Projectile.NewProjectile(Projectile.GetSource_FromThis(), Vector2.Lerp(start, end, 0.6f), Vector2.Zero,
				ModContent.ProjectileType<LaevatainFireFlowParticle>(), 0, 0f,
				Projectile.owner, 0.74f, Main.rand.NextFloat(0f, MathHelper.TwoPi), helper.swordRot);
		}

	}
	/// <summary>
	/// 42戳刺的特效弹幕
	/// </summary>
	public sealed class LaevatainFireFlowParticle : ModProjectile
	{
		private const int Lifetime = 28;
		public override string Texture => "ArknightsMod/Content/Dusts/Fire/fire_18";

		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.friendly = false;
			Projectile.hostile = false;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.penetrate = -1;
			Projectile.timeLeft = Lifetime;
			Projectile.aiStyle = -1;
		}

		public override void AI()
		{
			Projectile.velocity = Vector2.Zero;
			Projectile.rotation = Projectile.ai[2];
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
			float progress = MathHelper.Clamp((Lifetime - Projectile.timeLeft) / (float)Lifetime, 0f, 1f);
			float fade = 1f - progress;
			Main.spriteBatch.End();
			Effect effect = ModContent.Request<Effect>("ArknightsMod/Content/Projectiles/Guard/Laevatain/FireProcedural",AssetRequestMode.ImmediateLoad).Value;;
			effect.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly * 2f);
			Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap,
				DepthStencilState.None, RasterizerState.CullNone, effect,
				Main.GameViewMatrix.TransformationMatrix);
			Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, null,
				Color.White * fade,Projectile.ai[2],texture.Size()/2f,new Vector2(1,0.5f),SpriteEffects.None, 0f);
			Main.spriteBatch.End();
			Main.spriteBatch.Begin();
			return false;
		}
	}
}
