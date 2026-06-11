using ArknightsMod.Content.Items.Weapons;
using ArknightsMod.Players;
using ArknightsMod.Systems.Gameplay.Skill;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace ArknightsMod.Content.Items.Weapons.Sniper.Jessica
{
	public class JessicaGun : ExpansionWeaponBase
	{
		protected override int[] EliteDamage => [37, 44, 53];

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

		public override bool CanUseItem(Player player) {
			var mp = player.GetModPlayer<WeaponPlayer>();
			if (mp.CurrentSkill?.ChargeType == SkillChargeType.Attack && !mp.SkillActive && mp.Skill == 0)
				mp.OffensiveRecovery();
			Item.damage = mp.SkillActive && mp.Skill == 1
				? (int)(EliteDamage[EliteStage] * 1.8f)  // S2 掩护烟幕：攻击力+80%
				: EliteDamage[EliteStage];
			return base.CanUseItem(player);
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			var mp = player.GetModPlayer<WeaponPlayer>();
			if (mp.Skill == 0 && mp.StockCount > 0) {
				mp.DelStockCount();
				damage = (int)(damage * 2.3f);  // S1 强力击·B
			}
			return base.Shoot(player, source, position, velocity, type, damage, knockback);
		}

		public override void HoldItem(Player player) {
			base.HoldItem(player);
			var mp = player.GetModPlayer<WeaponPlayer>();
			// S2 掩护烟幕：伤害减免50%（近似闪避效果）
			if (mp.SkillActive && mp.Skill == 1)
				player.endurance += 0.5f;
		}
	}
}
