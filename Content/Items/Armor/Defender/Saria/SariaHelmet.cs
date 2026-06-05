using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Defender.Saria
{
	public class SariaHelmet : SariaSetHeadPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(SariaHead);
		protected override int VanityItemType => ModContent.ItemType<SariaHead>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
