using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanguard.Texas
{
	public class TexasHelmet : TexasSetHeadPiece
	{
		public override int Rarity => 5;
		protected override string VanityItemName => nameof(TexasHead);
		protected override int VanityItemType => ModContent.ItemType<TexasHead>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
