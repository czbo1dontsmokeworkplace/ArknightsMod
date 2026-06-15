using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Supporter.Orchid
{
	public class OrchidHelmet : OrchidSetHeadPiece
	{
		public override int Rarity => 3;
		protected override string VanityItemName => nameof(OrchidHead);
		protected override int VanityItemType => ModContent.ItemType<OrchidHead>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
