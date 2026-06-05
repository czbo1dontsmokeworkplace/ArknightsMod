using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Supporter.CivilightEterna
{
	public class CivilightEternaGreaves : CivilightEternaSetLegsPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(CivilightEternaLegs);
		protected override int VanityItemType => ModContent.ItemType<CivilightEternaLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
