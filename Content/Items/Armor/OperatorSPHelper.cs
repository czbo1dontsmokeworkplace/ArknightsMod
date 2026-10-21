using ArknightsMod.Players;
using Terraria;
using Terraria.ID;

namespace ArknightsMod.Content.Items.Armor
{
	internal static class OperatorSPHelper
	{
		public static void TryGainSP(Player player, int amount) {
			if (player == null || Main.netMode == NetmodeID.MultiplayerClient || amount <= 0)
				return;

			player.GetModPlayer<WeaponPlayer>().TryAddSP(amount);
		}
	}
}
