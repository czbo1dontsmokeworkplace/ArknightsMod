using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Supporter.Ling
{
	public class LingHelmet : LingSetHeadPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(LingHead);
		protected override int VanityItemType => ModContent.ItemType<LingHead>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
