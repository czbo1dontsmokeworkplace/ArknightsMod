using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Supporter.CivilightEterna
{
	public class CivilightEternaChestplate : CivilightEternaSetBodyPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(CivilightEternaBody);
		protected override int VanityItemType => ModContent.ItemType<CivilightEternaBody>();

		protected override void SetSetDefaults() {
			Item.defense = 18;
		}
	}
}
