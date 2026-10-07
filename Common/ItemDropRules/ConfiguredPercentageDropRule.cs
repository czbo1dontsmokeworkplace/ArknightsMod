using System;
using System.Collections.Generic;
using Terraria.GameContent.ItemDropRules;

namespace ArknightsMod.Common.ItemDropRules
{
	// 每次掉落和显示概率时读取当前服务器配置，修改百分比后无需重新加载模组。
	public sealed class ConfiguredPercentageDropRule : CommonDrop
	{
		private const int PercentageDenominator = 1_000_000;
		private readonly Func<float> getPercentage;

		public ConfiguredPercentageDropRule(int itemType, Func<float> getPercentage, int minimum = 1, int maximum = 1)
			: base(itemType, PercentageDenominator, minimum, maximum) {
			this.getPercentage = getPercentage;
		}

		private void UpdateChance() {
			float percentage = Math.Clamp(getPercentage(), 0f, 100f);
			chanceNumerator = (int)Math.Round(percentage * (PercentageDenominator / 100d));
		}

		public override ItemDropAttemptResult TryDroppingItem(DropAttemptInfo info) {
			UpdateChance();
			return base.TryDroppingItem(info);
		}

		public override void ReportDroprates(List<DropRateInfo> drops, DropRateInfoChainFeed ratesInfo) {
			UpdateChance();
			base.ReportDroprates(drops, ratesInfo);
		}
	}
}
