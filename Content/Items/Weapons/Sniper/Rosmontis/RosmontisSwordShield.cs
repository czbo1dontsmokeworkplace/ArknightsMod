using ArknightsMod.Content.Items.Weapons;
using ArknightsMod.Content.Projectiles.Sniper.Rosmontis;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using System;
using System.Drawing;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace ArknightsMod.Content.Items.Weapons.Sniper.Rosmontis
{
	public class RosmontisSwordShield : UpgradeWeaponBase
	{
		public float RandomShootRadius => 200;

		public Rectangle ClampShootRect => new Rectangle() { Height = 800, Width = 1600,X = 0,Y = -1200} ;

		public float BaseSpeed = 15;


		public override void SetStaticDefaults()
		{
			Item.ResearchUnlockCount = 1;
		}

		public override void SetDefaults()
		{
			Item.damage = 50;
			Item.DamageType = DamageClass.Ranged;
			Item.width = 34;
			Item.height = 80;
			Item.useTime = 30;
			Item.useAnimation = 30;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.knockBack = 5f;
			Item.autoReuse = true;
			Item.noMelee = true;
			Item.noUseGraphic = true;
			Item.rare = ItemRarityID.Green;
			Item.value = Item.sellPrice(silver: 50);
			Item.shoot = 1;
		}

		public override float UseSpeedMultiplier(Player player) {
			var modPlayer = player.GetModPlayer<WeaponPlayer>();
			if (modPlayer.SkillActive) {
				switch (modPlayer.Skill) {
					case 1:
						return base.UseSpeedMultiplier(player) * 0.5f;
					case 2:
						return base.UseSpeedMultiplier(player) * 1.5f;
				}
			}

			return base.UseSpeedMultiplier(player);
		}

		public override bool AltFunctionUse(Player player) => true;

		public override bool CanUseItem(Player player)
		{
			var modPlayer = player.GetModPlayer<WeaponPlayer>();
			if (Main.myPlayer == player.whoAmI) {
				if (player.altFunctionUse == 2) {
					if ((modPlayer.Skill == 1 || modPlayer.Skill == 2) && modPlayer.StockCount > 0 && !modPlayer.SkillActive) {
						modPlayer.SkillActive = true;
						modPlayer.SkillTimer = 0;

						modPlayer.DelStockCount();
						return false;
					}

					if (modPlayer.Skill ==2 && modPlayer.SkillActive) {
						return true;
					}
					return false;
				}

			}

			return true;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage,
			float knockback) {
			var proj = ModContent.ProjectileType<RosmontisProjectileBase>();

			var VisualPlayer = player.GetModPlayer<RosmontisModPlayer>();
			var modPlayer = player.GetModPlayer<WeaponPlayer>();



			if (modPlayer.Skill == 0)
			{
				if (modPlayer.StockCount == 0)
				{
					modPlayer.OffensiveRecovery();
				}
				else if (modPlayer.StockCount > 0)
				{
					// 当前有已充能次数，本次攻击启动技能
					modPlayer.SkillActive = true;
					modPlayer.SkillTimer = 0;
					modPlayer.DelStockCount();
				}
			}
			if (modPlayer.SkillActive) {
				switch (modPlayer.Skill) {
					case 0: {
						VisualPlayer.Controller.ShootAnimationForBase();
						var pos = GetBasicRandomShootPosition(player,out Vector2 _direction);
						ShootBaseProjectile(player,source, pos, _direction * BaseSpeed*2f, (int)(damage * 1.8f), knockback,modPlayer.Skill);
						return false;
						break;
					}
					case 1: {
						VisualPlayer.Controller.StartShootAnimation(2);
						VisualPlayer.Controller.StartShootAnimation(3);
						var mousePos = Main.MouseWorld;
						ShootBaseProjectile(player,source, mousePos + Vector2.UnitY*-800, Vector2.UnitY * BaseSpeed*1f, (int)(damage * 1.5f), knockback,modPlayer.Skill);
						return false;
						break;
					}
					case 2: {
						var pos = GetBasicRandomShootPosition(player,out Vector2 _direction);
						if (player.altFunctionUse ==2) {
							VisualPlayer.ShootSkill3TaggedProjectile(source,pos, _direction * BaseSpeed*2.75f, (int)(damage * 3f), knockback,player);
						}
						else {
							VisualPlayer.Controller.ShootAnimationForBase();
							ShootBaseProjectile(player,source, pos, _direction * BaseSpeed*1.75f, (int)(damage * 1.75f), knockback,modPlayer.Skill);
						}
						return false;
						break;
					}
				}
			}

			{
				VisualPlayer.Controller.ShootAnimationForBase();
				var pos = GetBasicRandomShootPosition(player,out Vector2 _direction);
				ShootBaseProjectile(player,source, pos, _direction * BaseSpeed, damage, knockback,0);
			}

			return false;
		}



		public Vector2 GetBasicRandomShootPosition(Player player,out Vector2 direction) {
			Vector2  mousePosition = Main.MouseWorld;
			float mousePercentY = Main.MouseScreen.Y / Main.screenHeight;
			float mouseX = (mousePosition.X + player.Center.X) / 2;

			Vector2 random = Main.rand.NextVector2CircularEdge(RandomShootRadius, RandomShootRadius);

			Vector2 rPos = new Vector2(mouseX,- (1-mousePercentY) * ClampShootRect.Height + player.Center.Y) + random;
			Rectangle clampRect = new Rectangle(ClampShootRect.X + (int)player.Center.X, ClampShootRect.Y +(int)player.Center.Y, ClampShootRect.Width, ClampShootRect.Height);
			Vector2 final = Vector2.Clamp(rPos,clampRect.TopLeft(),clampRect.BottomRight());

			direction = (Main.MouseWorld - final).SafeNormalize(Vector2.Zero);
			return final;
		}

		private void ShootBaseProjectile(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int damage,
			float knockback,int SkillID) {
			var proj = ModContent.ProjectileType<RosmontisProjectileBase>();
			var projID = Projectile.NewProjectile(source, position, velocity, proj, damage, knockback, player.whoAmI,SkillID);


		}
	}
}
