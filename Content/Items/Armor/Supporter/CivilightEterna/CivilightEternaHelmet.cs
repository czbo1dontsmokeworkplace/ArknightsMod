using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Supporter.CivilightEterna
{
	public class CivilightEternaHelmet : CivilightEternaSetHeadPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(CivilightEternaHead);
		protected override int VanityItemType => ModContent.ItemType<CivilightEternaHead>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
