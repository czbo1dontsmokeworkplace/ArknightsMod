using ArknightsMod.Content.Items.Weapons;
using ArknightsMod.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Specialist.Gravel
{
	public class GravelDualBlades : ExpansionWeaponBase
	{
		protected override int[] EliteDamage => [25, 30, 36];

		public override void SetDefaults() {
			Item.damage = EliteDamage[0];
			Item.DamageType = DamageClass.Melee;
			Item.width = 46;
			Item.height = 46;
			Item.useTime = 22;
			Item.useAnimation = 22;
			Item.knockBack = 3f;
			Item.value = Item.sellPrice(silver: 25);
			Item.rare = ItemRarityID.Green;
			Item.autoReuse = true;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.crit = 4;
		}

		public override bool CanUseItem(Player player) {
			var mp = player.GetModPlayer<WeaponPlayer>();
			if (mp.SkillActive) {
				// S1 影袭：防御+400% 持续衰减（简化：持续期间大幅提升防御）
				// S2 鼠群：获得护盾（简化：提升防御）
				Item.damage = EliteDamage[EliteStage];
			} else {
				Item.damage = EliteDamage[EliteStage];
			}
			return base.CanUseItem(player);
		}

		public override void HoldItem(Player player) {
			base.HoldItem(player);
			var mp = player.GetModPlayer<WeaponPlayer>();
			if (mp.SkillActive) {
				if (mp.Skill == 0) {
					// S1 影袭：防御+400%，随时间衰减（简化：+400%）
					float activeTime = mp.CurrentSkill?.CurrentLevelData.ActiveTime ?? 12f;
					float progress = activeTime > 0f ? (float)mp.SkillTimer / (activeTime * 60f) : 0f;
					float bonus = 4.0f * (1f - progress);
					player.statDefense *= (1f + bonus);
				} else {
					// S2 鼠群：护盾（通过临时生命上限模拟）
					player.statLifeMax2 += (int)(player.statLifeMax * 2.5f);
				}
			}
		}
	}
}
