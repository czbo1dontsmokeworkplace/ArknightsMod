using ArknightsMod.Content.Items.Weapons;
using ArknightsMod.Players;
using ArknightsMod.Systems.Gameplay.Skill;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace ArknightsMod.Content.Items.Weapons.Guard.Dobermann
{
	public class DobermannWhip : ExpansionWeaponBase
	{
		protected override int[] EliteDamage => [41, 52, 62];

		public override void SetDefaults() {
			Item.damage = EliteDamage[0];
			Item.DamageType = DamageClass.Melee;
			Item.width = 48;
			Item.height = 48;
			Item.useTime = 38;
			Item.useAnimation = 38;
			Item.knockBack = 4f;
			Item.value = Item.sellPrice(silver: 35);
			Item.rare = ItemRarityID.Green;
			Item.autoReuse = true;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.crit = 4;
		}

		public override bool CanUseItem(Player player) {
			var mp = player.GetModPlayer<WeaponPlayer>();
			if (mp.CurrentSkill?.ChargeType == SkillChargeType.Attack && !mp.SkillActive && mp.Skill == 0)
				mp.OffensiveRecovery();
			Item.damage = mp.SkillActive ? (int)(EliteDamage[EliteStage] * 1.8f) : EliteDamage[EliteStage];
			return base.CanUseItem(player);
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			var mp = player.GetModPlayer<WeaponPlayer>();
			// S1 强力击·B: 下次攻击230%，消耗库存
			if (mp.Skill == 0 && mp.StockCount > 0) {
				mp.DelStockCount();
				damage = (int)(damage * 2.30f);
			}
			return base.Shoot(player, source, position, velocity, type, damage, knockback);
		}
	}
}
