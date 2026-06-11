using ArknightsMod.Content.Items.Weapons;
using ArknightsMod.Players;
using ArknightsMod.Systems.Gameplay.Skill;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace ArknightsMod.Content.Items.Weapons.Supporter.Ansel
{
	public class AnselClaw : ExpansionWeaponBase
	{
		protected override int[] EliteDamage => [44, 53, 64];

		public override void SetDefaults() {
			Item.damage = EliteDamage[0];
			Item.DamageType = DamageClass.Melee;
			Item.width = 46;
			Item.height = 46;
			Item.useTime = 22;
			Item.useAnimation = 22;
			Item.knockBack = 5f;
			Item.value = Item.sellPrice(silver: 35);
			Item.rare = ItemRarityID.Green;
			Item.autoReuse = true;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.crit = 4;
		}

		public override bool CanUseItem(Player player) {
			var mp = player.GetModPlayer<WeaponPlayer>();
			if (mp.CurrentSkill?.ChargeType == SkillChargeType.Attack && !mp.SkillActive)
				mp.OffensiveRecovery();
			return base.CanUseItem(player);
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			var mp = player.GetModPlayer<WeaponPlayer>();
			if (mp.Skill == 0 && mp.StockCount > 0) {
				mp.DelStockCount();
				// S1 勾爪发射：190% 伤害，拉扯最近敌人
				damage = (int)(damage * 1.9f);
				NPC target = FindNearestNPC(player);
				if (target != null)
					target.velocity += (player.Center - target.Center) * 0.2f;
			} else if (mp.Skill == 1 && mp.StockCount > 0) {
				mp.DelStockCount();
				// S2 复式勾爪：225% 伤害，拉扯2个敌人
				damage = (int)(damage * 2.25f);
				NPC t1 = FindNearestNPC(player);
				if (t1 != null) t1.velocity += (player.Center - t1.Center) * 0.25f;
				NPC t2 = FindNearestNPC(player, t1);
				if (t2 != null) t2.velocity += (player.Center - t2.Center) * 0.25f;
			}
			return base.Shoot(player, source, position, velocity, type, damage, knockback);
		}

		private static NPC FindNearestNPC(Player player, NPC exclude = null) {
			NPC best = null;
			float dist = 800f;
			foreach (NPC npc in Main.npc) {
				if (!npc.active || npc.friendly || npc == exclude) continue;
				float d = Vector2.Distance(npc.Center, player.Center);
				if (d < dist) { dist = d; best = npc; }
			}
			return best;
		}
	}
}
