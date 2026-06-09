using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Supporter.Ling
{
	public class LingGreaves : LingSetLegsPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(LingLegs);
		protected override int VanityItemType => ModContent.ItemType<LingLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 4;
		}
	}
}
