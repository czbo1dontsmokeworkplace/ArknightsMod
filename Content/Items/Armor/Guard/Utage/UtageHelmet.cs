using System.Collections.Generic;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.Utage
{
	public class UtageHelmet : UtageSetHeadPiece
	{
		public override int Rarity => 4;
		protected override string VanityItemName => nameof(UtageHead);
		protected override int VanityItemType => ModContent.ItemType<UtageHead>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}

		protected override void AppendActiveSetTooltips(List<TooltipLine> tooltips) {
			UtageSetTooltipHelper.AppendDynamicSetEffect(Mod, tooltips);
		}
	}
}
