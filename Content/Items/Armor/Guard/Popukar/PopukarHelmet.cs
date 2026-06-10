using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.Popukar
{
	public class PopukarHelmet : PopukarSetHeadPiece
	{
		public override int Rarity => 3;
		protected override string VanityItemName => nameof(PopukarHead);
		protected override int VanityItemType => ModContent.ItemType<PopukarHead>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
