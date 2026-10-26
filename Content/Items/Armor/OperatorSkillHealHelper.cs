using ArknightsMod.Content.Buffs.ArmorSets;
using ArknightsMod.Content.Items.Armor.Defender.Spot;
using ArknightsMod.Content.Items.Armor.Supporter.CivilightEterna;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor
{
	// 技能治疗友方时由各技能实现处调用。
	internal static class OperatorSkillHealHelper
	{
		public static int ApplyHeal(Player target, int baseAmount, Player healer = null) {
			if (baseAmount <= 0)
				return 0;

			int lifeBefore = target.statLife;
			int finalAmount = SpotHealingHelper.ModifyOutgoingHeal(healer, baseAmount);
			target.Heal(finalAmount);

			CivilightEternaSetPlayer civilight = healer?.GetModPlayer<CivilightEternaSetPlayer>();
			if (civilight != null && civilight.CivilightEternaSetActive
				&& target.HasBuff(ModContent.BuffType<CivilightEternaHealBoostBuff>())) {
				int bonus = (int)(finalAmount * 0.5f);
				if (bonus > 0) {
					target.Heal(bonus);
					target.HealEffect(bonus);
					finalAmount += bonus;
				}
			}

			if (healer != null) {
				global::ArknightsMod.Content.Items.Armor.DirectHealingEffects.OnDirectHeal(healer, target, target.statLife - lifeBefore);
			}

			return finalAmount;
		}

	}
}
