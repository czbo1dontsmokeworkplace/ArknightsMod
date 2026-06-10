using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Defender.Saria
{
	public class SariaGreaves : SariaSetLegsPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(SariaLegs);
		protected override int VanityItemType => ModContent.ItemType<SariaLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 15;
		}
	}
}
