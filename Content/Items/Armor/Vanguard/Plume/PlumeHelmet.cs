using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanguard.Plume
{
	public class PlumeHelmet : PlumeSetHeadPiece
	{
		public override int Rarity => 3;
		protected override string VanityItemName => nameof(PlumeHead);
		protected override int VanityItemType => ModContent.ItemType<PlumeHead>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
