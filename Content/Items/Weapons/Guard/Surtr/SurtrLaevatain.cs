using ArknightsMod.Content.Buffs;
using ArknightsMod.Content.Items.Material;
using ArknightsMod.Content.Projectiles.Guard.Laevatain;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Guard.Surtr
{
	public class SurtrLaevatain : UpgradeWeaponBase
	{
		public static SoundStyle SkillActiveSound;

		public override void SetDefaults()
		{
			Item.damage = 134;
			Item.DamageType = DamageClass.Melee;
			Item.width = 64;
			Item.height = 70;
			Item.useTime = 37;
			Item.useAnimation = 37;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.knockBack = 10;
			Item.value = Item.sellPrice(silver: 3000);
			Item.noUseGraphic = true;
			Item.noMelee = true;
			Item.shoot = ModContent.ProjectileType<LaevatainProjectile_normal>();
			Item.shootSpeed = 1f;
			Item.rare = ItemRarityID.Red;
			Item.UseSound = SoundID.Item1;
			Item.autoReuse = true;
		}

		public override void Load()
		{
			SkillActiveSound = new SoundStyle("ArknightsMod/Sounds/SkillActive1")
			{
				Volume = 0.4f,
				MaxInstances = 4,
			};
		}

		public override void AddRecipes()
		{
			Recipe recipe = CreateRecipe()
				.AddIngredient<global::ArknightsMod.Content.Items.Material.PolymerizationPreparation>(1)
				.AddIngredient<global::ArknightsMod.Content.Items.Material.KetonColloid>(2)
				.AddIngredient(ItemID.TrueNightsEdge)
				.AddIngredient(ItemID.HellstoneBar, 12)
				.AddIngredient(ItemID.LavaBucket);
			recipe.AddTile(ModContent.TileType<FactoryTile>());
			recipe.Register();
		}

		public override bool AltFunctionUse(Player player) => true;

		public override bool Shoot(
			Player player,
			EntitySource_ItemUse_WithAmmo source,
			Vector2 position,
			Vector2 velocity,
			int type,
			int damage,
			float knockback
		)
		{
			return false;
		}

		public override bool CanUseItem(Player player)
		{
			if (Main.myPlayer != player.whoAmI)
				return base.CanUseItem(player);

			var modPlayer = player.GetModPlayer<WeaponPlayer>();

			if (ArknightsKeybinds.SkillActivatePressed(player))
			{
				//其实好像写成一个就行了
				if (modPlayer.Skill == 1 && modPlayer.StockCount > 0 && !modPlayer.SkillActive)
				{
					modPlayer.SkillActive = true;
					modPlayer.SkillTimer = 0;
					modPlayer.DelStockCount();
					SoundEngine.PlaySound(SkillActiveSound, player.Center);
					return false;
				}
				else if (modPlayer.Skill == 2 && modPlayer.StockCount > 0 && !modPlayer.SkillActive)
				{
					modPlayer.SkillActive = true;
					modPlayer.SkillTimer = 0;
					modPlayer.DelStockCount();
					SoundEngine.PlaySound(SkillActiveSound, player.Center);
					// 持续效果只在手持本武器并保持三技能开启时生效。
					player.AddBuff(ModContent.BuffType<SurtrLaevatainS3Buff>(), SurtrLaevatainS3Buff.InitialDuration);
					player.GetModPlayer<SurtrLaevatain_Player>()
						.StartTransformationFire(new Color(255, 40, 8));
					return false;
				}
				return false;
			}
			return true;
		}

		public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
		{
			if (Main.myPlayer != player.whoAmI)
				return;
			var modPlayer = player.GetModPlayer<WeaponPlayer>();
			if (modPlayer.Skill == 0 && modPlayer.SkillActive)
				damage *= 3.1f;
			if (modPlayer.Skill == 1 && modPlayer.SkillActive)
				damage *= 2.2f; // 只命中一个敌人时 ×1.5 的加成在 LaevatainProjectile_2 里实现
			if (modPlayer.Skill == 2 && modPlayer.SkillActive)
				damage *= 4.3f;
		}
	}
}
