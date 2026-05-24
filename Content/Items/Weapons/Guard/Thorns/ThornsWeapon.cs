using ArknightsMod.Content.Items.Material.T4;
using ArknightsMod.Content.Items.Material.T5;
using ArknightsMod.Content.Projectiles.Guard.Thorns;
using ArknightsMod.Content.Rarities;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Players;
using Microsoft.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.ModLoader;
using static Terraria.Graphics.VertexStrip;
using Color = Microsoft.Xna.Framework.Color;

namespace ArknightsMod.Content.Items.Weapons.Guard.Thorns
{
    public class ThornsWeapon : UpgradeWeaponBase
	{
		public override void AddRecipes() {
			Recipe recipe = CreateRecipe();
			recipe.AddIngredient<PolymerizationPreparation>(4);
			recipe.AddIngredient<OrironBlock>(6);
			recipe.AddTile(ModContent.TileType<FactoryTile>());
			recipe.Register();
		}
		private int skillcs = 0;

		private static SoundStyle SkillActive1;
		private static SoundStyle SkillActive3;
		private static SoundStyle jici1;
		private static SoundStyle JiCia;
		private static SoundStyle JiCi2;
		private static SoundStyle JiCi3a;
		public override void Load() {
			SkillActive1 = new SoundStyle("ArknightsMod/Sounds/SkillActive1") {
				Volume = 0.4f,
				MaxInstances = 4,
			};
			SkillActive3 = new SoundStyle("ArknightsMod/Sounds/SkillActive3") {
				Volume = 1f,
				MaxInstances = 4,
			};
			jici1 = new SoundStyle("ArknightsMod/Sounds/jici1") {
				Volume = 1f,
				MaxInstances = 4,
			};
			JiCia = new SoundStyle("ArknightsMod/Sounds/JiCia") {
				Volume = 1f,
				MaxInstances = 5,
			};
			JiCi2 = new SoundStyle("ArknightsMod/Sounds/JiCi2") {
				Volume = 1f,
				MaxInstances = 5,
			};
			JiCi3a = new SoundStyle("ArknightsMod/Sounds/JiCi3a") {
				Volume = 0.8f,
				MaxInstances = 5,
			};
		}
		public override bool MeleePrefix() => true;

