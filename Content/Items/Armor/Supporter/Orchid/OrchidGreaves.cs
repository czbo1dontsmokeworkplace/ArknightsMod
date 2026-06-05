using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Supporter.Orchid
{
	public class OrchidGreaves : OrchidSetLegsPiece
	{
		public override int Rarity => 3;
		protected override string VanityItemName => nameof(OrchidLegs);
		protected override int VanityItemType => ModContent.ItemType<OrchidLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
