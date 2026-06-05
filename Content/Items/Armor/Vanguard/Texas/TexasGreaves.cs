using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanguard.Texas
{
	public class TexasGreaves : TexasSetLegsPiece
	{
		public override int Rarity => 5;
		protected override string VanityItemName => nameof(TexasLegs);
		protected override int VanityItemType => ModContent.ItemType<TexasLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
