using ArknightsMod.Content.Items.Weapons;
using ArknightsMod.Players;
using ArknightsMod.Systems.Gameplay.Skill;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace ArknightsMod.Content.Items.Weapons.Sniper.Meteor
{
	public class MeteorBow : ExpansionWeaponBase
	{
		protected override int[] EliteDamage => [35, 42, 51];

		public override void SetDefaults() {
			Item.damage = EliteDamage[0];
			Item.DamageType = DamageClass.Ranged;
			Item.width = 24;
			Item.height = 56;
			Item.useTime = 30;
			Item.useAnimation = 30;
			Item.knockBack = 2f;
			Item.value = Item.sellPrice(silver: 30);
			Item.rare = ItemRarityID.Green;
			Item.autoReuse = true;
			Item.noMelee = true;
			Item.shoot = ProjectileID.WoodenArrowFriendly;
			Item.useAmmo = AmmoID.Arrow;
			Item.crit = 4;
			Item.shootSpeed = 12f;
			Item.useStyle = ItemUseStyleID.Shoot;
		}

		public override bool CanUseItem(Player player) {
			var mp = player.GetModPlayer<WeaponPlayer>();
			if (mp.CurrentSkill?.ChargeType == SkillChargeType.Attack && !mp.SkillActive)
				mp.OffensiveRecovery();
			return base.CanUseItem(player);
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			var mp = player.GetModPlayer<WeaponPlayer>();
			if (mp.StockCount > 0) {
				mp.DelStockCount();
				if (mp.Skill == 0) {
					// S1 碎甲击：180% 物理伤害，-35% 防御 5s
					damage = (int)(damage * 1.8f);
					NPC target = null; float dist = 600f;
					foreach (NPC npc in Main.npc) {
						if (!npc.active || npc.friendly) continue;
						float d = Vector2.Distance(npc.Center, player.Center);
						if (d < dist) { dist = d; target = npc; }
					}
					if (target != null) target.AddBuff(BuffID.BrokenArmor, 5 * 60);
				} else {
					// S2 碎甲击·扩散：200% 对最多5个敌人
					damage = (int)(damage * 2.0f);
					int hits = 0;
					foreach (NPC npc in Main.npc) {
						if (hits >= 5 || !npc.active || npc.friendly) continue;
						if (Vector2.Distance(npc.Center, player.Center) < 600f) {
							npc.AddBuff(BuffID.BrokenArmor, 5 * 60);
							hits++;
						}
					}
				}
			}
			return base.Shoot(player, source, position, velocity, type, damage, knockback);
		}
	}
}
