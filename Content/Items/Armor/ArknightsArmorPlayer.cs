using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ArknightsMod.Content.Items.Armor.Vanity.Defender.Beagle;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor
{
    public class ArknightsArmorPlayer : ModPlayer
    {
		/// <summary>
		/// 额外防御力加成，按百分比算，要加算，不要直接赋值！！
		/// </summary>
		public float extraDefenseBonus = 0f;

		public (float ratio, int value) LifeReplacement_Head = (0f, 0);
		public (float ratio, int value) LifeReplacement_Body = (0f, 0);
		public (float ratio, int value) LifeReplacement_Legs = (0f, 0);

		public override void PostUpdateEquips()
		{
			if (extraDefenseBonus != 0)
			{
				int bonusDefense = (int)(Player.statDefense * extraDefenseBonus);
				Player.statDefense += bonusDefense;
			}
		}
		public override void ResetEffects()
		{
			extraDefenseBonus = 0;
			LifeReplacement_Head = (0f, 0);
			LifeReplacement_Body = (0f, 0);
			LifeReplacement_Legs = (0f, 0);
		}
		public override void UpdateLifeRegen()
		{
			int baseLife = Player.statLifeMax;

			float totalRatio = 0f;
			int totalFixedValue = 0;

			//头盔
			if (LifeReplacement_Head.ratio > 0) {
				totalRatio += LifeReplacement_Head.ratio;
				totalFixedValue += LifeReplacement_Head.value;
			}
			//胸甲
			if (LifeReplacement_Body.ratio > 0) {
				totalRatio += LifeReplacement_Body.ratio;
				totalFixedValue += LifeReplacement_Body.value;
			}
			//腿甲
			if (LifeReplacement_Legs.ratio > 0) {
				totalRatio += LifeReplacement_Legs.ratio;
				totalFixedValue += LifeReplacement_Legs.value;
			}

			if (totalRatio > 0) {
				int newLife = (int)(baseLife * (1 - totalRatio)) + totalFixedValue;
				int offset = newLife - baseLife;
				Player.statLifeMax += offset;
				Player.statLifeMax2 += offset;
			}
		}
	}
}
