using ArknightsMod.Content.Items.Weapons;
using ArknightsMod.Players;
using ArknightsMod.Systems.Gameplay.Skill;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Guard.Skadi
{
	public class SkadisGreatsword : ExpansionWeaponBase
	{
		protected override int[] EliteDamage => [78, 96, 113];

		public override void SetDefaults() {
			Item.damage = EliteDamage[0];
			Item.DamageType = DamageClass.Melee;
			Item.width = 94;
			Item.height = 92;
			Item.useTime = 40;
			Item.useAnimation = 40;
			Item.knockBack = 8f;
			Item.value = Item.sellPrice(gold: 1);
			Item.rare = ItemRarityID.Green;
			Item.autoReuse = true;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.crit = 4;
		}

		public override bool CanUseItem(Player player) {
			var mp = player.GetModPlayer<WeaponPlayer>();
			if (mp.SkillActive && mp.Skill == 1) {
				// S2：攻击力+170%，简化为伤害倍率提升
				Item.damage = (int)(EliteDamage[EliteStage] * 2.7f);
			} else {
				Item.damage = EliteDamage[EliteStage];
			}
			return base.CanUseItem(player);
		}
	}
}
