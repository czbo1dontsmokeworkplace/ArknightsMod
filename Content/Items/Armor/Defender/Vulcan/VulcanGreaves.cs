using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Defender.Vulcan
{
	public class VulcanGreaves : VulcanSetLegsPiece
	{
		public override int Rarity => 5;
		protected override string VanityItemName => nameof(VulcanLegs);
		protected override int VanityItemType => ModContent.ItemType<VulcanLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 15;
		}
	}
}
