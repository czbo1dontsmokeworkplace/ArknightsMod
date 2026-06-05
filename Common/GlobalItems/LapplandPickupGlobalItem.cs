using ArknightsMod.Content.Items.Armor.Guard.Lappland;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Common.GlobalItems
{
	public class LapplandPickupGlobalItem : GlobalItem
	{
		public override void GrabRange(WorldItem item, Player player, ref int grabRange) {
			if (!player.GetModPlayer<LapplandSetPlayer>().LapplandSetActive)
				return;

			if (item.type == ItemID.Heart || item.inner.healMana > 0)
				grabRange += Item.lifeGrabRange;
		}
	}
}
