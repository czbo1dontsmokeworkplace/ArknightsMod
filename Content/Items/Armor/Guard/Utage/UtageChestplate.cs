using System.Collections.Generic;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.Utage
{
	public class UtageChestplate : UtageSetBodyPiece
	{
		public override int Rarity => 4;
		protected override string VanityItemName => nameof(UtageBody);
		protected override int VanityItemType => ModContent.ItemType<UtageBody>();

		protected override void SetSetDefaults() {
			Item.defense = 16;
		}

		protected override void AppendActiveSetTooltips(List<TooltipLine> tooltips) {
			UtageSetTooltipHelper.AppendDynamicSetEffect(Mod, tooltips);
		}
	}
}
