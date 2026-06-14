using ArknightsMod.Content.Items.Weapons;
using ArknightsMod.Content.Projectiles.Caster.Goldenglow;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Caster.Goldenglow
{
	public class GoldenglowWand : ExpansionWeaponBase
	{
		protected override int[] EliteDamage => [30, 36, 41];

		private static SoundStyle SkillActiveSfx;

		public override void Load() {
			SkillActiveSfx = new SoundStyle("ArknightsMod/Sounds/SkillActive1") { Volume = 0.5f, MaxInstances = 2 };
		}

		public override void SetDefaults() {
			Item.damage = EliteDamage[0];
			Item.DamageType = DamageClass.Magic;
			Item.width = 54;
			Item.height = 54;
			Item.useTime = 23;
			Item.useAnimation = 23;
			Item.knockBack = 2f;
			Item.value = Item.sellPrice(gold: 1);
			Item.rare = ItemRarityID.Green;
			Item.autoReuse = true;
			Item.noMelee = true;
			Item.shoot = ProjectileID.MagicMissile;  // 原版可引导导弹特效
			Item.mana = 8;
			Item.crit = 4;
			Item.shootSpeed = 10f;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.channel = true;
			Item.staff[Item.type] = true;
		}

		public override bool AltFunctionUse(Player player) => true;

		public override bool CanUseItem(Player player) {
			var mp = player.GetModPlayer<WeaponPlayer>();

			if (player.altFunctionUse == 2) {
				if (player.controlDown) {
					// 下+右键：激活当前选中技能
					if (mp.StockCount > 0 && !mp.SkillActive) {
						mp.SkillActive = true;
						mp.SkillTimer = 0;
						mp.DelStockCount();
						SoundEngine.PlaySound(SkillActiveSfx, player.Center);
					}
				} else {
					// 右键：在光标位置部署浮游信标（最多 6 个）
					if (player.ownedProjectileCounts[ModContent.ProjectileType<GoldenglowBeacon>()] < GoldenglowBeacon.MaxBeacons) {
						Projectile.NewProjectile(
							player.GetSource_ItemUse(Item),
							Main.MouseWorld,
							Vector2.Zero,
							ModContent.ProjectileType<GoldenglowBeacon>(),
							0, 0f, player.whoAmI);
					}
				}
				return false;
			}

			return base.CanUseItem(player);
		}

		public override void ModifyWeaponDamage(Player player, ref StatModifier damage) {
			base.ModifyWeaponDamage(player, ref damage);
			var mp = player.GetModPlayer<WeaponPlayer>();
			if (mp.SkillActive) {
				damage *= mp.Skill switch {
					0 => 1.4f,
					1 => 1.6f,
					2 => 1.8f,
					_ => 1f
				};
			}
		}
	}
}
