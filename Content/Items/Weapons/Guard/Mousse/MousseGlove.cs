using ArknightsMod.Content.Items.Weapons;
using ArknightsMod.Content.Projectiles.Guard.Mousse;
using ArknightsMod.Content.Projectiles.Guard.ArtsFighter;
using ArknightsMod.Players;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content;
using ArknightsMod.Content.Tiles.Infrastructure;
using Microsoft.Xna.Framework;
using Terraria.DataStructures;

namespace ArknightsMod.Content.Items.Weapons.Guard.Mousse
{
	public class MousseGlove : ExpansionWeaponBase
	{
		protected override int[] EliteDamage => [58, 72, 86];

		private static SoundStyle SkillActiveSfx;

		public override void Load() {
			SkillActiveSfx = new SoundStyle("ArknightsMod/Sounds/SkillActive1") { Volume = 0.5f, MaxInstances = 2 };
		}

		public override void SetDefaults() {
			Item.damage = EliteDamage[0];
			Item.DamageType = DamageClass.Melee;
			Item.width = 44;
			Item.height = 44;
			// Auger 的 12 帧基准放慢至 0.75 倍攻击频率。
			Item.useTime = ArtsFighterSwing.BaseUseTime;
			Item.useAnimation = ArtsFighterSwing.BaseUseTime;
			Item.knockBack = 4f;
			Item.value = Item.sellPrice(silver: 35);
			Item.rare = ItemRarityID.Green;
			Item.autoReuse = true;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.noMelee = true;
			Item.noUseGraphic = true;
			Item.shoot = ModContent.ProjectileType<MousseGlovePunch>();
			Item.shootSpeed = 16f;
			Item.UseSound = null; // 每次挥刃过半时由弹幕播放一次 Item68。
			Item.channel = true;
			Item.crit = 4;
		}

		public override bool AltFunctionUse(Player player) => false;

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
			Vector2 velocity, int type, int damage, float knockback) {
			Vector2 aim = velocity.SafeNormalize(new Vector2(player.direction, 0f));
			if (player.whoAmI == Main.myPlayer)
				Projectile.NewProjectile(source, player.MountedCenter, aim, type, damage, knockback,
					player.whoAmI, -player.itemAnimationMax, 0, player.itemAnimationMax);
			return false;
		}

		public override void AddRecipes() {
			CreateRecipe()
				.AddRecipeGroup(RecipeGroupID.IronBar, 12)
				.AddIngredient(ItemID.Silk, 6)
				.AddIngredient(ItemID.FallenStar, 3)
				.AddTile(ModContent.TileType<FactoryTile>())
				.Register();
		}

		public override bool CanUseItem(Player player) {
			var mp = player.GetModPlayer<WeaponPlayer>();

			if (ArknightsKeybinds.SkillActivatePressed(player)) {
				// S1 已改为自动释放，技能键对它不生效；此处只保留 S2 的手动开启
				if (mp.Skill == 1 && mp.StockCount > 0 && !mp.SkillActive) {
					mp.SkillActive = true;
					mp.SkillTimer = 0;
					mp.DelStockCount();
					SoundEngine.PlaySound(SkillActiveSfx, player.Center);
				}
				return false;
			}

			return player.ownedProjectileCounts[Item.shoot] == 0 && base.CanUseItem(player);
		}

		public override void ModifyWeaponDamage(Player player, ref StatModifier damage) {
			base.ModifyWeaponDamage(player, ref damage);
			var mp = player.GetModPlayer<WeaponPlayer>();
			if (mp.SkillActive)
				damage *= 1.75f; // S1/S2 攻击力 +75%
		}

		public override void HoldItem(Player player) {
			base.HoldItem(player);
			var mp = player.GetModPlayer<WeaponPlayer>();
			// 持续引导不再反复进入 CanUseItem，技能键也必须能在挥舞期间响应。
			if (ArknightsKeybinds.SkillActivatePressed(player) && mp.Skill == 1 && mp.StockCount > 0 && !mp.SkillActive) {
				mp.SkillActive = true;
				mp.SkillTimer = 0;
				mp.DelStockCount();
				SoundEngine.PlaySound(SkillActiveSfx, player.Center);
			}
			// S1「挠伤」改为自动释放：蓄满后下一次攻击自动变为强化（×1.75 并施加虚弱），无需手动按键。
			// !mp.SkillActive 充当"已上膛"标记，发出强化斩击后会置 false，待重新蓄满才会再次自动上膛，
			// 天然防止每帧重复触发。
			if (mp.Skill == 0 && mp.StockCount > 0 && !mp.SkillActive) {
				mp.SkillActive = true;
				mp.SkillTimer = 0;
				mp.DelStockCount();
				SoundEngine.PlaySound(SkillActiveSfx, player.Center);
			}
			// S2 炸毛：防御 +75%
			if (mp.SkillActive && mp.Skill == 1)
				player.statDefense *= 1.75f;
		}
	}

}
