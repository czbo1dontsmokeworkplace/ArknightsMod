using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Defender.Beagle
{
	public class BeagleGreaves : BeagleSetLegsPiece
	{
		public override int Rarity => 3;
		protected override string VanityItemName => nameof(BeagleLegs);
		protected override int VanityItemType => ModContent.ItemType<BeagleLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 6;
		}
	}
}
