using ArknightsMod.Content.Items.Weapons.Guard.Chen;
using ArknightsMod.Content.Items.Weapons.Guard.Frostleaf;
using ArknightsMod.Content.SwingHelper;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Frostleaf
{
	public class Frostleaf_Axe : ModProjectile
	{
		public SwingHelper.SwingHelper helper;
		public Player player => Main.player[Projectile.owner];
		public override void SetDefaults() {
			Projectile.width = 10;
			Projectile.height = 10; // ?�������?��?�
			Projectile.friendly = true; // ?������?��?���
			Projectile.penetrate = -1; // ?�������?�?
			Projectile.tileCollide = false; // ?���?����?��?
			Projectile.usesLocalNPCImmunity = true; // ?��?�����?
			Projectile.ownerHitCheck = true; // ?��?�����?���������?�����??�?�����?�?��?����?�?
			Projectile.DamageType = DamageClass.MeleeNoSpeed; // ?����?��??����
			Projectile.ignoreWater = true;
			Projectile.localNPCHitCooldown = 14;
		}
		private enum WeaponState{Move,Swing,SpSwing,Shoot}
		public override void OnSpawn(IEntitySource source) {
			helper = new SwingHelper.SwingHelper(14, 2)
				.SetPlayer(player)
				.SetProj(Projectile)
				.SetTex(TextureAssets.Projectile[Projectile.type].Value)
				.SetSwingRad(MathF.PI);
			helper.setoff = new Vector2(-12, 0);
			helper.handleLength = new Vector2(36, 10);
			helper.swordLength = new Vector2(70, 10);
			helper.lagTime = 8;
			helper.MaxChargetime = 10;
			combo1.Func = () =>
			{
				if (helper.Swing())
				{
					combo1.IsOver(true);
					helper.ReloadIndex();
					helper.lagTime = 8;
					helper.Chargetime = 10;
					helper.swingTime = 0;
					helper.SetScale(new Vector2(1f, -0.5f));
					return StepResult.Next;
				}
				return StepResult.Back;
			};
			combo1.Next = combo2;
			combo2.Func = () => {
				if (helper.Swing()) {
					helper.SetScale(new Vector2(1f, 1f));
					combo2.IsOver(true);
					helper.ReloadIndex();
					helper.Chargetime = 0;
					helper.lagTime = 8;
					helper.swingTime = 0;
					helper.SetIndex(20);
					helper.SetAction(() => {helper.ScreenPosModify(helper.swordPos);});
					Projectile.NewProjectile(Projectile.GetSource_FromAI(), player.Center, Main.MouseScreen.SafeNormalize(Vector2.One)
						,ModContent.ProjectileType<Frostleaf_Projectile>(),Projectile.damage,Projectile.knockBack);
					return StepResult.Next;
				}
				return StepResult.Back;
			};
			combo2.Next = combo3;
			combo3.Func = () => {
				if (helper.Swing(swingDir: RotationHelper.SwingDir.minus)) {
					combo3.IsOver(true);
					helper.ReloadIndex();
					helper.lagTime = 8;
					helper.Chargetime = 0;
					helper.swingTime = 0;
					helper.SetIndex(14);
					state = WeaponState.Move;
					helper.SetAction(() => { });
					return StepResult.Next;
				}
				return StepResult.Back;
			};
			runner = new StepSkillRunner(combo1);
		}

		private StepSkillRunner runner;
		private StepSkill combo1 = new StepSkill(false);
		private StepSkill combo2 =  new StepSkill(false);
		private StepSkill combo3 = new StepSkill(true);

		private int attack = 0;
		private bool press = false;
		private WeaponState state = WeaponState.Move;
		public override void AI() {
			if(player.dead||player.HeldItem.type != ModContent.ItemType<FrostleafAxe>())
				Projectile.Kill();
			switch (state) {
				case WeaponState.Move:
					helper.Move();
					if (PlayerInput.MouseInfo.LeftButton == ButtonState.Pressed && !press)
					{
						press = true;
						if (attack == 0) {
							state = WeaponState.Swing;
							attack += 1;
						}
						else {
							state = WeaponState.SpSwing;
							attack = 0;
						}
						Vector2 mouse = Main.MouseWorld - player.Center;
						helper.PointMouseRad(MathF.Atan2(mouse.Y, mouse.X));
						runner =  new StepSkillRunner(combo1);
					}
					break;
				case WeaponState.Swing:
					if (helper.Swing()) {
						state = WeaponState.Move;
						helper.ReloadIndex();
						helper.swingTime = 0;
						helper.lagTime = 8;
						helper.SetScale(new Vector2(1f, 0.5f));
					}
					break;
				case WeaponState.SpSwing:
					runner.Run();
					break;
			}
			if(press&&PlayerInput.MouseInfo.LeftButton==ButtonState.Pressed)
				press = false;
		}

		public override bool PreDraw(ref Color lightColor) {
			SpriteBatch sb = Main.spriteBatch;
			helper.DrawBlade(sb);
			if(state != WeaponState.Move)
				helper.DrawTrip(SwingHelper.SwingHelper.SwingEffect.Zero,lightColor,sb);
			return false;
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => helper.Colliding(targetHitbox);
	}
}

