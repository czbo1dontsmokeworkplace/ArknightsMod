using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor
{
	internal static class OperatorMinionSlotHelper
	{
		public static int CountActiveMinions(Player player) {
			int count = 0;
			for (int i = 0; i < Main.maxProjectiles; i++) {
				Projectile proj = Main.projectile[i];
				if (proj.active && proj.owner == player.whoAmI && (proj.minion || proj.sentry))
					count++;
			}

			return count;
		}

		// 未使用的仆从栏位（可召唤但未占用）。
		public static float CountUnusedMinionSlots(Player player) {
			float usedSlots = 0f;
			foreach (Projectile proj in Main.ActiveProjectiles) {
				if (proj.owner == player.whoAmI && proj.minion && !proj.sentry)
					usedSlots += proj.minionSlots;
			}

			return System.Math.Max(0f, player.maxMinions - usedSlots);
		}
	}
}
