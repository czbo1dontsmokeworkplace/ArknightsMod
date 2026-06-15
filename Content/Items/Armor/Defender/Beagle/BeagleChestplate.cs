using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Defender.Beagle
{
	public class BeagleChestplate : BeagleSetBodyPiece
	{
		public override int Rarity => 3;
		protected override string VanityItemName => nameof(BeagleBody);
		protected override int VanityItemType => ModContent.ItemType<BeagleBody>();

		protected override void SetSetDefaults() {
			Item.defense = 18;
		}
	}
}
