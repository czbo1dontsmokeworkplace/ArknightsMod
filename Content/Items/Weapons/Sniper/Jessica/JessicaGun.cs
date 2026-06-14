using ArknightsMod.Content.Items.Weapons;
using ArknightsMod.Players;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace ArknightsMod.Content.Items.Weapons.Sniper.Jessica
{
	public class JessicaGun : ExpansionWeaponBase
	{
		protected override int[] EliteDamage => [37, 44, 53];

		private static SoundStyle SkillActiveSfx;

		public override void Load() {
			SkillActiveSfx = new SoundStyle("ArknightsMod/Sounds/SkillActive1") { Volume = 0.5f, MaxInstances = 2 };
		}

		public override void SetDefaults() {
			Item.damage = EliteDamage[0];
			Item.DamageType = DamageClass.Ranged;
			Item.width = 48;
			Item.height = 26;
			Item.useTime = 30;
			Item.useAnimation = 30;
			Item.knockBack = 3f;
			Item.value = Item.sellPrice(silver: 35);
			Item.rare = ItemRarityID.Green;
			Item.autoReuse = true;
			Item.noMelee = true;
			Item.shoot = ProjectileID.Bullet;
			Item.useAmmo = AmmoID.Bullet;
			Item.crit = 4;
			Item.shootSpeed = 14f;
			Item.useStyle = ItemUseStyleID.Shoot;
		}

		public override bool AltFunctionUse(Player player) => true;

		public override bool CanUseItem(Player player) {
			var mp = player.GetModPlayer<WeaponPlayer>();

			if (player.altFunctionUse == 2) {
				if (mp.StockCount > 0 && !mp.SkillActive) {
					mp.SkillActive = true;
					mp.SkillTimer = 0;
					mp.DelStockCount();
					SoundEngine.PlaySound(SkillActiveSfx, player.Center);
				}
				return false;
			}

			mp.OffensiveRecovery();
			return base.CanUseItem(player);
		}

		public override void ModifyWeaponDamage(Player player, ref StatModifier damage) {
			base.ModifyWeaponDamage(player, ref damage);
			var mp = player.GetModPlayer<WeaponPlayer>();
			if (!mp.SkillActive) return;
			if (mp.Skill == 0)
				damage *= 2.3f; // S1 强力击·B：230% 伤害
			else if (mp.Skill == 1)
				damage *= 1.8f; // S2 掩护烟幕：攻击力 +80%
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
				Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			var mp = player.GetModPlayer<WeaponPlayer>();
			// S1：子弹射出即消耗，伤害加成已由 ModifyWeaponDamage 生效
			if (mp.SkillActive && mp.Skill == 0)
				mp.SkillActive = false;
			return base.Shoot(player, source, position, velocity, type, damage, knockback);
		}

		public override void HoldItem(Player player) {
			base.HoldItem(player);
			var mp = player.GetModPlayer<WeaponPlayer>();
			// S2 掩护烟幕：75% 伤害减免（近似闪避效果）
			if (mp.SkillActive && mp.Skill == 1)
				player.endurance += 0.75f;
		}
	}
}
