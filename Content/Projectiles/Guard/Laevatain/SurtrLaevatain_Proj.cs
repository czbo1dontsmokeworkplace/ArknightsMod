using ArknightsMod.Content.Dusts;
using ArknightsMod.Content.Dusts.Fire;
using ArknightsMod.Content.Items.Weapons.Guard.Surtr;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Content;
using System;
using System.Net.Mail;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameInput;
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
			ParallelSpacing = 18f,
			WindColor = new Color(255, 90, 24),
			WindPositionAlongBlade = 0.8f,
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
			helper.SetHandlePos(new Vector2(0, 10));
			helper.SetSwordPos(new Vector2(70, 10));
			helper.SetScale(new Vector2(1f,1f));
			helper.lagTime = 8;
			helper.MaxChargetime = 12;
			helper.TotalStabDuration = StabDuration;
			helper.SetScale(1f);
		}

		public enum ProjMode {Move,Attack,Wait,Stab}
		public ProjMode projMode = ProjMode.Move;
		public WeaponPlayer mp => player.GetModPlayer<WeaponPlayer>();
		private bool press = false;
		public override void AI() {
			Projectile.timeLeft = 2;
			if(player.dead||player.HeldItem.type != ModContent.ItemType<SurtrLaevatain>())
				Projectile.Kill();
			switch (projMode) {
				case ProjMode.Move:
					helper.Move();
					if (!press && PlayerInput.MouseInfo.LeftButton == ButtonState.Pressed) {
						press = true;
						stabEffect.Reset();
						projMode = ProjMode.Attack;
						Vector2 mousePos = Main.MouseWorld - player.Center;
						helper.PointMouseRad(MathF.Atan2(mousePos.Y, mousePos.X));
						helper.SetStabRad(MathF.Atan2(mousePos.Y, mousePos.X));
						helper.ResetTime(12);
						helper.ReloadIndex();
						helper.SetScale(new Vector2(1f, 1f));
						helper.SetScale(1f);
						if (mp.Skill == 0) {
							Main.NewText(mp.StockCount);
							if (mp.SkillCharge >= mp.SkillChargeMax) {
								helper.SetScale(new Vector2(1f, 0.856f));
								helper.SetScale(1.3f);
								mp.SkillCharge = 0;
								SoundEngine.PlaySound(SurtrLaevatain.SkillActiveSound, player.Center);
							}
							else {
								mp.SkillCharge++;
							}
						}
						else if(mp.Skill == 1 && mp.SkillActive)
							projMode =  ProjMode.Stab;
					}
					break;
				case ProjMode.Attack:
					if(Main.rand.NextBool(3))
						Dust.NewDust(helper.swordPos, 0, 0, ModContent.DustType<SurtrAttack_Dust>());
					if (helper.Swing()) {
						projMode = ProjMode.Wait;
					}
					break;
				case ProjMode.Stab:
					bool stabFinished = helper.Stab(Main.MouseWorld);
					stabEffect.Update(helper);
					if (helper.IsStabThrustStart) {
						Vector2 drawOffset = helper.setoff.RotatedBy(helper.swordRot);
						Vector2 burstTip = helper.swordPos + drawOffset;
						Vector2 firePosition = Vector2.Lerp(player.Center, burstTip, 0.55f);
						stabEffect.OnBurst(burstTip);
						SpawnStabFire(firePosition);
					}
					if (stabFinished) {
						projMode = ProjMode.Move;
					}

					break;
				case ProjMode.Wait:
					if (helper.Wait()) {
						helper.ResetTime(12);
						projMode = ProjMode.Move;
						helper.SetScale(new Vector2(1f, 1f));
						helper.ReloadIndex();
					}
					break;

			}

			if (press && PlayerInput.MouseInfo.LeftButton == ButtonState.Released)
				press = false;
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
			float sweptAngle = helper.swordRad - helper.startRad;
			if (distance < 1f || MathF.Abs(sweptAngle) < 0.001f)
				return;

			// 直接使用 helper 的起手角和当前角度，覆盖本次已经扫过的弧线。
			var direction = sweptAngle > 0f ? RotationHelper.SwingDir.plus : RotationHelper.SwingDir.minus;

			ModContent.GetInstance<RTHelper>().DrawScreen(sb, sourceRT => {
				HeatWaveRTEffect.Draw(sb, sourceRT, helper.startRad, helper.swordRad, direction,
					screenOrigin, distance, strength: 5f, blendState: BlendState.Opaque);
			});
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
			if(projMode!=ProjMode.Move)
				return helper.Colliding(targetHitbox);
			return false;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			if (projMode == ProjMode.Stab)
				stabEffect.OnHit(target.Center);
			Dust.NewDust(target.Center, 0, 0, ModContent.DustType<surtrDamage_Dust>(),Scale:1f);
			if(Main.rand.NextBool(2))
				Dust.NewDust(target.Center, 0, 0, ModContent.DustType<fire_28>(),Scale:1f);
			else {
				Dust.NewDust(target.Center, 0, 0, ModContent.DustType<fire_03>(),Scale:1f);
			}
		}

		private void SpawnStabFire(Vector2 position)
		{
			Projectile.NewProjectile(Projectile.GetSource_FromThis(), position, Vector2.Zero,
				ModContent.ProjectileType<LaevatainFireFlowParticle>(), 0, 0f,
				Projectile.owner, 0.74f, Main.rand.NextFloat(0f, MathHelper.TwoPi), helper.swordRot);
		}
	}

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
			int age = Lifetime - Projectile.timeLeft;
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
			effect.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly*2f);
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
