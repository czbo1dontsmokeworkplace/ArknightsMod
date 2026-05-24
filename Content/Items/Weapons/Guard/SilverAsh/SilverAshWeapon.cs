using ArknightsMod.Content.Items.Material.T4;
using ArknightsMod.Content.Items.Material.T5;
using ArknightsMod.Content.Projectiles.Guard.SilverAsh;
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


namespace ArknightsMod.Content.Items.Weapons.Guard.SilverAsh
{
    public class SilverAshWeapon : UpgradeWeaponBase
	{
		public override void AddRecipes() {
			Recipe recipe = CreateRecipe();
			recipe.AddIngredient<D32Steel>(4);
			recipe.AddIngredient<WhiteHorseKohl>(6);
			recipe.AddTile(ModContent.TileType<FactoryTile>());
			recipe.Register();
		}
		public override bool MeleePrefix() => true;
        public override void SetDefaults()
        {
            Item.damage = 142;//攻击力
            Item.DamageType = DamageClass.Melee;
            Item.width = 52;//丢出体积
            Item.height = 48;//丢出体积
            Item.scale = 1;//图片缩放
            Item.useTime = 39;//使用一次时间 
            Item.useAnimation = 39;//动画显示时间
			Item.knockBack = 2f;//击退
            Item.value = 200000;//大概是价格吧 
            Item.rare = ModContent.RarityType<ArknightsRarities>();//稀有度
            Item.autoReuse = true;//是否可以连续使用
            Item.noMelee = true;//贴图是否造成伤害
            Item.shoot = 87;
            Item.shootSpeed = 4;//弹幕射速
            Item.useTurn = false;
            Item.noUseGraphic = true;
			Item.UseSound = NoSound;
            Item.useStyle = 13;//?
            Item.channel = true;
            //SoundStyle zji = new SoundStyle("Slashsoul/bgms/yc");
            //Item.UseSound = zji;
        }
        /// <summary>
        /// 技能切换
        /// </summary>
        bool GJXT = false;
		private bool skcl = true;
		private int skill2cd=0;
		private static SoundStyle SkillActive1;
		private static SoundStyle SkillActive3;
		private static SoundStyle yinhui2A;
		private static SoundStyle yinhuiA;
		private static SoundStyle NoSound;

