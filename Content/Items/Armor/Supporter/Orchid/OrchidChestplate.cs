using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Supporter.Orchid
{
	public class OrchidChestplate : OrchidSetBodyPiece
	{
		public override int Rarity => 3;
		protected override string VanityItemName => nameof(OrchidBody);
		protected override int VanityItemType => ModContent.ItemType<OrchidBody>();

		protected override void SetSetDefaults() {
			Item.defense = 6;
		}
	}
}
