using ArknightsMod.Content.Projectiles.Defender.Nian;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent.Creative;
using Terraria.Audio;
using ArknightsMod.Content.Buffs;
using System;
using ArknightsMod.Players;
using ArknightsMod.Content.Tiles;
using ArknightsMod.Content.Tiles.Infrastructure;
using Terraria.Audio;



namespace ArknightsMod.Content.Items.Weapons.Defender.Nian
{

	public class NianWeapon : UpgradeItemBase
	{
		private static SoundStyle SkillActive1;
		private static SoundStyle NoSound;

		public override void SetStaticDefaults() {
			ItemID.Sets.Spears[Item.type] = true;
			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
		}

		public override void AddRecipes() {
			Recipe recipe = CreateRecipe();
			recipe.AddIngredient<Material.PolymerizationPreparation>(4);
			recipe.AddIngredient<Material.IncandescentAlloyBlock>(7);
			recipe.AddTile(ModContent.TileType<FactoryTile>());
			recipe.Register();
		}

		public override void SetDefaults() {
			Item.rare = ItemRarityID.Orange; 
			Item.value = Item.sellPrice(0, 40, 0, 0);
			Item.consumable = false;

			// Use Properties
			Item.useStyle = ItemUseStyleID.Swing; 
			Item.useAnimation = 45;
			Item.useTime = 45;
			Item.autoReuse = true; 

			// Weapon Properties
			Item.damage = 124;
			Item.knockBack = 2.5f;
			Item.noUseGraphic = true; 
			Item.DamageType = DamageClass.Melee;
			Item.noMelee = true;
			Item.channel = true;
			Item.crit = 21; 

			// Projectile Properties
			Item.shootSpeed = 3.3f; 
			Item.shoot = ModContent.ProjectileType<NianSword_Projectile>(); 


		}

		public override bool AltFunctionUse(Player player) => true;

		public override void Load() {
			SkillActive1 = new SoundStyle("ArknightsMod/Sounds/SkillActive1") {
				Volume = 0.4f,
				MaxInstances = 4,
			};
			NoSound = new SoundStyle("ArknightsMod/Sounds/NoSound") {
				Volume = 0f,
				MaxInstances = 4,
			};
		}

		public override bool CanUseItem(Player player) {
			var modPlayer = Main.LocalPlayer.GetModPlayer<WeaponPlayer>();
			if (Main.myPlayer == player.whoAmI) {
				if (player.altFunctionUse == 2) {
					// S1
					if (modPlayer.Skill == 0  && !modPlayer.SkillActive) {
						modPlayer.SkillActive = true;
						modPlayer.SkillTimer = 0;

						modPlayer.DelStockCount();

						Item.UseSound = new SoundStyle("ArknightsMod/Sounds/SkillActive1") {
							Volume = 0.6f,
							MaxInstances = 4, //This dicatates how many instances of a sound can be playing at the same time. The default is 1. Adjust this to allow overlapping sounds.
						};
						SoundEngine.PlaySound(Item.UseSound.Value, player.Center);
					}
					// S3
					if (modPlayer.Skill == 2 && modPlayer.StockCount > 0 && !modPlayer.SkillActive) {
						modPlayer.SkillActive = true;
						modPlayer.SkillTimer = 0;

						modPlayer.DelStockCount();

						Item.UseSound = new SoundStyle("ArknightsMod/Sounds/SkillActive2") {
							Volume = 0.4f,
							MaxInstances = 4, //This dicatates how many instances of a sound can be playing at the same time. The default is 1. Adjust this to allow overlapping sounds.
						};
						SoundEngine.PlaySound(Item.UseSound.Value, player.Center);
					}

					else
						return false;
				}
				else {
					Item.useAnimation = 30;
					Item.useTime = 30; // If you want to attack triple hit, useTime = useAnimation/3
					Item.UseSound = new SoundStyle("ArknightsMod/Sounds/BagpipeSpearS0") {
						Volume = 0.4f,
						MaxInstances = 4, //This dicatates how many instances of a sound can be playing at the same time. The default is 1. Adjust this to allow overlapping sounds.
					};

					// S1
					if (modPlayer.Skill == 0 && modPlayer.SkillActive) {
						Item.useAnimation = 22;
						Item.useTime = 22;
						Item.UseSound = new SoundStyle("ArknightsMod/Sounds/BagpipeSpearS0") {
							Volume = 0.4f,
							MaxInstances = 4, //This dicatates how many instances of a sound can be playing at the same time. The default is 1. Adjust this to allow overlapping sounds.
						};
					}
					// S2
					if (modPlayer.Skill == 1 && modPlayer.StockCount > 0) {
						Item.useTime = 15;
						Item.UseSound = new SoundStyle("ArknightsMod/Sounds/BagpipeSpearS2") {
							Volume = 0.4f,
							MaxInstances = 4, //This dicatates how many instances of a sound can be playing at the same time. The default is 1. Adjust this to allow overlapping sounds.
						};
						modPlayer.SkillActive = true;
						modPlayer.SkillTimer = 0;
						modPlayer.DelStockCount();
					}
					// S3
					if (modPlayer.Skill == 2 && modPlayer.SkillActive) {
						Item.useAnimation = 48;
						Item.useTime = 16;
						Item.UseSound = new SoundStyle("ArknightsMod/Sounds/BagpipeSpearS3") {
							Volume = 0.4f,
							MaxInstances = 4, //This dicatates how many instances of a sound can be playing at the same time. The default is 1. Adjust this to allow overlapping sounds.
						};
					}
				}
			}
			// Ensures no more than one spear can be thrown out, use this when using autoReuse
			return player.ownedProjectileCounts[Item.shoot] < 1;
		}

		public override void ModifyWeaponDamage(Player player, ref StatModifier damage) {
			var modPlayer = Main.LocalPlayer.GetModPlayer<WeaponPlayer>();
			if (Main.myPlayer == player.whoAmI) {
				// S1
				if (modPlayer.Skill == 0 && modPlayer.SkillActive) {
					damage *= 1.45f;
				}
				// S2
				if (modPlayer.Skill == 1 && (modPlayer.StockCount > 0 || modPlayer.SkillActive == true)) {
					damage *= 2f;
				}
				// S3
				if (modPlayer.Skill == 2 && modPlayer.SkillActive) {
					damage *= 2.2f;
				}
			}
		}

		public override void HoldItem(Player player) {
			var modPlayer = Main.LocalPlayer.GetModPlayer<WeaponPlayer>();
			if (Main.myPlayer == player.whoAmI) {
				
				
				modPlayer.HoldBagpipeSpear = true; // you have to write this line HERE!
			}
			base.HoldItem(player);
		}


	}
}