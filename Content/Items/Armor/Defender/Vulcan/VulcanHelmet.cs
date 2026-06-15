using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Defender.Vulcan
{
	public class VulcanHelmet : VulcanSetHeadPiece
	{
		public override int Rarity => 5;
		protected override string VanityItemName => nameof(VulcanHead);
		protected override int VanityItemType => ModContent.ItemType<VulcanHead>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
