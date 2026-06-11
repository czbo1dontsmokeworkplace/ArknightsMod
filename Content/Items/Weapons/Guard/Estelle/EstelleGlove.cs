using ArknightsMod.Content.Items.Weapons;
using ArknightsMod.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Guard.Estelle
{
	public class EstelleGlove : ExpansionWeaponBase
	{
		protected override int[] EliteDamage => [39, 47, 57];

		public override void SetDefaults() {
			Item.damage = EliteDamage[0];
			Item.DamageType = DamageClass.Melee;
			Item.width = 44;
			Item.height = 44;
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
			if (mp.SkillActive) {
				Item.damage = mp.Skill == 0
					? (int)(EliteDamage[EliteStage] * 1.8f)   // S1 攻击力+80%
					: (int)(EliteDamage[EliteStage] * 2.5f);  // S2 舍身突击 攻击力+150%
			} else {
				Item.damage = EliteDamage[EliteStage];
			}
			return base.CanUseItem(player);
		}

		public override void HoldItem(Player player) {
			base.HoldItem(player);
			var mp = player.GetModPlayer<WeaponPlayer>();
			// S2 舍身突击：不再成为治疗目标（通过添加 TimesBeenHurt 防止自动回血效果，暂用 LifeRegen 抑制）
			if (mp.SkillActive && mp.Skill == 1)
				player.lifeRegen = System.Math.Min(player.lifeRegen, 0);
		}
	}
}
