using ArknightsMod.Content.Items.Weapons;
using ArknightsMod.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Guard.Beehunter
{
	public class BeehunterBag : ExpansionWeaponBase
	{
		protected override int[] EliteDamage => [23, 28, 34];

		public override void SetDefaults() {
			Item.damage = EliteDamage[0];
			Item.DamageType = DamageClass.Melee;
			Item.width = 48;
			Item.height = 48;
			Item.useTime = 20;
			Item.useAnimation = 20;
			Item.knockBack = 4f;
			Item.value = Item.sellPrice(silver: 20);
			Item.rare = ItemRarityID.Green;
			Item.autoReuse = true;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.crit = 4;
		}

		public override bool CanUseItem(Player player) {
			var mp = player.GetModPlayer<WeaponPlayer>();
			if (mp.SkillActive) {
				Item.damage = mp.Skill == 0
					? (int)(EliteDamage[EliteStage] * 1.8f)   // S1 防御力+80%（实际：对敌攻击力+80%）
					: EliteDamage[EliteStage];                  // S2 壳状防御：停止攻击，防御+130%，回血
			} else {
				Item.damage = EliteDamage[EliteStage];
			}
			// S2 壳状防御：停止攻击
			if (mp.SkillActive && mp.Skill == 1) return false;
			return base.CanUseItem(player);
		}

		public override void HoldItem(Player player) {
			base.HoldItem(player);
			var mp = player.GetModPlayer<WeaponPlayer>();
			if (mp.SkillActive) {
				if (mp.Skill == 0) {
					// S1 防御力强化·B
					player.statDefense *= 1.8f;
				} else {
					// S2 壳状防御
					player.statDefense *= 2.3f;
					if (player.statLife < player.statLifeMax) {
						float heal = player.statLifeMax * 0.03f / 60f;
						if (Main.rand.NextFloat() < heal - (int)heal) player.statLife++;
						player.statLife = System.Math.Min(player.statLife + (int)heal, player.statLifeMax);
					}
				}
			}
		}
	}
}
