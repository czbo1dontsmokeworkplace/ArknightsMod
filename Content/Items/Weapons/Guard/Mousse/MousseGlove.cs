using ArknightsMod.Content.Items.Weapons;
using ArknightsMod.Players;
using ArknightsMod.Systems.Gameplay.Skill;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace ArknightsMod.Content.Items.Weapons.Guard.Mousse
{
	public class MousseGlove : ExpansionWeaponBase
	{
		protected override int[] EliteDamage => [43, 52, 62];

		public override void SetDefaults() {
			Item.damage = EliteDamage[0];
			Item.DamageType = DamageClass.Melee;
			Item.width = 44;
			Item.height = 44;
			Item.useTime = 22;
			Item.useAnimation = 22;
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
			Item.damage = mp.SkillActive && mp.Skill == 1
				? (int)(EliteDamage[EliteStage] * 1.75f)  // S2 炸毛：攻击力+75%，防御+75%
				: EliteDamage[EliteStage];
			return base.CanUseItem(player);
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			var mp = player.GetModPlayer<WeaponPlayer>();
			// S1 挠伤：攻击力+75%，命中目标攻击力-40% 5秒
			if (mp.Skill == 0 && mp.StockCount > 0) {
				mp.DelStockCount();
				damage = (int)(damage * 1.75f);
			}
			return base.Shoot(player, source, position, velocity, type, damage, knockback);
		}

		public override void ModifyHitNPC(Player player, NPC target, ref NPC.HitModifiers modifiers) {
			var mp = player.GetModPlayer<WeaponPlayer>();
			if (mp.Skill == 0 && !mp.SkillActive && mp.StockCount == 0)
				// S1 余效：命中后添加攻击力降低 debuff（使用已有 StunDebuff 暂替）
				target.AddBuff(BuffID.Weak, 5 * 60);
		}

		public override void HoldItem(Player player) {
			base.HoldItem(player);
			var mp = player.GetModPlayer<WeaponPlayer>();
			// S2 炸毛：防御+75%
			if (mp.SkillActive && mp.Skill == 1)
				player.statDefense *= 1.75f;
		}
	}
}
