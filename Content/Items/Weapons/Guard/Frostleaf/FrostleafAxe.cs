using ArknightsMod.Content.Items.Weapons;
using ArknightsMod.Players;
using ArknightsMod.Systems.Gameplay.Skill;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace ArknightsMod.Content.Items.Weapons.Guard.Frostleaf
{
	public class FrostleafAxe : ExpansionWeaponBase
	{
		protected override int[] EliteDamage => [40, 48, 58];

		public override void SetDefaults() {
			Item.damage = EliteDamage[0];
			Item.DamageType = DamageClass.Melee;
			Item.width = 60;
			Item.height = 62;
			Item.useTime = 31;
			Item.useAnimation = 31;
			Item.knockBack = 7f;
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
			Item.damage = mp.SkillActive ? (int)(EliteDamage[EliteStage] * 1.5f) : EliteDamage[EliteStage];
			return base.CanUseItem(player);
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			var mp = player.GetModPlayer<WeaponPlayer>();
			if (mp.Skill == 0 && mp.StockCount > 0) {
				mp.DelStockCount();
				damage = (int)(damage * 1.5f);
				// S1 寒霜枪刃：命中后减速（依赖 NPC 冻结效果暂用普通击退）
			}
			return base.Shoot(player, source, position, velocity, type, damage, knockback);
		}

		public override void ModifyHitNPC(Player player, NPC target, ref NPC.HitModifiers modifiers) {
			var mp = player.GetModPlayer<WeaponPlayer>();
			if (mp.SkillActive && mp.Skill == 1)
				// S2 凝冰枪刃：每次攻击减速 50%，40% 概率束缚 2 秒
				target.AddBuff(BuffID.Slow, 3 * 60);
		}
	}
}
