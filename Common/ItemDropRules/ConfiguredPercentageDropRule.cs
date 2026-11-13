using System;
using System.Collections.Generic;
using Terraria.GameContent.ItemDropRules;

namespace ArknightsMod.Common.ItemDropRules
{
	// 先按配置概率随机判断，再读取配置数量；概率与数量独立，不受玩家幸运值影响。
	public sealed class ConfiguredPercentageDropRule : CommonDrop
	{
		private readonly Func<float> getPercentage;
		private readonly Func<int> getQuantity;

		public ConfiguredPercentageDropRule(int itemType, Func<float> getPercentage, Func<int> getQuantity)
			: base(itemType, 1) {
			this.getPercentage = getPercentage;
			this.getQuantity = getQuantity;
		}

		private float GetPercentage() {
			float percentage = getPercentage();
			return float.IsFinite(percentage) ? Math.Clamp(percentage, 0f, 100f) : 0f;
		}

		public override ItemDropAttemptResult TryDroppingItem(DropAttemptInfo info) {
			// 随机值范围为 [0, 100)，因此 0% 必不掉，100% 必定通过。
			if (info.rng.NextDouble() * 100d >= GetPercentage())
				return new ItemDropAttemptResult { State = ItemDropAttemptResultState.FailedRandomRoll };

			// 概率通过后才获取数量，1% 可以掉 100 个，100% 也可以只掉 1 个。
			int stack = Math.Clamp(getQuantity(), 0, 9999);
			if (stack == 0)
				return new ItemDropAttemptResult { State = ItemDropAttemptResultState.FailedRandomRoll };

			CommonCode.DropItem(info, itemId, stack);
			return new ItemDropAttemptResult { State = ItemDropAttemptResultState.Success };
		}

		public override void ReportDroprates(List<DropRateInfo> drops, DropRateInfoChainFeed ratesInfo) {
			int quantity = Math.Clamp(getQuantity(), 0, 9999);
			float chance = quantity > 0 ? GetPercentage() / 100f : 0f;
			if (chance > 0f) {
				drops.Add(new DropRateInfo(itemId, quantity, quantity,
					chance * ratesInfo.parentDroprateChance, ratesInfo.conditions));
			}
			Chains.ReportDroprates(ChainedRules, chance, drops, ratesInfo);
		}
	}
}