		private int skill = 0;
		public override void SetDefaults()
        {
            Item.damage = 142;//攻击力
            Item.DamageType = DamageClass.Melee;
            Item.width = 71;//丢出体积
            Item.height = 104;//丢出体积
            Item.scale = 1;//图片缩放
            Item.useTime = 39;//使用一次时间 
            Item.useAnimation = 39;//动画显示时间
            Item.knockBack = 2f;//击退
            Item.value = 200000;//大概是价格吧
            Item.rare = ModContent.RarityType<ArknightsRarities>();//稀有度
            Item.autoReuse = true;//是否可以连续使用
            Item.noMelee = true;//贴图是否造成伤害
            Item.shoot = 87;
            Item.shootSpeed = 16;//弹幕射速
            Item.useTurn = false;
            Item.noUseGraphic = true;
            Item.useStyle = 13;//?
            Item.channel = true;
        }
		public override bool AltFunctionUse(Player player) => true;
		public override void HoldItem(Player player) {
			var modPlayer = Main.LocalPlayer.GetModPlayer<WeaponPlayer>();
			if (Main.myPlayer == player.whoAmI) {
				if (Item.type != ModContent.ItemType<ThornsWeapon>()) {
					player.GetModPlayer<ThornsCounterPlayer>().JiCi2 = false;
				}
				if (modPlayer.Skill == 1 && modPlayer.SkillActive&&Item.type == ModContent.ItemType<ThornsWeapon>()) {
					player.GetModPlayer<ThornsCounterPlayer>().JiCi2 = true;
				}
				// S1
				if (modPlayer.Skill == 1 && !modPlayer.SkillActive) {
					player.GetModPlayer<ThornsCounterPlayer>().JiCi2 = false;
				}
			}
		}
		public override bool CanUseItem(Player player) {
			var modPlayer = Main.LocalPlayer.GetModPlayer<WeaponPlayer>();
			if (Main.myPlayer == player.whoAmI) {
				if (player.altFunctionUse == 2) {
					if (!modPlayer.SummonMode) {
						// S1
						if (modPlayer.Skill == 0 && modPlayer.StockCount > 0 && !modPlayer.SkillActive) {
							modPlayer.SkillActive = true;
							modPlayer.SkillTimer = 0;

							modPlayer.DelStockCount();

							Item.UseSound = SkillActive1;
							SoundEngine.PlaySound(Item.UseSound.Value, player.Center);
						//S2
						}
						if (modPlayer.Skill == 1 && modPlayer.StockCount > 0 && !modPlayer.SkillActive) {
							modPlayer.SkillActive = true;
							modPlayer.SkillTimer = 0;
							player.GetModPlayer<ThornsCounterPlayer>().JiCi2 = true;
							modPlayer.DelStockCount();

							Item.UseSound = SkillActive3;
							SoundEngine.PlaySound(Item.UseSound.Value, player.Center);
						}
						//S3
						if (modPlayer.Skill == 2 && modPlayer.StockCount > 0 && !modPlayer.SkillActive&&skillcs>=1) {
							modPlayer.SkillActive = true;
							modPlayer.SkillTimer = 0;
							modPlayer.DelStockCount();
							skillcs = 2;
							Item.UseSound = SkillActive1;
							modPlayer.UpdateActiveSkill2();
							SoundEngine.PlaySound(Item.UseSound.Value, player.Center);
						}
						else if (modPlayer.Skill == 2 && modPlayer.StockCount > 0 && !modPlayer.SkillActive && skillcs < 1) {
							modPlayer.SkillActive = true;
							modPlayer.SkillTimer = 0;

							modPlayer.DelStockCount();
							skillcs = 1;
							Item.UseSound = SkillActive1;
							SoundEngine.PlaySound(Item.UseSound.Value, player.Center);
						}
						else
							return false;
					}
				}
				else {
					if (!modPlayer.SummonMode) {
						Item.UseSound = JiCia;
						SoundEngine.PlaySound(Item.UseSound.Value, player.Center);
						if(modPlayer.CurrentSkill.AutoUpdateActive == false) {
							skillcs = 2;
						}

						// S1
						if (modPlayer.Skill == 0 && modPlayer.SkillActive) {

						}
						else if (modPlayer.Skill == 0 && !modPlayer.SkillActive) {

						}
						// S2
						if (modPlayer.Skill == 1 && modPlayer.SkillActive) {
							player.controlUseItem = false; 
							player.itemAnimation = 0;
							player.itemTime = 0;
						}
						else if (modPlayer.Skill == 1 && !modPlayer.SkillActive) {
							player.GetModPlayer<ThornsCounterPlayer>().JiCi2 = false;
						}
						//S31
						if (modPlayer.Skill ==2 && modPlayer.SkillActive&&skillcs>=1) {
							Item.UseSound = JiCi3a;
							SoundEngine.PlaySound(Item.UseSound.Value, player.Center);
						}
						else if (modPlayer.Skill == 2 && !modPlayer.SkillActive) {
							modPlayer.OffensiveRecovery();
							player.GetModPlayer<ThornsCounterPlayer>().JiCi2 = false;
						}
						else if (modPlayer.Skill == 2 && modPlayer.SkillActive) {
							Item.UseSound = JiCi3a;
							SoundEngine.PlaySound(Item.UseSound.Value, player.Center);
						}
					}
				}
			}
			return base.CanUseItem(player);
		}
		public override void ModifyWeaponDamage(Player player, ref StatModifier damage) {
			var modPlayer = Main.LocalPlayer.GetModPlayer<WeaponPlayer>();
			if (Main.myPlayer == player.whoAmI) {
				if (modPlayer.Skill == 2 && modPlayer.SkillActive == true&& skillcs <= 1) {
					player.GetAttackSpeed(DamageClass.Melee) += 0.25f;
				}
				if (modPlayer.Skill == 2 && modPlayer.SkillActive == true&& skillcs > 1) {
					player.GetAttackSpeed(DamageClass.Melee) += 0.5f;
				}
			}
		}
		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {

			var modPlayer = Main.LocalPlayer.GetModPlayer<WeaponPlayer>();

                //S3至高之术2
             if(modPlayer.Skill == 2 && modPlayer.SkillActive&&skillcs>1)
             {
                    float jc = 2.2f;
                    Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<ThornsS3Slash>(), (int)(damage * jc), knockback, Main.myPlayer);
                    int p = Projectile.NewProjectile(source, position + velocity*3, velocity/2f, ModContent.ProjectileType<ThornsBolt>(), (int)(damage * jc), knockback, Main.myPlayer, 1);
                    Main.projectile[p].extraUpdates = 1;
             }

			//S3至高之术1
			 else if (modPlayer.Skill == 2 && modPlayer.SkillActive&& skillcs <= 1)
            {
                float jc = 1.6f;
                Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<ThornsS3Slash>(), (int)(damage * jc), knockback, Main.myPlayer);
                int p = Projectile.NewProjectile(source, position + velocity, velocity/2f, ModContent.ProjectileType<ThornsBolt>(), (int)(damage * jc), knockback, Main.myPlayer, 1);
                Main.projectile[p].extraUpdates = 1;
            }
			//S1攻击力强化
			else if (modPlayer.Skill == 0 && modPlayer.SkillActive) {
				Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<ThornsSlash>(), (int)(damage * 2f), knockback, Main.myPlayer);
				Projectile.NewProjectile(source, position + velocity * 3, velocity, ModContent.ProjectileType<ThornsBolt>(), (int)(damage * .8f * 2f), knockback, Main.myPlayer);
			}
			else {
				if(!modPlayer.SkillActive)
				{
					Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<ThornsSlash>(), damage, knockback, Main.myPlayer);
					Projectile.NewProjectile(source, position + velocity * 3, velocity, ModContent.ProjectileType<ThornsBolt>(), (int)(damage * .8f), knockback, Main.myPlayer);
				}
			}
			return false;
        }
    }
}