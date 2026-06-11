using ArknightsMod.Content.Items.Weapons;
using ArknightsMod.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Supporter.Earthspirit
{
	public class EarthspiritWand : ExpansionWeaponBase
	{
		protected override int[] EliteDamage => [37, 57, 74];

		public override void SetDefaults() {
			Item.damage = EliteDamage[0];
			Item.DamageType = DamageClass.Magic;
			Item.width = 46;
			Item.height = 80;
			Item.useTime = 29;
			Item.useAnimation = 29;
			Item.knockBack = 2f;
			Item.value = Item.sellPrice(silver: 40);
			Item.rare = ItemRarityID.Green;
			Item.autoReuse = true;
			Item.noMelee = true;
			Item.shoot = ProjectileID.MagicMissile;
			Item.mana = 7;
			Item.crit = 4;
			Item.shootSpeed = 9f;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.staff[Item.type] = true;
		}

		public override bool CanUseItem(Player player) {
			var mp = player.GetModPlayer<WeaponPlayer>();
			Item.damage = mp.SkillActive ? (int)(EliteDamage[EliteStage] * 1.8f) : EliteDamage[EliteStage];
			// S2 流沙化：停止攻击，改为对范围内敌人施加减速（停止射击直接返回 false）
			if (mp.SkillActive && mp.Skill == 1) return false;
			return base.CanUseItem(player);
		}

		public override void HoldItem(Player player) {
			base.HoldItem(player);
			var mp = player.GetModPlayer<WeaponPlayer>();
			// S2 流沙化：每 1.4 秒对附近敌人施加停顿效果
			if (mp.SkillActive && mp.Skill == 1 && Main.GameUpdateCount % 84 == 0) {
				foreach (NPC npc in Main.npc) {
					if (!npc.active || npc.friendly) continue;
					if (Terraria.Utils.Distance(npc.Center, player.Center) < 200f)
						npc.AddBuff(BuffID.Slow, 90);
				}
			}
		}
	}
}
