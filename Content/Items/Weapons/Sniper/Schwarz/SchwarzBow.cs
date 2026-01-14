using ArknightsMod.Content.Projectiles.Sniper.Schwarz;
using ArknightsMod.Content.Items.Material;
using ArknightsMod.Content.Tiles;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using System;
using Terraria.DataStructures;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

using static Mono.CompilerServices.SymbolWriter.CodeBlockEntry;

namespace ArknightsMod.Content.Items.Weapons.Sniper.Schwarz
{
    public class SchwarzBow : UpgradeWeaponBase
    {
        private static SoundStyle AttackSound;
		private static SoundStyle Skill2Sound;
        private static SoundStyle Skill3Sound;
		private static SoundStyle SkillActive1;

		public override void AddRecipes() {
			Recipe recipe = CreateRecipe();
			recipe.AddIngredient<Material.D32Steel>(4);
			recipe.AddIngredient<Material.OrironBlock>(5);
			recipe.AddTile(ModContent.TileType<FactoryTile>());
			recipe.Register();
		}
		public override void SetDefaults()
        {
            Item.width = 62;
            Item.height = 32;
			Item.rare = ItemRarityID.Orange; 
            Item.useTime = 48;
            Item.useAnimation = 48;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.value = Item.sellPrice(0, 40, 30, 0);
            Item.DamageType = DamageClass.Ranged;
            Item.damage = 168;
            Item.knockBack = 10f; 
            Item.noMelee = true;
            Item.shoot = ModContent.ProjectileType<SchwarzArrow>();
            Item.shootSpeed = 20f;
        }
        public override void Load() {
			AttackSound = new SoundStyle("ArknightsMod/Content/Items/Weapons/Sniper/Schwarz/SchwarzAttackSound") {
				Volume = 0.4f,
				MaxInstances = 4,
			};
			Skill2Sound = new SoundStyle("ArknightsMod/Content/Items/Weapons/Sniper/Schwarz/SchwarzSkill2Sound") {
				Volume = 0.4f,
				MaxInstances = 4,
			};
            Skill3Sound = new SoundStyle("ArknightsMod/Content/Items/Weapons/Sniper/Schwarz/SchwarzSkill3Sound") {
				Volume = 0.4f,
				MaxInstances = 4,
			};
			SkillActive1 = new SoundStyle("ArknightsMod/Sounds/SkillActive1") {
				Volume = 0.4f,
				MaxInstances = 4,
			};
		}
		public override bool AltFunctionUse(Player player) => true;
        public override bool CanUseItem(Player player) {
			var modPlayer = Main.LocalPlayer.GetModPlayer<WeaponPlayer>();
			if (Main.myPlayer == player.whoAmI) {
				if (player.altFunctionUse == 2) {
					if (!modPlayer.SummonMode) {
						//S2
						if (modPlayer.Skill == 1 && modPlayer.StockCount > 0 && !modPlayer.SkillActive) {
							modPlayer.SkillActive = true;
							modPlayer.SkillTimer = 0;

							modPlayer.DelStockCount();

							Item.UseSound = SkillActive1;
							SoundEngine.PlaySound(Item.UseSound.Value, player.Center);
						}
                        else if (modPlayer.Skill == 2 && modPlayer.StockCount > 0 && !modPlayer.SkillActive) {
							modPlayer.SkillActive = true;
							modPlayer.SkillTimer = 0;
							modPlayer.DelStockCount();
							Item.UseSound = SkillActive1;
							SoundEngine.PlaySound(Item.UseSound.Value, player.Center);
						}
						else
							return false;
					}
				}
				else {
					// S1
					if (!modPlayer.SummonMode && modPlayer.Skill == 0 && modPlayer.StockCount > 0 && !modPlayer.SkillActive) {
						modPlayer.SkillActive = true;
						modPlayer.SkillTimer = 0;
						modPlayer.DelStockCount();
						Item.UseSound = SkillActive1;
						SoundEngine.PlaySound(Item.UseSound.Value, player.Center);
					}
					if (!modPlayer.SummonMode) {
						Item.UseSound = AttackSound;
					}
				}
			}
			return base.CanUseItem(player);
		}
		public override void ModifyWeaponDamage(Player player, ref StatModifier damage) {
			var modPlayer = Main.LocalPlayer.GetModPlayer<WeaponPlayer>();
			if (Main.myPlayer == player.whoAmI) {
				if (modPlayer.Skill == 0 && modPlayer.SkillActive == true) {
                    if (Main.rand.NextFloat() < 0.2f)
					    damage *= 2.2f;
                    else{
                        damage *= 2.2f*1.6f;
                    }
				}
                else if (modPlayer.Skill == 1 && modPlayer.SkillActive == true) {
					if (Main.rand.NextFloat() < 0.5f)
					    damage *= 2.3f;
                    else{
                        damage *= 2.3f*1.6f;
                    }
				}
                else if (modPlayer.Skill == 2 && modPlayer.SkillActive == true) {
					damage *= 2.8f*1.6f;
				}
			}
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
			var modPlayer = Main.LocalPlayer.GetModPlayer<WeaponPlayer>();
            Projectile.NewProjectileDirect(source, position, velocity,
				ModContent.ProjectileType<SchwarzArrow>(), damage, knockback, player.whoAmI);
			if (modPlayer.Skill == 0 && modPlayer.StockCount == 0 && !modPlayer.SkillActive){
				modPlayer.OffensiveRecovery();
			}
			else if (modPlayer.Skill == 1 && modPlayer.SkillActive){
				Item.UseSound = Skill2Sound;
				SoundEngine.PlaySound(Item.UseSound.Value, player.Center);
			}
			else if (modPlayer.Skill == 2 && modPlayer.SkillActive){
				Item.UseSound = Skill3Sound;
				SoundEngine.PlaySound(Item.UseSound.Value, player.Center);
			}
            return false;
        }
        public override Vector2? HoldoutOffset()
        {
            return new Vector2(0, 4);
        }

    }
}