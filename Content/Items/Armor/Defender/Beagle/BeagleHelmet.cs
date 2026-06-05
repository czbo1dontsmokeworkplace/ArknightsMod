using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Defender.Beagle
{
	public class BeagleHelmet : BeagleSetHeadPiece
	{
		public override int Rarity => 3;
		protected override string VanityItemName => nameof(BeagleHead);
		protected override int VanityItemType => ModContent.ItemType<BeagleHead>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
