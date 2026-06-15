using System.Collections.Generic;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.Utage
{
	public class UtageGreaves : UtageSetLegsPiece
	{
		public override int Rarity => 4;
		protected override string VanityItemName => nameof(UtageLegs);
		protected override int VanityItemType => ModContent.ItemType<UtageLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 5;
		}

		protected override void AppendActiveSetTooltips(List<TooltipLine> tooltips) {
			UtageSetTooltipHelper.AppendDynamicSetEffect(Mod, tooltips);
		}
	}
}
