using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanguard.Plume
{
	public class PlumeGreaves : PlumeSetLegsPiece
	{
		public override int Rarity => 3;
		protected override string VanityItemName => nameof(PlumeLegs);
		protected override int VanityItemType => ModContent.ItemType<PlumeLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 7;
		}
	}
}