		public override void Load() {
			SkillActive1 = new SoundStyle("ArknightsMod/Sounds/SkillActive1") {
				Volume = 0.4f,
				MaxInstances = 4,
			};
			SkillActive3 = new SoundStyle("ArknightsMod/Sounds/SkillActive3") {
				Volume = 1f,
				MaxInstances = 4,
			};
			yinhuiA = new SoundStyle("ArknightsMod/Sounds/yinhuiA") {
				Volume = 0.4f,
				MaxInstances = 4,
			};
			yinhui2A = new SoundStyle("ArknightsMod/Sounds/yinhui2A") {
				Volume = 0.5f,
				MaxInstances = 4,
			};
			NoSound = new SoundStyle("ArknightsMod/Sounds/NoSound") {
				Volume = 0f,
				MaxInstances = 4,
			};
		}
		public override bool AltFunctionUse(Player player) => true;
		public override bool CanUseItem(Player player) {
			var modPlayer = Main.LocalPlayer.GetModPlayer<WeaponPlayer>();
			if (Main.myPlayer == player.whoAmI) {
				if (player.altFunctionUse == 2) {
					if (!modPlayer.SummonMode) {
						// S2
						if (modPlayer.Skill == 1 && modPlayer.StockCount > 0 && !modPlayer.SkillActive) {
							modPlayer.SkillActive = true;
							modPlayer.SkillTimer = 0;

							modPlayer.DelStockCount();
							player.GetModPlayer<SilverAshS2Player>().yinhui2 = true;
							Item.UseSound = SkillActive3;
							SoundEngine.PlaySound(Item.UseSound.Value, player.Center);
						}
						else if (modPlayer.Skill == 1 && modPlayer.SkillActive&&skill2cd>=300) {
							modPlayer.SkillActive = false;
							modPlayer.StockCount = 0;
							player.GetModPlayer<SilverAshS2Player>().yinhui2 = false;
							Item.UseSound = SkillActive1;
							SoundEngine.PlaySound(Item.UseSound.Value, player.Center);

						}
						else if (modPlayer.Skill == 2 && modPlayer.StockCount > 0 && !modPlayer.SkillActive) {
							modPlayer.SkillActive = true;
							modPlayer.SkillTimer = 0;
							player.GetModPlayer<SilverAshS3Player>().yinhui3 = true;
							modPlayer.DelStockCount();

							Item.UseSound = SkillActive1;
							SoundEngine.PlaySound(Item.UseSound.Value, player.Center);
						}
						return false;
					}
				}
				else {
					if (!modPlayer.SummonMode) {
						if (modPlayer.Skill == 0) {
							if (modPlayer.StockCount == 0) {
								modPlayer.OffensiveRecovery();
								Item.UseSound = NoSound;
								SoundEngine.PlaySound(Item.UseSound.Value, player.Center);
							}
							else if (modPlayer.StockCount > 0) {
								modPlayer.SkillActive = true;
								modPlayer.SkillTimer = 0;
								modPlayer.DelStockCount();
								Item.UseSound = NoSound;
								SoundEngine.PlaySound(Item.UseSound.Value, player.Center);
							}
						}
						if (modPlayer.Skill == 1&&modPlayer.SkillActive) {
							Item.UseSound = NoSound;
							SoundEngine.PlaySound(Item.UseSound.Value, player.Center);
						}
						else if (modPlayer.Skill == 1 && !modPlayer.SkillActive) {
							Item.UseSound = NoSound;
							SoundEngine.PlaySound(Item.UseSound.Value, player.Center);
						}
						if (modPlayer.Skill == 2 && modPlayer.SkillActive) {
							Item.UseSound = NoSound;
							SoundEngine.PlaySound(Item.UseSound.Value, player.Center);
						}
						else if (modPlayer.Skill == 2 && !modPlayer.SkillActive) {
							Item.UseSound = NoSound;
							SoundEngine.PlaySound(Item.UseSound.Value, player.Center);
						}
					}
				}
			}
			return base.CanUseItem(player);
		}
		public override void HoldItem(Player player) {
			var modPlayer = Main.LocalPlayer.GetModPlayer<WeaponPlayer>();
			if (Main.myPlayer == player.whoAmI) {
				if (modPlayer.Skill == 1 && modPlayer.SkillActive && Item.type == ModContent.ItemType<SilverAshWeapon>()) {
					player.GetModPlayer<SilverAshS2Player>().yinhui2 = true;
					skill2cd++;
				}
				// S1
				if (modPlayer.Skill == 1 && !modPlayer.SkillActive) {
					player.GetModPlayer<SilverAshS2Player>().yinhui2 = false;
					skill2cd=0;
				}
				if (modPlayer.Skill != 1) {
					player.GetModPlayer<SilverAshS2Player>().yinhui2 = false;
					skill2cd = 0;
				}
				if (modPlayer.Skill == 2 && modPlayer.SkillActive && Item.type == ModContent.ItemType<SilverAshWeapon>()) {
					player.GetModPlayer<SilverAshS3Player>().yinhui3 = true;
				}
				if (modPlayer.Skill == 2 && !modPlayer.SkillActive) {
					player.GetModPlayer<SilverAshS3Player>().yinhui3 = false;
				}
				if (modPlayer.Skill != 2) {
					player.GetModPlayer<SilverAshS3Player>().yinhui3 = false;
				}
			}
		}
		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
			var modPlayer = Main.LocalPlayer.GetModPlayer<WeaponPlayer>();
			SoundStyle zji2 = new SoundStyle("ArknightsMod/Sounds/yinh1")
            {
                MaxInstances = 4
            };
            SoundStyle zji3 = new SoundStyle("ArknightsMod/Sounds/yinh2")
            {
                MaxInstances = 4
            };
			SoundStyle yh2A = new SoundStyle("ArknightsMod/Sounds/yinhui2A") {
				MaxInstances = 4
			};
			SoundStyle yhA = new SoundStyle("ArknightsMod/Sounds/yinhuiA") {
				MaxInstances = 4
			};
			int bzd = Main.rand.Next(-7, 7 + 1);
            //天赋
            int jinsh = (int)(damage * 1.12f);
            //强力击类技能单独写
            if (modPlayer.Skill == 0)
            {
                if (!modPlayer.SkillActive)//普攻
                {
					SoundEngine.PlaySound(yhA, player.position);
					Projectile.NewProjectile(source, position
                    , (velocity * 1.4f).RotatedBy(bzd / 45f), ModContent.ProjectileType<SilverAshSlash>(), jinsh, knockback, Main.myPlayer);
                    int mm1 = Main.rand.Next(100, 401) * player.direction;
                    Vector2 velocity1 = (new Vector2(mm1, 655)).SafeNormalize(Vector2.Zero) * (22f);
                    Projectile.NewProjectile(source, Main.MouseWorld - new Vector2(mm1, 655)
                    , velocity1, ModContent.ProjectileType<SilverAshHomingSlash>(), (int)(jinsh * .8f), knockback, Main.myPlayer);
                }
                else //技能1
                {
                    SoundEngine.PlaySound(zji2, player.position);
					SoundEngine.PlaySound(yhA, player.position);
					Projectile.NewProjectile(source, position
                  , (velocity * 1.4f).RotatedBy(bzd / 45f), ModContent.ProjectileType<SilverAshSlash>(), (int)(jinsh * 2.9f), knockback, Main.myPlayer, 0, 1);
                    int mm1 = Main.rand.Next(100, 401) * player.direction;
                    Vector2 velocity1 = (new Vector2(mm1, 655)).SafeNormalize(Vector2.Zero) * (22f);
                    Projectile.NewProjectile(source, Main.MouseWorld - new Vector2(mm1, 655)
                    , velocity1, ModContent.ProjectileType<SilverAshHomingSlash>(), (int)(jinsh * 2.9f * .8f), knockback, Main.myPlayer, 1);
                }
            }
            else if (modPlayer.Skill == 1)
            {
                if (!modPlayer.SkillActive)
                {
					//普通攻击
					SoundEngine.PlaySound(yhA, player.position);
					Projectile.NewProjectile(source, position
                 , (velocity * 1.4f).RotatedBy(bzd / 45f), ModContent.ProjectileType<SilverAshSlash>(), jinsh, knockback, Main.myPlayer);
                    int mm1 = Main.rand.Next(100, 401) * player.direction;
                    Vector2 velocity1 = (new Vector2(mm1, 655)).SafeNormalize(Vector2.Zero) * (22f);
                    Projectile.NewProjectile(source, Main.MouseWorld - new Vector2(mm1, 655)
                    , velocity1, ModContent.ProjectileType<SilverAshHomingSlash>(), (int)(jinsh * .8f), knockback, Main.myPlayer);
                }
                else
                {
					//2技能
					SoundEngine.PlaySound(yh2A, player.position);
					Projectile.NewProjectile(source, position
                  , (velocity * 1.4f).RotatedBy(bzd / 45f), ModContent.ProjectileType<SilverAshSlash>(), jinsh, knockback, Main.myPlayer, 0, 1);
                }
            }
            else if (modPlayer.Skill == 2)
            {
                if (!modPlayer.SkillActive)
                {
					//普通攻击
					SoundEngine.PlaySound(yhA, player.position);
					Projectile.NewProjectile(source, position
                    , (velocity * 1.4f).RotatedBy(bzd / 45f), ModContent.ProjectileType<SilverAshSlash>(), jinsh, knockback, Main.myPlayer);
                    int mm1 = Main.rand.Next(100, 401) * player.direction;
                    Vector2 velocity1 = (new Vector2(mm1, 655)).SafeNormalize(Vector2.Zero) * (22f);
                    Projectile.NewProjectile(source, Main.MouseWorld - new Vector2(mm1, 655)
                    , velocity1, ModContent.ProjectileType<SilverAshHomingSlash>(), (int)(jinsh * .8f), knockback, Main.myPlayer);
                }
                else
                {
                    //3技能
                    SoundEngine.PlaySound(zji3, player.position);
                    Projectile.NewProjectile(source, position
                    , (velocity * 1.4f), ModContent.ProjectileType<SilverAshTrueSilverSlash>(), jinsh * 3, knockback, Main.myPlayer);
                    Projectile.NewProjectile(source, position - velocity.SafeNormalize(Vector2.Zero) * 60
                         , (velocity * 1.4f), ModContent.ProjectileType<SilverAshS3Blade>(), jinsh * 3, knockback, Main.myPlayer);
                }
            }
            return false;
        }
    }
}