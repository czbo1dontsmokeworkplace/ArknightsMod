using ArknightsMod.Content;
using ArknightsMod.Content.Items.Material;
using ArknightsMod.Content.Items.Weapons;
using ArknightsMod.Content.Projectiles.Guard.Astesia;
using ArknightsMod.Content.Projectiles.Guard.ArtsFighter;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Weapons.Guard.Mousse;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Guard.Astesia
{
	/// <summary>
	/// 星辉剑：沿用术战者的一顺一逆双斩节拍，发射天蓝色能量弧光。
	/// 剑身、斩击和技能范围缩放共用当前武器尺寸。
	///
	/// 技能：
	///   一技能「星座守护」（Skill 0）：手动开启，技力 10/30，持续 30 秒。
	///       开启期间攻击力 +50%、防御力 +80%。
	///   二技能「星辉剑」（Skill 1）：手动开启，技力 10/20，持续 15 秒。
	///       开启期间攻击力 +80%、防御力 +80%，武器与弧光弹幕放大至 130%，
	///       并在玩家身后召唤一个跟随移动的星辉光环（AstraSwordAura，64×64），
	///       技能结束或切换武器时消失。
	/// </summary>
	public class AstraSword : UpgradeWeaponBase
	{
		private static SoundStyle skillSound;

		public override void Load() {
			skillSound = new SoundStyle("ArknightsMod/Sounds/SkillActive1") {
				Volume = 0.5f,
				MaxInstances = 2
			};
		}

		public override void Unload() => skillSound = default;

		public override void SetDefaults() {
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.useAnimation = ArtsFighterSwing.BaseUseTime;
			Item.useTime = ArtsFighterSwing.BaseUseTime;
			Item.damage = 182;
			Item.DamageType = DamageClass.Melee;
			Item.knockBack = 4.5f;
			Item.width = 78;
			Item.height = 78;
			Item.scale = 1f;
			Item.UseSound = null;
			Item.rare = ItemRarityID.Lime;
			Item.value = Item.buyPrice(gold: 23);
			Item.shoot = ModContent.ProjectileType<AstraSwordHoldout>();
			Item.shootSpeed = 1f;
			Item.noMelee = true;
			Item.noUseGraphic = true;
			Item.channel = true;
			Item.autoReuse = true;
		}

		public override bool AltFunctionUse(Player player) => false;

		public override bool CanUseItem(Player player) {
			if (Main.myPlayer != player.whoAmI)
				return base.CanUseItem(player);

			var wp = player.GetModPlayer<WeaponPlayer>();

			// 技能键按下 → 激活当前选中的技能（一/二技能共用激活流程）
			if (ArknightsKeybinds.SkillActivatePressed(player)) {
				if (wp.StockCount > 0 && !wp.SkillActive) {
					wp.SkillActive = true;
					wp.SkillTimer = 0;
					wp.DelStockCount();
					if (!Main.dedServ)
						SoundEngine.PlaySound(skillSound, player.Center);
				}
				return false;
			}

			return player.ownedProjectileCounts[Item.shoot] == 0 && base.CanUseItem(player);
		}

		// 二技能「星辉剑」激活期间：
		//  · 武器模型放大到 130%，持握弹幕每帧读取尺寸，斩击与判定同步放大；
		//  · 本地玩家召唤背后跟随的星辉光环（光环的消失由弹幕自行逐帧判定）。
		public override void HoldItem(Player player) {
			base.HoldItem(player);
			var wp = player.GetModPlayer<WeaponPlayer>();
			// Channelled attacks do not repeatedly enter CanUseItem.
			if (player.whoAmI == Main.myPlayer && ArknightsKeybinds.SkillActivatePressed(player)
				&& wp.StockCount > 0 && !wp.SkillActive) {
				wp.SkillActive = true;
				wp.SkillTimer = 0;
				wp.DelStockCount();
				if (!Main.dedServ) SoundEngine.PlaySound(skillSound, player.Center);
			}
			Item.scale = (wp.SkillActive && wp.Skill == 1) ? 1.3f : 1f;

			if (player.whoAmI == Main.myPlayer && wp.SkillActive && wp.Skill == 1)
				EnsureAura(player);
		}

		private static void EnsureAura(Player player) {
			int type = ModContent.ProjectileType<AstraSwordAura>();
			if (player.ownedProjectileCounts[type] <= 0)
				Projectile.NewProjectile(player.GetSource_FromThis(), player.Center,
					Vector2.Zero, type, 0, 0f, player.whoAmI);
		}

		// 攻击力加成：星座守护（Skill 0）+50%；星辉剑（Skill 1）+80%
		public override void ModifyWeaponDamage(Player player, ref StatModifier damage) {
			var wp = player.GetModPlayer<WeaponPlayer>();
			if (!wp.SkillActive)
				return;

			if (wp.Skill == 0)
				damage *= 1.5f;
			else if (wp.Skill == 1)
				damage *= 1.8f;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            if (player.whoAmI == Main.myPlayer) {
                Vector2 aim = (Main.MouseWorld - player.MountedCenter).SafeNormalize(new Vector2(player.direction, 0f));
                Projectile.NewProjectile(source, player.MountedCenter, aim, type, damage, knockback,
                    player.whoAmI, -player.itemAnimationMax, 0f, player.itemAnimationMax);
            }
            return false;
        }

		public override void AddRecipes() {
			CreateRecipe()
				.AddIngredient<MousseGlove>()
				.AddIngredient<global::ArknightsMod.Content.Items.Material.PolyesterLump>(1)
				.AddIngredient<global::ArknightsMod.Content.Items.Material.OrironCluster>(2)
				.AddIngredient(ItemID.Starfury)
				.AddIngredient(ItemID.HallowedBar, 8)
				.AddIngredient(ItemID.FallenStar, 5)
				.AddTile(ModContent.TileType<FactoryTile>())
				.Register();
		}
	}

	/// <summary>
	/// 星辉剑专属玩家效果：技能开启期间防御力 +80%
	/// （星座守护与星辉剑两个技能的防御加成相同，共用此判断）。
	/// 加成写入 DefenseStat 的百分比加算层，收益恒为 +80%：写成
	/// <c>statDefense += (int)(statDefense * 0.8f)</c> 会把"已含乘法的总值"当平铺值再加一次，
	/// 再被乘法层放大（例如场上另有 ×1.7 时，实际收益会从 +80% 变成 +301%）。
	/// </summary>
	public class AstraSwordPlayer : ModPlayer
	{
		public override void PostUpdateEquips() {
			var wp = Player.GetModPlayer<WeaponPlayer>();
			if (Player.HeldItem?.ModItem is AstraSword && wp.SkillActive
				&& (wp.Skill == 0 || wp.Skill == 1))
				Player.statDefense.AdditiveBonus += 0.8f; // 防御力 +80%
		}
	}
}
