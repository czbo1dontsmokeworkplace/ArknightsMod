using ArknightsMod.Content.Items.Weapons;
using ArknightsMod.Players;
using ArknightsMod.Systems.Gameplay.Skill;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace ArknightsMod.Content.Items.Weapons.Specialist.Shaw
{
	public class ShawAxe : ExpansionWeaponBase
	{
		protected override int[] EliteDamage => [39, 47, 57];

		public override void SetDefaults() {
			Item.damage = EliteDamage[0];
			Item.DamageType = DamageClass.Melee;
			Item.width = 52;
			Item.height = 52;
			Item.useTime = 30;
			Item.useAnimation = 30;
			Item.knockBack = 6f;
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
			if (mp.StockCount > 0) {
				mp.DelStockCount();
				damage = mp.Skill == 0
					? (int)(damage * 1.5f)
					: (int)(damage * 3.0f);
				NPC target = FindNearestNPC(player);
				if (target != null)
					target.velocity += velocity.SafeNormalize(Vector2.Zero) * (mp.Skill == 0 ? 8f : 14f);
			}
			return base.Shoot(player, source, position, velocity, type, damage, knockback);
		}

		private static NPC FindNearestNPC(Player player) {
			NPC best = null;
			float dist = 300f;
			foreach (NPC npc in Main.npc) {
				if (!npc.active || npc.friendly) continue;
				float d = Vector2.Distance(npc.Center, player.Center);
				if (d < dist) { dist = d; best = npc; }
			}
			return best;
		}
	}
}
