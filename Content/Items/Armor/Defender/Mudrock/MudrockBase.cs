using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Defender.Mudrock
{
	internal class MudrockItemChange : GlobalItem
	{
		public override void RightClick(Item item, Player player) {
			if (item.type == ModContent.ItemType<MudrockHelmet>() || item.type == ModContent.ItemType<MudrockHead>()) {
				int targetType = item.type == ModContent.ItemType<MudrockHelmet>()
					? ModContent.ItemType<MudrockHead>()
					: ModContent.ItemType<MudrockHelmet>();

				item.ChangeItemType(targetType);
				item.stack++;
				return;
			}

			if (item.type == ModContent.ItemType<MudrockChestplate>() || item.type == ModContent.ItemType<MudrockBody>()) {
				int targetType = item.type == ModContent.ItemType<MudrockChestplate>()
					? ModContent.ItemType<MudrockBody>()
					: ModContent.ItemType<MudrockChestplate>();

				item.ChangeItemType(targetType);
				item.stack++;
			}
		}

		public override bool CanRightClick(Item item) {
			return item.type == ModContent.ItemType<MudrockHelmet>() || item.type == ModContent.ItemType<MudrockHead>()
				|| item.type == ModContent.ItemType<MudrockChestplate>() || item.type == ModContent.ItemType<MudrockBody>()
				|| base.CanRightClick(item);
		}
	}
}
